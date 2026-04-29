using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using AI.Application.Common.Resources.Prompts;
using AI.Application.Common.Telemetry;
using AI.Application.DTOs;
using AI.Application.DTOs.ExcelAnalysis;
using AI.Application.DTOs.Chat;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.Results;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using ToonSharp;

namespace AI.Application.UseCases;

/// <summary>
/// Excel/CSV dosya analizi Use Case implementasyonu
/// DuckDB ile Excel/CSV analizi, LLM ile analiz planı ve yorumlama
/// AIChatUseCase'den SRP prensibi gereği ayrıştırılmıştır
/// </summary>
public class ExcelAnalysisUseCase : IExcelAnalysisUseCase
{
    #region Fields

    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly ISignalRHubContext _hubContext;
    private readonly IConversationUseCase _historyService;
    private readonly IExcelAnalysisService _excelAnalysisService;
    private readonly ILogger<ExcelAnalysisUseCase> _logger;

    #endregion

    #region Constructor

    public ExcelAnalysisUseCase(
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ISignalRHubContext hubContext,
        IConversationUseCase historyService,
        IExcelAnalysisService excelAnalysisService,
        ILogger<ExcelAnalysisUseCase> logger)
    {
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _excelAnalysisService = excelAnalysisService ?? throw new ArgumentNullException(nameof(excelAnalysisService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public async Task<Result<LLmResponseModel>> ProcessExcelQueryAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.DocumentProcessing.StartActivity("ProcessExcelQuery");
        activity?.SetTag("file.name", request.FileName);
        activity?.SetTag("user.query", request.Prompt);

        var metadata = new Dictionary<string, object>
        {
            { "SignalRJsFunction", "None" }
        };

        try
        {
            // Base64 → Stream
            byte[] fileBytes = Convert.FromBase64String(request.FileBase64);
            using var stream = new MemoryStream(fileBytes);

            // 1. Şemayı çıkar
            var schemaResult = await _excelAnalysisService.GetSchemaAsync(stream, request.FileName).ConfigureAwait(false);

            if (!schemaResult.Success)
            {
                return Result<LLmResponseModel>.Error($"Excel şema hatası: {schemaResult.ErrorMessage}");
            }

            activity?.SetTag("table.name", schemaResult.TableName);
            activity?.SetTag("row.count", schemaResult.RowCount);
            activity?.SetTag("column.count", schemaResult.Columns.Count);

            // 2. Sütun bilgilerini formatla
            var columnsText = string.Join("\n", schemaResult.Columns.Select(c =>
                $"- `{c.Name}` ({c.DataType}){(c.IsNullable ? " - nullable" : "")}"));

            // 3. Örnek verileri JSON'a çevir
            var sampleDataJson = JsonConvert.SerializeObject(schemaResult.SampleRows, Formatting.Indented);

            // 4. LLM'den analiz planı al (tek veya çoklu SQL)
            var analysisPlan = await GetAnalysisPlanAsync(
                schemaResult.TableName, schemaResult.RowCount, columnsText, sampleDataJson, request.Prompt
            ).ConfigureAwait(false);

            if (analysisPlan == null || analysisPlan.Queries.Count == 0)
            {
                return Result<LLmResponseModel>.Error("LLM'den geçerli bir analiz planı alınamadı.");
            }

            activity?.SetTag("analysis.type", analysisPlan.AnalysisType);
            activity?.SetTag("query.count", analysisPlan.Queries.Count);

            _logger.LogInformation(
                "Analiz planı alındı - Tip: {AnalysisType}, Sorgu sayısı: {QueryCount}",
                analysisPlan.AnalysisType, analysisPlan.Queries.Count);

            // 5. Her sorguyu DuckDB'de çalıştır
            var allResults = new List<AnalysisQueryResult>();
            foreach (var query in analysisPlan.Queries)
            {
                stream.Position = 0;
                var queryResult = await ExecuteSingleQueryWithRetryAsync(
                    stream, request, query, schemaResult
                ).ConfigureAwait(false);

                if (queryResult.Success)
                {
                    allResults.Add(queryResult);

                    // Ara sonucu SignalR ile gönder (çoklu sorguda)
                    if (analysisPlan.Queries.Count > 1)
                    {
                        await SendIntermediateResultAsync(
                            request.ConnectionId, request.ConversationId, queryResult
                        ).ConfigureAwait(false);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Sorgu başarısız atlanıyor - Başlık: {Title}, Hata: {Error}",
                        query.Title, queryResult.ErrorMessage);
                }
            }

            if (allResults.Count == 0)
            {
                return Result<LLmResponseModel>.Error("Hiçbir SQL sorgusu başarılı olarak çalıştırılamadı.");
            }

            // 6. Bağlam bilgisini history'e kaydet
            var contextMessage = BuildContextMessage(request, schemaResult, columnsText, allResults);
            await _historyService.AddAssistantMessageAsync(request, contextMessage, metadata).ConfigureAwait(false);

            // 7. Sonuçları LLM'e gönder ve yorumlatarak streaming yanıt al
            string interpretPrompt;
            if (allResults.Count > 1)
            {
                // Çoklu sonuç yorumlama
                var allResultsText = BuildMultiResultInterpretData(allResults);
                interpretPrompt = SystemPrompt.GetExcelMultiInterpretPrompt(
                    request.Prompt, allResults.Count, allResultsText);
            }
            else
            {
                // Tek sonuç yorumlama (mevcut davranış)
                var firstResult = allResults.First();
                var dataToon = ToonSerializer.Serialize(new { results = firstResult.QueryResult.Data });
                interpretPrompt = SystemPrompt.GetExcelInterpretPrompt(
                    request.Prompt,
                    firstResult.QueryResult.RowCount,
                    firstResult.QueryResult.ExecutionTimeMs,
                    dataToon);
            }

            // Yeni chat history oluştur (yorum için)
            var interpretHistory = new ChatHistory();
            interpretHistory.AddUserMessage(interpretPrompt);

            var responseBuilder = new StringBuilder();
            var openAiSettings = CreateOpenAiSettings();

            await foreach (var content in _chatCompletionService.GetStreamingChatMessageContentsAsync(
                interpretHistory, openAiSettings, _kernel))
            {
                if (string.IsNullOrEmpty(content.Content))
                {
                    continue;
                }

                responseBuilder.Append(content.Content);

                var streamingResponse = new LLmResponseModel
                {
                    IsSuccess = true,
                    ConversationId = request.ConversationId,
                    HtmlMessage = content.Content
                };

                var message = Result<LLmResponseModel>.Success(streamingResponse, "chat");
                await _hubContext.Clients.Client(request.ConnectionId)
                    .SendAsync("ReceiveStreamingMessage", message).ConfigureAwait(false);
            }

            var fullResponse = responseBuilder.ToString();

            // 8. Save to history first and get MessageId
            var historyResult = await _historyService.AddAssistantMessageAsync(request, fullResponse,
                new Dictionary<string, object> { { "SignalRJsFunction", "ReceiveMessage" } }).ConfigureAwait(false);

            // 9. Final response with MessageId
            var response = new LLmResponseModel
            {
                IsSuccess = true,
                ConversationId = request.ConversationId,
                MessageId = historyResult.MessageId.ToString(),
                HtmlMessage = fullResponse
            };

            var apiResult = Result<LLmResponseModel>.Success(response, "Excel analizi tamamlandı.", "chat");

            // Send final message with MessageId
            await _hubContext.Clients.Client(request.ConnectionId)
                .SendAsync("ReceiveMessage", apiResult).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Ok);

            var totalRows = allResults.Sum(r => r.QueryResult.RowCount);
            var totalMs = allResults.Sum(r => r.QueryResult.ExecutionTimeMs);
            _logger.LogInformation(
                "Excel analizi tamamlandı - Dosya: {FileName}, Sorgular: {QueryCount}, Toplam Satır: {RowCount}, Toplam Süre: {ExecutionTime}ms",
                request.FileName, allResults.Count, totalRows, totalMs);

            return apiResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel sorgu hatası - Dosya: {FileName}", request.FileName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return Result<LLmResponseModel>.Error($"Excel işleme hatası: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// LLM'den analiz planı alır (tek veya çoklu SQL)
    /// </summary>
    private async Task<ExcelAnalysisPlan?> GetAnalysisPlanAsync(
        string tableName, long rowCount, string columnsText, string sampleDataJson, string userQuery)
    {
        var prompt = SystemPrompt.GetExcelAnalysisPlanPrompt(
            tableName, rowCount, columnsText, sampleDataJson, userQuery);

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            new OpenAIPromptExecutionSettings { Temperature = 0.1f },
            _kernel).ConfigureAwait(false);

        var responseText = response.Content ?? "";
        _logger.LogDebug("LLM analiz planı yanıtı: {Response}", responseText);

        return ParseAnalysisPlanFromResponse(responseText);
    }

    /// <summary>
    /// LLM yanıtından analiz planını (ExcelAnalysisPlan) parse eder
    /// </summary>
    private ExcelAnalysisPlan? ParseAnalysisPlanFromResponse(string response)
    {
        try
        {
            // JSON bloğunu bul
            var jsonMatch = Regex.Match(response, @"\{[\s\S]*\}", RegexOptions.Multiline);
            if (!jsonMatch.Success)
            {
                _logger.LogWarning("Analiz planı JSON'u bulunamadı: {Response}", response);
                return null;
            }

            var json = jsonMatch.Value;
            var plan = JsonConvert.DeserializeObject<ExcelAnalysisPlan>(json);

            if (plan == null || plan.Queries.Count == 0)
            {
                _logger.LogWarning("Analiz planı boş veya geçersiz: {Json}", json);
                return null;
            }

            // Güvenlik kontrolü: her SQL'in SELECT ile başladığından emin ol
            plan.Queries = plan.Queries
                .Where(q => !string.IsNullOrWhiteSpace(q.Sql) &&
                            q.Sql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (plan.Queries.Count == 0)
            {
                _logger.LogWarning("Güvenlik kontrolünden geçen sorgu yok");
                return null;
            }

            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analiz planı parse hatası");

            // Fallback: Eski tek-SQL formatını dene
            var (sql, explanation) = ParseSqlFromLlmResponse(response);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                return new ExcelAnalysisPlan
                {
                    AnalysisType = "single",
                    Queries = new List<AnalysisQuery>
                    {
                        new() { Title = "Sorgu Sonucu", Description = explanation, Sql = sql }
                    }
                };
            }

            return null;
        }
    }

    /// <summary>
    /// Tek bir SQL sorgusunu retry mekanizması ile çalıştırır
    /// </summary>
    private async Task<AnalysisQueryResult> ExecuteSingleQueryWithRetryAsync(
        Stream fileStream, ChatRequest request, AnalysisQuery query, ExcelSchemaResult schemaResult)
    {
        const int maxRetries = 3;
        string? lastError = null;

        var chatHistory = new ChatHistory();
        // İlk SQL'i çalıştır, hata olursa LLM'den düzeltmesini iste
        var currentSql = query.Sql;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                fileStream.Position = 0;
                var queryResult = await _excelAnalysisService.ExecuteQueryAsync(
                    fileStream, request.FileName, currentSql).ConfigureAwait(false);

                if (queryResult.Success)
                {
                    _logger.LogInformation(
                        "Sorgu başarılı - Başlık: {Title}, Deneme: {Attempt}, Satır: {RowCount}",
                        query.Title, attempt, queryResult.RowCount);

                    return new AnalysisQueryResult
                    {
                        Title = query.Title,
                        Description = query.Description,
                        ExecutedSql = currentSql,
                        QueryResult = queryResult,
                        Success = true
                    };
                }

                lastError = queryResult.ErrorMessage;
                _logger.LogWarning(
                    "Sorgu hatası - Başlık: {Title}, Deneme: {Attempt}, Hata: {Error}",
                    query.Title, attempt, queryResult.ErrorMessage);
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning(ex,
                    "Sorgu exception - Başlık: {Title}, Deneme: {Attempt}", query.Title, attempt);
            }

            // Retry: LLM'den düzeltme iste
            if (attempt < maxRetries)
            {
                var retryPrompt = $"""
                    SQL sorgusu çalıştırılırken hata oluştu. DuckDB SQL sözdizimi ile düzelt.
                    
                    Tablo: {schemaResult.TableName}
                    Orijinal SQL: {currentSql}
                    Hata: {lastError}
                    
                    Sadece düzeltilmiş SELECT sorgusunu döndür, başka bir şey yazma.
                    """;

                chatHistory.AddUserMessage(retryPrompt);

                var retryResponse = await _chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    new OpenAIPromptExecutionSettings { Temperature = 0.1f },
                    _kernel).ConfigureAwait(false);

                var retryText = retryResponse.Content?.Trim() ?? "";
                chatHistory.AddAssistantMessage(retryText);

                // SQL'i çıkar
                var (retrySql, _) = ParseSqlFromLlmResponse(retryText);
                if (!string.IsNullOrWhiteSpace(retrySql))
                {
                    currentSql = retrySql;
                }
            }
        }

        return new AnalysisQueryResult
        {
            Title = query.Title,
            Description = query.Description,
            ExecutedSql = currentSql,
            Success = false,
            ErrorMessage = lastError
        };
    }

    /// <summary>
    /// Ara sonucu Markdown tablo olarak SignalR ile gönderir (kullanıcı beklerken tabloları görsün)
    /// Markdown formatı kullanılır, frontend'deki parseMarkdownContent() render/stil işlemini yapar.
    /// </summary>
    private async Task SendIntermediateResultAsync(
        string connectionId, string conversationId, AnalysisQueryResult result)
    {
        try
        {
            var md = new StringBuilder();
            md.AppendLine($"#### 📊 {result.Title}");
            md.AppendLine();
            md.AppendLine($"{result.Description}");
            md.AppendLine();
            md.AppendLine($"✅ {result.QueryResult.RowCount} satır, {result.QueryResult.ExecutionTimeMs}ms");
            md.AppendLine();

            // Markdown tablo oluştur (max 10 satır göster)
            if (result.QueryResult.Data != null && result.QueryResult.Data.Count > 0)
            {
                var firstRow = result.QueryResult.Data[0] as IDictionary<string, object>;
                if (firstRow != null)
                {
                    var keys = firstRow.Keys.ToList();

                    // Başlık satırı
                    md.AppendLine("| " + string.Join(" | ", keys) + " |");
                    // Ayırıcı satır
                    md.AppendLine("| " + string.Join(" | ", keys.Select(_ => "---")) + " |");

                    // Veri satırları (max 10)
                    var rowsToShow = Math.Min(result.QueryResult.Data.Count, 10);
                    for (int i = 0; i < rowsToShow; i++)
                    {
                        var row = result.QueryResult.Data[i] as IDictionary<string, object>;
                        if (row == null) continue;

                        var values = keys.Select(k =>
                        {
                            row.TryGetValue(k, out var val);
                            var str = val?.ToString() ?? "";
                            // Pipe karakterini escape et (markdown tablo bozulmasın)
                            return str.Replace("|", "\\|");
                        });
                        md.AppendLine("| " + string.Join(" | ", values) + " |");
                    }

                    if (result.QueryResult.Data.Count > 10)
                    {
                        md.AppendLine();
                        md.AppendLine($"*... ve {result.QueryResult.Data.Count - 10} satır daha*");
                    }
                }
            }

            md.AppendLine();
            md.AppendLine("---");

            var streamingResponse = new LLmResponseModel
            {
                IsSuccess = true,
                ConversationId = conversationId,
                HtmlMessage = md.ToString()
            };

            var message = Result<LLmResponseModel>.Success(streamingResponse, "chat");
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveStreamingMessage", message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ara sonuç gönderme hatası - Başlık: {Title}", result.Title);
        }
    }

    /// <summary>
    /// Çoklu sonuçları TOON formatında birleştirir (LLM yorumlaması için)
    /// </summary>
    private static string BuildMultiResultInterpretData(List<AnalysisQueryResult> results)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.AppendLine($"### Sorgu {i + 1}: {r.Title}");
            sb.AppendLine($"**Açıklama:** {r.Description}");
            sb.AppendLine($"**SQL:** `{r.ExecutedSql}`");
            sb.AppendLine($"**Sonuç:** {r.QueryResult.RowCount} satır, {r.QueryResult.ExecutionTimeMs}ms");
            sb.AppendLine();

            // TOON formatında veri
            var dataToon = ToonSerializer.Serialize(new { results = r.QueryResult.Data });
            sb.AppendLine("```toon");
            sb.AppendLine(dataToon);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Bağlam mesajı oluşturur (sonraki sorgularda kullanılmak üzere)
    /// </summary>
    private static string BuildContextMessage(
        ChatRequest request, ExcelSchemaResult schema, string columnsText, List<AnalysisQueryResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Excel Analiz Bağlamı");
        sb.AppendLine();
        sb.AppendLine($"**Dosya:** {request.FileName}");
        sb.AppendLine($"**Tablo Adı:** {schema.TableName}");
        sb.AppendLine($"**Toplam Satır:** {schema.RowCount}");
        sb.AppendLine();
        sb.AppendLine("### Mevcut Sütunlar:");
        sb.AppendLine(columnsText);
        sb.AppendLine();
        sb.AppendLine("### Kullanıcı İsteği:");
        sb.AppendLine(request.Prompt);
        sb.AppendLine();
        sb.AppendLine($"### Çalıştırılan Sorgular ({results.Count} adet):");

        foreach (var r in results)
        {
            sb.AppendLine($"- **{r.Title}**: `{r.ExecutedSql}` → {r.QueryResult.RowCount} satır, {r.QueryResult.ExecutionTimeMs}ms");
        }

        sb.AppendLine();
        sb.AppendLine("> Not: Kullanıcı ek sorgular isterse, yukarıdaki şema bilgilerini referans alarak yeni sorgular oluşturulabilir.");

        return sb.ToString();
    }

    /// <summary>
    /// LLM yanıtından SQL ve açıklamayı parse eder
    /// </summary>
    private static (string sql, string explanation) ParseSqlFromLlmResponse(string response)
    {
        try
        {
            // JSON bloğunu bul
            var jsonMatch = Regex.Match(response, @"\{[\s\S]*\}", RegexOptions.Multiline);
            if (!jsonMatch.Success)
            {
                // Doğrudan SQL olabilir
                if (response.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return (response.Trim(), "");
                }
                return ("", "");
            }

            var json = jsonMatch.Value;
            var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (parsed == null)
                return ("", "");

            var sql = parsed.GetValueOrDefault("sql", "")?.Trim() ?? "";
            var explanation = parsed.GetValueOrDefault("explanation", "")?.Trim() ?? "";

            return (sql, explanation);
        }
        catch (Exception)
        {
            // JSON parse edilemezse doğrudan response'u kontrol et
            if (response.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                var selectMatch = Regex.Match(response, @"SELECT[\s\S]+?(?:LIMIT\s+\d+|;|$)", RegexOptions.IgnoreCase);
                if (selectMatch.Success)
                {
                    return (selectMatch.Value.Trim().TrimEnd(';'), "");
                }
            }
            return ("", "");
        }
    }

    /// <summary>
    /// OpenAI ayarlarını oluşturur
    /// </summary>
    private static OpenAIPromptExecutionSettings CreateOpenAiSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.3F
        };
    }

    #endregion
}
