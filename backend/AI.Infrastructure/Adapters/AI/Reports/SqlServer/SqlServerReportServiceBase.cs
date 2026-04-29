using System.Text;
using System.Text.RegularExpressions;
using AI.Application.Common.Helpers;
using AI.Application.Common.Resources.Prompts;
using AI.Application.Configuration;
using AI.Application.DTOs;
using AI.Application.DTOs.History;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AIChat;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.Ports.Secondary.Services.Database;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.Results;
using AI.Application.DTOs.Dashboard;
using AI.Application.DTOs.Chat;
using AI.Application.DTOs.Database;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ToonSharp;

namespace AI.Infrastructure.Adapters.AI.Reports.SqlServer;

/// <summary>
/// SQL Server veritabanı rapor servisleri için abstract base class.
/// Tüm SQL Server rapor servisleri bu sınıftan türetilmelidir.
/// OracleReportServiceBase ile aynı pattern ve özellikleri içerir.
/// </summary>
public abstract class SqlServerReportServiceBase : IReportService
{
    #region Constants

    private static readonly Regex JsonBlockRegex = new(@"```json\s*(.*?)\s*```",
        RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex LineBreakRegex = new(@"\r\n|\r|\n",
        RegexOptions.Compiled);

    private const int DelayBetweenReports = 1000;
    private const float ChartGenerationTemperature = 0.1F;

    // Retry configuration
    private const int MaxRetryAttempts = 5;
    private const int MaxEmptyResultRetries = 2;
    private const int BaseDelayMs = 1000;
    private const int MaxDelayMs = 10000;
    
    // Token bazlı chunk ayarları
    private const int TokenBasedChunkingThreshold = 700_000; // 700K token eşiği
    private const int TargetTokensPerChunk = 80_000;
    private const int TokensPerRowEstimate = 100;
    
    // Kayıt bazlı chunk ayarları — alt sınıflar override edebilir
    protected virtual int RecordBasedChunkingThreshold => 1000; // 1000 kayıt eşiği
    protected virtual int RecordsPerChunk => 1000; // Chunk başına kayıt sayısı
    protected virtual int MaxChunks => 10;
    protected virtual int MaxParallelism => 3; // Paralel chunk analizi sayısı (500 hatalarını azaltmak için düşürüldü)
    
    // Chunk retry ayarları
    private const int MaxChunkRetryAttempts = 3;
    private const int ChunkRetryBaseDelayMs = 2000;
    
    // Tema normalizasyon mapping'leri - AdventureWorks odaklı
    private static readonly Dictionary<string, string[]> ThemeMappings = new()
    {
        { "Ürün Kalitesi Sorunları", new[] { "ürün kalitesi", "defolu", "bozuk", "hasarlı", "quality", "defect", "damaged" } },
        { "Teslimat Gecikmeleri", new[] { "teslimat", "kargo", "gecikme", "shipment", "delivery", "delay", "late" } },
        { "Fiyat Uyumsuzlukları", new[] { "fiyat", "etiket", "kampanya", "indirim", "price", "discount", "pricing" } },
        { "Müşteri Memnuniyeti", new[] { "müşteri", "memnuniyet", "şikayet", "customer", "satisfaction", "complaint" } },
        { "Stok Sorunları", new[] { "stok", "envanter", "tükenen", "yetersiz", "inventory", "stock", "out of stock" } },
        { "Sipariş İşleme Sorunları", new[] { "sipariş", "işleme", "onay", "order", "processing", "approval" } },
        { "Ödeme Sorunları", new[] { "ödeme", "fatura", "kredi kartı", "payment", "invoice", "credit card" } },
        { "Satış Temsilcisi Performansı", new[] { "satış temsilcisi", "salesperson", "sales rep", "performans", "performance" } },
        { "Bölgesel Satış Sorunları", new[] { "bölge", "territory", "region", "satış", "sales" } },
        { "Üretim Gecikmeleri", new[] { "üretim", "manufacturing", "production", "gecikme", "delay" } },
        { "İade/İptal Sorunları", new[] { "iade", "iptal", "geri ödeme", "return", "cancel", "refund" } },
        { "Müşteri Hizmetleri", new[] { "müşteri hizmetleri", "customer service", "destek", "support" } }
    };

    #endregion

    #region Fields

    protected readonly IDatabaseService DatabaseService;
    protected readonly IChatCompletionService ChatCompletionService;
    protected readonly Kernel Kernel;
    protected readonly ISignalRHubContext HubContext;
    protected readonly IDashboardUseCase DashboardUseCase;
    protected readonly IConversationUseCase ConversationUseCase;
    protected readonly ILogger Logger;
    protected readonly ISqlAgentPipeline? SqlAgentPipeline;
    protected readonly MultiAgentSettings? MultiAgentSettings;
    protected readonly DashboardSettings? DashboardSettings;
    protected readonly InsightAnalysisSettings? InsightAnalysisSettings;
    protected readonly IUserMemoryUseCase? UserMemoryUseCase;
    protected readonly ICurrentUserService? CurrentUserService;
    protected readonly IDynamicPromptBuilder? DynamicPromptBuilder;
    protected readonly IReActUseCase? ReActUseCase;

    #endregion

    #region Abstract Members

    /// <summary>
    /// Her rapor servisi kendi system prompt dosya adını belirtmelidir.
    /// Örnek: "adventurerworks_server_assistant_prompt.md"
    /// </summary>
    protected abstract string SystemPromptFileName { get; }
    protected abstract string ReportServiceType { get; }
    
    /// <summary>
    /// Veritabanı tipi. Bu class için sabit: "sqlserver"
    /// </summary>
    protected string ReportDatabaseType => "sqlserver";
    
    /// <summary>
    /// Veritabanı servis tipi. Varsayılan: "adventureworks". Alt sınıflar override edebilir.
    /// </summary>
    protected virtual string ReportDatabaseServiceType => "adventureworks";

    /// <summary>
    /// Token bazlı chunking kullanılsın mı?
    /// </summary>
    protected bool UseTokenBasedChunking => InsightAnalysisSettings?.Chunking?.UseTokenBasedChunking ?? false;

    #endregion

    #region Virtual Prompt Properties

    /// <summary>
    /// Prompt dosyalarının bulunduğu klasör yolu. Alt sınıflar override edebilir.
    /// </summary>
    protected virtual string PromptFolder => "Common/Resources/Prompts";
    
    /// <summary>
    /// Chunk analizi prompt dosya adı. Alt sınıflar override edebilir.
    /// </summary>
    protected virtual string ChunkAnalysisPromptFile => "chunk_analysis_prompt.md";
    
    /// <summary>
    /// Insight analizi prompt dosya adı. Alt sınıflar override edebilir.
    /// </summary>
    protected virtual string InsightAnalysisPromptFile => "insight_analysis_prompt.md";
    
    /// <summary>
    /// Dashboard config prompt dosya adı. Alt sınıflar override edebilir.
    /// </summary>
    protected virtual string DashboardConfigPromptFile => "dashboard_config_generator_prompt.md";
    
    /// <summary>
    /// Dashboard HTML prompt dosya adı. Alt sınıflar override edebilir.
    /// </summary>
    protected virtual string DashboardHtmlPromptFile => "dashboard_generator_prompt_adventureworks.md";

    #endregion

    #region Constructor

    protected SqlServerReportServiceBase(
        IDatabaseService databaseService,
        ISignalRHubContext hubContext,
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ILogger logger,
        IDashboardUseCase dashboardService,
        IConversationUseCase historyService,
        ISqlAgentPipeline? sqlAgentPipeline = null,
        MultiAgentSettings? multiAgentSettings = null,
        DashboardSettings? dashboardSettings = null,
        InsightAnalysisSettings? insightAnalysisSettings = null,
        IUserMemoryUseCase? userMemoryService = null,
        ICurrentUserService? currentUserService = null,
        IDynamicPromptBuilder? dynamicPromptBuilder = null,
        IReActUseCase? reactUseCase = null)
    {
        DatabaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        ChatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        HubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        DashboardUseCase = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        ConversationUseCase = historyService ?? throw new ArgumentNullException(nameof(historyService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SqlAgentPipeline = sqlAgentPipeline;
        MultiAgentSettings = multiAgentSettings;
        DashboardSettings = dashboardSettings ?? new DashboardSettings();
        InsightAnalysisSettings = insightAnalysisSettings ?? new InsightAnalysisSettings();
        UserMemoryUseCase = userMemoryService;
        CurrentUserService = currentUserService;
        DynamicPromptBuilder = dynamicPromptBuilder;
        ReActUseCase = reactUseCase;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Standart rapor üretimi
    /// </summary>
    public virtual async Task<Result<LLmResponseModel>> GetReportsAsync(ChatRequest request)
    {
        try
        {
            ValidateRequest(request);

            await ConversationUseCase.AddUserMessageAsync(request, request.Prompt).ConfigureAwait(false);
            var chatHistory = await ConversationUseCase.GetChatHistoryAsync(request).ConfigureAwait(false);
            var openAiSettings = CreateOpenAiSettings();
            var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(chatHistory!, openAiSettings, Kernel).ConfigureAwait(false);

            return await ParseResponseAsync(resultPrompt[0].ToString()!, request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rapor üretilirken hata oluştu - ConnectionId: {ConnectionId}",
                request?.ConnectionId ?? "NULL");
            return CreateErrorResponse("Rapor üretilirken bir hata oluştu.");
        }
    }

    /// <summary>
    /// HTML ile rapor üretimi
    /// </summary>
    public virtual async Task<Result<LLmResponseModel>> GetReportsWithHtmlAsync(ChatRequest request)
    {
        try
        {
            ValidateRequest(request);

            // ===== ReAct: THOUGHT =====
            if (ReActUseCase != null)
            {
                await ReActUseCase.SendThoughtAsync(
                    request.ConnectionId,
                    request.Prompt,
                    $"Rapor üretimi başlatılıyor. Veritabanı tipi: {ReportServiceType}"
                ).ConfigureAwait(false);
            }

            // System prompt oluştur (dinamik veya statik)
            var systemPrompt = await BuildSystemPromptAsync(request.Prompt).ConfigureAwait(false);
            
            // Long-Term Memory: Kullanıcı tercihleri ve bağlamını ekle
            if (UserMemoryUseCase != null)
            {
                try
                {
                    var memoryContext = await UserMemoryUseCase.BuildMemoryContextAsync(request.Prompt).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(memoryContext))
                    {
                        systemPrompt = systemPrompt + "\n\n" + memoryContext;
                        Logger.LogDebug("Memory context added to report system prompt for ConnectionId: {ConnectionId}", request.ConnectionId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Memory context could not be loaded for report, continuing without it");
                }
            }
            
            await ConversationUseCase.ReplaceSystemPromptAsync(request, systemPrompt).ConfigureAwait(false);

            var openAiSettings = CreateOpenAiSettings();

            // Retry mekanizması ile LLM çağrısı ve ParseResponse
            var response = await ExecuteWithRetryAsync(
                async () =>
                {
                    // Her retry'da güncel chat history'yi al (history'e eklenen hata mesajlarını da içerir)
                    // IsDbResponse: true olan mesajlar otomatik olarak filtrelenir (includeDbResponses: false)
                    var chatHistory = await ConversationUseCase.GetChatHistoryAsync(request, includeDbResponses: false).ConfigureAwait(false);

                    var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(
                        chatHistory!, openAiSettings, Kernel).ConfigureAwait(false);
                    var result = resultPrompt[0].ToString()!;

                    // ParseResponseAsync'i de retry kapsamına al
                    return await ParseResponseAsync(result, request).ConfigureAwait(false);
                },
                onRetryAsync: async (ex, attempt, delay) =>
                {
                    // Her retry'da kullanıcıya bilgi mesajı ekle
                    var retryMessage = $"İşlem başarısız oldu (Deneme {attempt}/{MaxRetryAttempts}): {ex.Message}. {delay}ms sonra tekrar denenecek...";
                    await Task.CompletedTask;
                },
                connectionId: request.ConnectionId,
                operationName: "LLM Chat Completion ve Rapor İşleme").ConfigureAwait(false);

            // Long-Term Memory: Rapor konuşmasından kullanıcı bilgilerini çıkar ve sakla (fire-and-forget)
            if (UserMemoryUseCase != null && CurrentUserService != null)
            {
                var currentUserId = CurrentUserService.UserId;
                if (!string.IsNullOrEmpty(currentUserId) && response.IsSucceed && response.ResultData != null)
                {
                    var responseContent = response.ResultData.HtmlMessage ?? string.Empty;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await UserMemoryUseCase.ExtractAndStoreMemoriesAsync(request.Prompt, responseContent, currentUserId, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Memory extraction failed for report, continuing without it");
                        }
                    }, CancellationToken.None);
                }
            }

            // ===== ReAct: OBSERVATION =====
            if (ReActUseCase != null)
            {
                await ReActUseCase.SendObservationAsync(
                    request.ConnectionId,
                    "Rapor başarıyla oluşturuldu."
                ).ConfigureAwait(false);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "HTML raporu üretilirken hata oluştu - ConnectionId: {ConnectionId}",
                request?.ConnectionId ?? "NULL");
            return Result<LLmResponseModel>.Error("HTML raporu üretilirken bir hata oluştu.");
        }
    }

    #endregion

    #region Virtual Methods (Override edilebilir)

    /// <summary>
    /// Request validasyonu. Ek validasyon kuralları için override edilebilir.
    /// </summary>
    protected virtual void ValidateRequest(ChatRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            throw new ArgumentException("ConnectionId boş olamaz", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt boş olamaz", nameof(request));
    }

    /// <summary>
    /// Kullanıcı sorgusuna göre sistem promptunu oluşturur.
    /// Neo4j Schema Catalog etkinse dinamik prompt, değilse statik prompt kullanır.
    /// </summary>
    /// <param name="userQuery">Kullanıcının doğal dildeki sorusu</param>
    /// <returns>Sistem promptu</returns>
    protected virtual async Task<string> BuildSystemPromptAsync(string userQuery)
    {
        // Statik prompt'u oku (fallback olarak kullanılacak)
        var staticPrompt = Helper.ReadFileContent(PromptFolder, SystemPromptFileName);
        
        // DynamicPromptBuilder yoksa statik prompt'u kullan
        if (DynamicPromptBuilder == null)
        {
            Logger.LogDebug("DynamicPromptBuilder yok, statik prompt kullanılıyor");
            return staticPrompt;
        }
        
        try
        {
            // Dinamik prompt oluşturmayı dene
            var result = await DynamicPromptBuilder.BuildPromptAsync(userQuery, staticPrompt).ConfigureAwait(false);
            
            if (result.IsSuccess && !result.UsedFallback)
            {
                Logger.LogInformation(
                    "Dinamik prompt oluşturuldu - Tablolar: {Tables}, Kolonlar: {Columns}, ~{Tokens} token",
                    result.TableCount, result.ColumnCount, result.EstimatedTokens);
                
                // İlgili tabloları logla
                if (result.RelevantTables.Any())
                {
                    Logger.LogDebug(
                        "İlgili tablolar: {Tables}",
                        string.Join(", ", result.RelevantTables));
                }
                
                return result.Prompt;
            }
            
            // Fallback durumunda statik prompt kullan
            if (result.UsedFallback)
            {
                Logger.LogDebug("Neo4j Schema Catalog fallback, statik prompt kullanılıyor");
            }
            
            return staticPrompt;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Dinamik prompt oluşturulamadı, statik prompt kullanılıyor");
            return staticPrompt;
        }
    }

    /// <summary>
    /// OpenAI ayarlarını oluşturur. Farklı temperature/settings için override edilebilir.
    /// </summary>
    protected virtual OpenAIPromptExecutionSettings CreateOpenAiSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.1F
        };
    }

    /// <summary>
    /// Yanıtı parse eder ve HTML raporu üretir. Farklı JSON yapısı için override edilebilir.
    /// </summary>
    protected virtual async Task<Result<LLmResponseModel>> ParseResponseAsync(string result, ChatRequest request)
    {
        try
        {
            var llmResponse = await ExtractJsonFromResponse(result, request).ConfigureAwait(false);
            if (llmResponse == null)
            {
                return CreateErrorResponse("JSON yanıt işlenemedi.", request.ConversationId!);
            }

            var responseModel = new LLmResponseModel
            {
                ConversationId = request.ConversationId!,
                Summary = llmResponse.Summary,
            };

            var chatsModel = new GenerateHtmlModel
            {
                Instructions = SystemPrompt.Instructions
            };

            await ProcessReportsAsync(llmResponse, request, responseModel, chatsModel).ConfigureAwait(false);

            return Result<LLmResponseModel>.Success(responseModel, "report", "report");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Yanıt parse edilirken hata oluştu - ConversationId: {ConversationId}", request.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Yanıttan JSON'u çıkarır ve parse eder. Farklı JSON extraction mantığı için override edilebilir.
    /// </summary>
    protected virtual async Task<LLmResponseModel?> ExtractJsonFromResponse(string result, ChatRequest request)
    {
        try
        {
            var jsonStr = result;
            var match = JsonBlockRegex.Match(result);
            if (match.Success)
            {
                jsonStr = match.Groups[1].Value;
            }

            var jsonString = CleanJsonString(jsonStr);
            return JsonConvert.DeserializeObject<LLmResponseModel>(jsonString);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "JSON parse hatası: {JsonString}", result);
            await ConversationUseCase.AddUserMessageAsync(request, "JSON formatı bulunamadı. Tekrar dene ve Json formatında bir yanıt ver.", MessageType.Temporary).ConfigureAwait(false);
            throw new InvalidOperationException($"JSON formatı bulunamadı. AI Yanıtı: {result}");
        }
    }

    /// <summary>
    /// Raporları işler ve HTML üretir. Farklı rapor işleme mantığı için override edilebilir.
    /// </summary>
    protected virtual async Task ProcessReportsAsync(LLmResponseModel llmResponse, ChatRequest request, LLmResponseModel responseModel, GenerateHtmlModel chatsModel)
    {
        var reportIndex = 1;
        var htmlBuilder = new StringBuilder();

        var metadata = new Dictionary<string, object>
        {
            { "SignalRJsFunction", "None" },
            { "ReportServiceType", ReportServiceType },
            { "ReportDatabaseType", ReportDatabaseType },
            { "ReportDatabaseServiceType", ReportDatabaseServiceType }
        };

        try
        {
            await Task.Delay(DelayBetweenReports).ConfigureAwait(false);
            await SendLoadingMessage(request.ConnectionId, reportIndex++, llmResponse.Summary ?? "Rapor").ConfigureAwait(false);
            await ConversationUseCase.AddAssistantMessageAsync(request, llmResponse.Summary!, metadata).ConfigureAwait(false);

            // SQL Agent Pipeline ile SQL'i validate et ve optimize et
            var finalQuery = llmResponse.Query!;
            if (SqlAgentPipeline != null)
            {
                // ===== ReAct: OBSERVATION (Agent Pipeline) =====
                if (ReActUseCase != null)
                {
                    await ReActUseCase.SendObservationAsync(
                        request.ConnectionId,
                        "SQL sorgusu Agent Pipeline ile doğrulanıyor ve optimize ediliyor..."
                    ).ConfigureAwait(false);
                }

                finalQuery = await ProcessSqlWithAgentPipelineAsync(llmResponse.Query!, request).ConfigureAwait(false);
            }

            var historyQueryResponse = await ConversationUseCase.AddAssistantMessageAsync(request, finalQuery, metadata).ConfigureAwait(false);

            // Database sorgusu - ayrı try-catch ile
            DbResponseModel dbResponse;
            try
            {
                dbResponse = await DatabaseService.GetDataTableWithExpandoObjectAsync(finalQuery, request.ConversationId).ConfigureAwait(false);
            }
            catch (Exception dbEx)
            {
                Logger.LogError(dbEx, "SQL sorgusu çalıştırılırken hata oluştu - ConversationId: {ConversationId}, Query: {Query}",
                    request.ConversationId, finalQuery);

                // History'e hata mesajı ekle (SQL sorgusu ile birlikte)
                var errorMessage = $"""
                    Sql Sorgusu çalıştırılırken hata oluştu. SQL Server'da çalışacak şekilde sql sorgusu oluştur.
                    
                    Çalıştırılan SQL:
                    {finalQuery}
                    
                    Alınan hata: {dbEx.Message}
                    """;

                await ConversationUseCase.AddUserMessageAsync(
                    request,
                    errorMessage,
                    MessageType.Temporary).ConfigureAwait(false);

                throw; // Exception'ı yukarıya fırlat
            }

            responseModel.Data = dbResponse.Data;

            // Boş sonuç döndüyse LLM'e feedback verip yeni sorgu ürettir
            if (dbResponse.Count <= 0)
            {
                var retryResult = await TryRetryOnEmptyResultAsync(finalQuery, request, metadata).ConfigureAwait(false);
                if (retryResult == null)
                    return;

                (llmResponse, finalQuery, dbResponse) = retryResult.Value;
                responseModel.Data = dbResponse.Data;
            }
            
            var dataToon = ToonSerializer.Serialize(new { results = dbResponse.Data });
            await ConversationUseCase.AddAssistantMessageAsync(request, dataToon, new Dictionary<string, object> { { "IsDbResponse", true } }).ConfigureAwait(false);
            
            var htmlMessage = await ProcessSingleReport(llmResponse, dbResponse, chatsModel, Guid.Parse(request.ConversationId!), request.ConnectionId).ConfigureAwait(false);
            htmlBuilder.Append(htmlMessage);

            var apiResult = Result<LLmResponseModel>.Success(new LLmResponseModel
            {
                ConversationId = responseModel.ConversationId,
                MessageId = historyQueryResponse.MessageId.ToString(),
                Summary = llmResponse.Summary,
                HtmlMessage = htmlMessage,
                Suggestions = llmResponse.Suggestions
            }, "Rapor oluşturuldu", "LINK");

            // Frontend için OutputApiUrl'yi HtmlMessage alanına ata
            responseModel.HtmlMessage = htmlMessage;
            await ConversationUseCase.AddAssistantMessageAsync(request, JsonConvert.SerializeObject(apiResult), new Dictionary<string, object> { { "SignalRJsFunction", "ReceiveMessage" } }).ConfigureAwait(false);
            await SendReportMessage(request.ConnectionId, apiResult).ConfigureAwait(false);
            chatsModel.Data.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rapor işlenirken hata - ConversationId: {ConversationId}, Summary: {Summary}",
                request.ConversationId, llmResponse.Summary);
            throw;
        }
    }

    /// <summary>
    /// SQL sorgusunu Agent Pipeline üzerinden geçirerek validate ve optimize eder.
    /// Ayarlar (MultiAgentSettings) üzerinden kontrol edilir.
    /// </summary>
    protected virtual async Task<string> ProcessSqlWithAgentPipelineAsync(string sql, ChatRequest request)
    {
        // Pipeline veya ayarlar yoksa direkt SQL döndür
        if (SqlAgentPipeline == null || MultiAgentSettings == null)
            return sql;

        // Multi-Agent veya SQL Agents devre dışıysa direkt SQL döndür
        if (!MultiAgentSettings.Enabled || !MultiAgentSettings.SqlAgents.Enabled)
            return sql;

        try
        {
            Logger.LogInformation("SQL Agent Pipeline başlatılıyor - ConversationId: {ConversationId}", request.ConversationId);

            // Ayarlardan pipeline seçeneklerini al
            var sqlAgentSettings = MultiAgentSettings.SqlAgents;
            var pipelineOptions = new SqlPipelineOptions
            {
                EnableValidation = sqlAgentSettings.EnableValidation,
                EnableOptimization = sqlAgentSettings.EnableOptimization,
                EnableSecurityCheck = sqlAgentSettings.EnableSecurityCheck,
                EnableAutoCorrection = sqlAgentSettings.EnableAutoCorrection,
                MaxRetries = sqlAgentSettings.MaxRetries
            };

            var pipelineResult = await SqlAgentPipeline.ProcessAsync(
                sql,
                ReportDatabaseType, // "sqlserver"
                pipelineOptions).ConfigureAwait(false);

            // Pipeline sonucunu logla
            Logger.LogInformation(
                "SQL Agent Pipeline tamamlandı - Success: {IsSuccess}, ProcessingTime: {ProcessingTimeMs}ms, Summary: {Summary}",
                pipelineResult.IsSuccess,
                pipelineResult.ProcessingTimeMs,
                pipelineResult.Summary);

            if (!pipelineResult.IsSuccess)
            {
                Logger.LogWarning(
                    "SQL Agent Pipeline başarısız - Error: {Error}, ConversationId: {ConversationId}",
                    pipelineResult.ErrorMessage,
                    request.ConversationId);

                // Pipeline başarısız olursa orijinal SQL'i kullan
                // (fail-open yaklaşımı - sistemin çalışmaya devam etmesi için)
                return sql;
            }

            // SQL değiştiyse logla
            if (pipelineResult.FinalSql != sql)
            {
                Logger.LogInformation(
                    "SQL Agent Pipeline SQL'i güncelledi - Original: {Original}, Final: {Final}",
                    sql.Length > 100 ? sql[..100] + "..." : sql,
                    pipelineResult.FinalSql.Length > 100 ? pipelineResult.FinalSql[..100] + "..." : pipelineResult.FinalSql);
            }

            return pipelineResult.FinalSql;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SQL Agent Pipeline hatası - orijinal SQL kullanılacak. ConversationId: {ConversationId}", request.ConversationId);
            // Hata durumunda orijinal SQL'i kullan (fail-open)
            return sql;
        }
    }

    /// <summary>
    /// SQL sorgusu boş sonuç döndüğünde LLM'e feedback vererek yeni sorgu ürettirip tekrar çalıştırır.
    /// Başarılı olursa (llmResponse, finalQuery, dbResponse) döndürür; tüm denemeler boş kalırsa null döndürür.
    /// </summary>
    private async Task<(LLmResponseModel llmResponse, string finalQuery, DbResponseModel dbResponse)?> TryRetryOnEmptyResultAsync(
        string currentQuery, ChatRequest request, Dictionary<string, object> metadata)
    {
        var lastQuery = currentQuery;

        for (var attempt = 1; attempt <= MaxEmptyResultRetries; attempt++)
        {
            Logger.LogWarning(
                "SQL sorgusu boş sonuç döndü. Retry {Attempt}/{MaxRetries} - ConversationId: {ConversationId}",
                attempt, MaxEmptyResultRetries, request.ConversationId);

            // LLM'e boş sonuç bilgisini history'ye ekle
            await ConversationUseCase.AddUserMessageAsync(request, $"""
                Oluşturduğun SQL sorgusu çalıştırıldı ancak hiç sonuç döndürmedi (0 satır).
                Çalıştırılan SQL: {lastQuery}
                Lütfen kullanıcının isteğini tekrar analiz et.
                Olası sorunlar: WHERE koşulları çok kısıtlayıcı, kolon eşleştirmesi hatalı (Açıklama > Alias > Kolon Adı), JOIN/tarih uyumsuzluğu.
                """, MessageType.Temporary).ConfigureAwait(false);

            // ReAct bildirimi
            if (ReActUseCase != null)
                await ReActUseCase.SendObservationAsync(request.ConnectionId,
                    $"SQL sorgusu sonuç döndürmedi. Farklı sorgu deneniyor... ({attempt}/{MaxEmptyResultRetries})").ConfigureAwait(false);

            try
            {
                // LLM'den yeni sorgu al
                var chatHistory = await ConversationUseCase.GetChatHistoryAsync(request, includeDbResponses: false).ConfigureAwait(false);
                var llmResult = await ChatCompletionService.GetChatMessageContentsAsync(chatHistory!, CreateOpenAiSettings(), Kernel).ConfigureAwait(false);
                var llmResponse = await ExtractJsonFromResponse(llmResult[0].ToString()!, request).ConfigureAwait(false);

                if (llmResponse?.Query == null) continue;

                // SQL Agent Pipeline
                lastQuery = SqlAgentPipeline != null
                    ? await ProcessSqlWithAgentPipelineAsync(llmResponse.Query, request).ConfigureAwait(false)
                    : llmResponse.Query;

                await ConversationUseCase.AddAssistantMessageAsync(request, lastQuery, metadata).ConfigureAwait(false);

                // Veritabanında çalıştır
                var dbResponse = await DatabaseService.GetDataTableWithExpandoObjectAsync(lastQuery, request.ConversationId).ConfigureAwait(false);

                if (dbResponse.Count > 0)
                    return (llmResponse, lastQuery, dbResponse);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Empty result retry {Attempt} sırasında hata - ConversationId: {ConversationId}", attempt, request.ConversationId);
            }
        }

        Logger.LogWarning("SQL sorgusu {MaxRetries} retry sonrasında da boş döndü - ConversationId: {ConversationId}",
            MaxEmptyResultRetries, request.ConversationId);
        return null;
    }

    /// <summary>
    /// Tek bir raporu işler. Farklı HTML üretimi için override edilebilir.
    /// </summary>
    protected virtual async Task<string> ProcessSingleReport(LLmResponseModel item, DbResponseModel dbResponse,
        GenerateHtmlModel chatsModel, Guid conversationId, string connectionId = "")
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        
        // Tam veri modeli - DashboardUseCase'e gönderilecek (data.json için)
        var dataForHtmlModel = new DataForHtmlModel
        {
            Instructions = chatsModel.Instructions,
            UniqueId = uniqueId,
            Summary = item.Summary,
            Data = dbResponse.Data
        };

        var outputFolder = DashboardSettings?.OutputFolder ?? "output-folder";

        // Fast dashboard modu aktifse template-based yaklaşımı kullan
        if (DashboardSettings?.UseFastDashboard == true)
        {
            return await ProcessFastDashboard(dataForHtmlModel, conversationId, outputFolder).ConfigureAwait(false);
        }

        // LLM için optimize edilmiş veri - sadece şema + istatistik + örnek veri
        var llmOptimizedData = BuildLlmOptimizedData(chatsModel.Instructions, uniqueId, item.Summary, dbResponse.Data);
        var jsonDataForLlm = JsonConvert.SerializeObject(llmOptimizedData, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });
        
        Logger.LogInformation(
            "LLM için optimize edilmiş veri hazırlandı - TotalRecords: {TotalRecords}, SchemaFields: {SchemaFields}, SampleRows: {SampleRows}, JsonSize: {JsonSize} bytes",
            llmOptimizedData.TotalRecords,
            llmOptimizedData.DataSchema.Count,
            (llmOptimizedData.SampleData as IEnumerable<object>)?.Count() ?? 0,
            jsonDataForLlm.Length);

        // Insight analizi için ham veri (chunk-based analiz gerekebilir)
        var rawDataForInsight = JsonConvert.SerializeObject(new { 
            uniqueId, 
            summary = item.Summary,
            data = dbResponse.Data 
        }, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });

        // Dashboard HTML (şema-bazlı optimize veri) ve Insight HTML (ham veri - chunk analizi için) paralel üret
        // ===== ReAct: OBSERVATION (Dashboard + Insight başlatılıyor) =====
        if (ReActUseCase != null && !string.IsNullOrEmpty(connectionId))
        {
            await ReActUseCase.SendObservationAsync(
                connectionId,
                $"Dashboard ve AI veri analizi paralel olarak oluşturuluyor... (Veri: {dbResponse.Count} kayıt)"
            ).ConfigureAwait(false);
        }

        var dashboardTask = GenerateHtml(jsonDataForLlm, conversationId);
        var insightTask = GenerateInsightHtml(rawDataForInsight, uniqueId, conversationId, connectionId);
        
        await Task.WhenAll(dashboardTask, insightTask).ConfigureAwait(false);
        
        var htmlMessage = await dashboardTask;
        var insightHtml = await insightTask;
        
        // Dashboard işle - TAM VERİ ile (data.json için)
        // ===== ReAct: OBSERVATION (Dashboard + Insight tamamlandı) =====
        if (ReActUseCase != null && !string.IsNullOrEmpty(connectionId))
        {
            await ReActUseCase.SendObservationAsync(
                connectionId,
                "Dashboard ve AI veri analizi başarıyla tamamlandı."
            ).ConfigureAwait(false);
        }

        var dashboardProcessResult = await DashboardUseCase.ProcessDashboardResponse(
            htmlMessage, 
            dataForHtmlModel,  // Tam veri - data.json'a yazılacak
            outputFolder,
            insightHtml).ConfigureAwait(false);

        return dashboardProcessResult.OutputApiUrl;
    }

    /// <summary>
    /// Template-based hızlı dashboard işleme - LLM sadece JSON config üretir
    /// </summary>
    protected virtual async Task<string> ProcessFastDashboard(DataForHtmlModel dataForHtmlModel, Guid conversationId, string outputFolder)
    {
        try
        {
            var jsonData = JsonConvert.SerializeObject(dataForHtmlModel, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var config = await GenerateDashboardConfig(jsonData, conversationId).ConfigureAwait(false);
            
            if (config == null)
            {
                Logger.LogWarning("Fast dashboard config üretilemedi, legacy yönteme fallback - ConversationId: {ConversationId}", conversationId);
                var htmlMessage = await GenerateHtml(jsonData, conversationId).ConfigureAwait(false);
                var fallbackResult = await DashboardUseCase.ProcessDashboardResponse(htmlMessage, dataForHtmlModel, outputFolder).ConfigureAwait(false);
                return fallbackResult.OutputApiUrl;
            }

            var dashboardProcessResult = await DashboardUseCase.ProcessTemplateDashboard(config, dataForHtmlModel, outputFolder).ConfigureAwait(false);
            
            Logger.LogInformation("Fast dashboard oluşturuldu - ConversationId: {ConversationId}, ProcessingTime: {ProcessingTime}ms",
                conversationId, dashboardProcessResult.ProcessingTime.TotalMilliseconds);
            
            return dashboardProcessResult.OutputApiUrl;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fast dashboard hatası, legacy yönteme fallback - ConversationId: {ConversationId}", conversationId);
            // Fallback to legacy method
            var jsonData = JsonConvert.SerializeObject(dataForHtmlModel, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var htmlMessage = await GenerateHtml(jsonData, conversationId).ConfigureAwait(false);
            var fallbackResult = await DashboardUseCase.ProcessDashboardResponse(htmlMessage, dataForHtmlModel, outputFolder).ConfigureAwait(false);
            return fallbackResult.OutputApiUrl;
        }
    }

    /// <summary>
    /// LLM ile dashboard config JSON üretimi
    /// </summary>
    protected virtual async Task<DashboardConfig?> GenerateDashboardConfig(string jsonData, Guid conversationId)
    {
        try
        {
            var history = new ChatHistory();
            var promptFileName = DashboardSettings?.ConfigPromptFileName ?? DashboardConfigPromptFile;
            var systemPrompt = Helper.ReadFileContent(PromptFolder, promptFileName);
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(jsonData);

            var openAiSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = ChartGenerationTemperature
            };

            var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(
                history, openAiSettings, Kernel).ConfigureAwait(false);

            var responseText = resultPrompt[0].ToString()!;
            
            // JSON bloğunu parse et
            var jsonMatch = JsonBlockRegex.Match(responseText);
            var configJson = jsonMatch.Success ? jsonMatch.Groups[1].Value : responseText;
            
            var config = JsonConvert.DeserializeObject<DashboardConfig>(configJson);
            return config;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Dashboard config üretilirken hata oluştu - ConversationId: {ConversationId}", conversationId);
            return null;
        }
    }

    /// <summary>
    /// Chart/Dashboard HTML üretimi. Farklı dashboard prompt'u için override edilebilir.
    /// </summary>
    protected virtual async Task<string> GenerateHtml(string jsonData, Guid conversationId)
    {
        try
        {
            var history = new ChatHistory();
            var systemPrompt = Helper.ReadFileContent(PromptFolder, DashboardHtmlPromptFile);
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(jsonData);

            var openAiSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = ChartGenerationTemperature
            };

            var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(
                history, openAiSettings, Kernel).ConfigureAwait(false);

            return resultPrompt[0].ToString()!;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Chart üretilirken hata oluştu - ConversationId: {ConversationId}", conversationId);
            return string.Empty;
        }
    }

    /// <summary>
    /// AI Veri Analizi HTML üretimi - Dashboard ile paralel çalışır
    /// Büyük veri setleri için chunk-based paralel analiz yapar.
    /// </summary>
    protected virtual async Task<string> GenerateInsightHtml(string jsonData, string uniqueId, Guid conversationId, string connectionId = "")
    {
        try
        {
            // JSON'ı deserialize et ve veri boyutunu kontrol et
            var dataObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
            var data = dataObject?.data ?? dataObject?.Data ?? dataObject?.sampleData ?? dataObject?.SampleData;
            
            // Eğer LlmOptimizedData formatındaysa, doğrudan kullan (zaten optimize edilmiş)
            if (dataObject?.dataSchema != null || dataObject?.DataSchema != null)
            {
                // Şema bazlı veri - doğrudan final LLM'e gönder
                var totalRecs = (int)(dataObject?.totalRecords ?? dataObject?.TotalRecords ?? 0);
                var summary = dataObject?.summary?.ToString() ?? dataObject?.Summary?.ToString() ?? "Rapor Analizi";
                return await GenerateInsightHtmlDirect(jsonData, uniqueId, conversationId, summary, totalRecs).ConfigureAwait(false);
            }
            
            // Eğer ham data varsa, chunk-based analiz gerekli mi kontrol et
            if (data is Newtonsoft.Json.Linq.JArray dataArray && dataArray.Count > 0)
            {
                var recordCount = dataArray.Count;
                bool shouldChunk;
                
                if (UseTokenBasedChunking)
                {
                    // Token bazlı eşik: satır başına yaklaşık 100 token
                    var estimatedTokens = recordCount * TokensPerRowEstimate;
                    shouldChunk = estimatedTokens > TokenBasedChunkingThreshold;
                    
                    if (shouldChunk)
                    {
                        Logger.LogInformation(
                            "Chunk-based insight analizi başlatılıyor (Token modu) - RecordCount: {RecordCount}, EstimatedTokens: {EstimatedTokens}",
                            recordCount, estimatedTokens);
                    }
                }
                else
                {
                    // Kayıt bazlı eşik: 1000'den fazla kayıt varsa chunk yap
                    shouldChunk = recordCount > RecordBasedChunkingThreshold;
                    
                    if (shouldChunk)
                    {
                        Logger.LogInformation(
                            "Chunk-based insight analizi başlatılıyor (Kayıt modu) - RecordCount: {RecordCount}",
                            recordCount);
                    }
                }
                
                if (shouldChunk)
                {
                    return await GenerateInsightHtmlChunked(
                        dataArray.ToObject<List<dynamic>>()!,
                        uniqueId,
                        conversationId,
                        dataObject?.summary?.ToString() ?? "Rapor Analizi",
                        connectionId
                    ).ConfigureAwait(false);
                }
            }
            
            // Küçük veri seti - doğrudan analiz
            var userPrompt = dataObject?.summary?.ToString() ?? dataObject?.Summary?.ToString() ?? "Rapor Analizi";
            var totalRecords = data is Newtonsoft.Json.Linq.JArray arr ? arr.Count : 0;
            return await GenerateInsightHtmlDirect(jsonData, uniqueId, conversationId, userPrompt, totalRecords).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AI Veri Analizi HTML üretilirken hata oluştu - ConversationId: {ConversationId}", conversationId);
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Doğrudan (chunk'sız) insight HTML üretimi
    /// </summary>
    private async Task<string> GenerateInsightHtmlDirect(string jsonData, string uniqueId, Guid conversationId, string userPrompt = "Rapor Analizi", int totalRecords = 0)
    {
        // JSON'dan kayıt sayısını çıkar (eğer parametre olarak verilmediyse)
        if (totalRecords == 0)
        {
            try
            {
                var dataObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
                var data = dataObject?.data ?? dataObject?.Data ?? dataObject?.sampleData ?? dataObject?.SampleData;
                if (data is Newtonsoft.Json.Linq.JArray dataArray)
                {
                    totalRecords = dataArray.Count;
                }
                else if (dataObject?.totalRecords != null)
                {
                    totalRecords = (int)dataObject.totalRecords;
                }
                else if (dataObject?.TotalRecords != null)
                {
                    totalRecords = (int)dataObject.TotalRecords;
                }
            }
            catch
            {
                totalRecords = 0;
            }
        }
        
        // Prompt template'i oku ve placeholder'ları değiştir
        var promptTemplate = Helper.ReadFileContent(PromptFolder, InsightAnalysisPromptFile);
        var systemPrompt = promptTemplate
            .Replace("{{total_records}}", totalRecords.ToString())
            .Replace("{{chunk_count}}", "1")
            .Replace("{{user_prompt}}", userPrompt)
            .Replace("{{merged_data}}", "Doğrudan analiz - veri aşağıda")
            .Replace("{{critical_cases}}", "Kritik vaka analizi yapılacak")
            .Replace("{{unique_id}}", uniqueId);
        
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        
        // Veriyi user message olarak gönder
        var insightRequest = $@"Aşağıdaki veriyi analiz et ve HTML rapor oluştur:

{jsonData}";
        history.AddUserMessage(insightRequest);

        var openAiSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = ChartGenerationTemperature
        };

        var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(
            history, openAiSettings, Kernel).ConfigureAwait(false);

        var insightHtml = resultPrompt[0].ToString()!;
        
        // HTML bloklarını temizle (eğer ```html ... ``` içinde geldiyse)
        insightHtml = CleanHtmlResponse(insightHtml);
        
        Logger.LogInformation("AI Veri Analizi HTML üretildi (doğrudan) - UniqueId: {UniqueId}, TotalRecords: {TotalRecords}, ConversationId: {ConversationId}", 
            uniqueId, totalRecords, conversationId);
        
        return insightHtml;
    }
    
    /// <summary>
    /// Chunk-based paralel insight HTML üretimi
    /// Büyük veri setlerini parçalara ayırır, paralel analiz eder ve sonuçları birleştirir.
    /// </summary>
    private async Task<string> GenerateInsightHtmlChunked(
        List<dynamic> data,
        string uniqueId,
        Guid conversationId,
        string reportSummary,
        string connectionId = "")
    {
        try
        {
            // 1. Chunking ayarları - Moda göre hesapla
            int rowsPerChunk;
            int tokensPerRow;
            
            if (UseTokenBasedChunking)
            {
                // Token bazlı: dinamik satır sayısı hesapla
                tokensPerRow = EstimateTokensPerRow(data);
                rowsPerChunk = Math.Max(200, TargetTokensPerChunk / tokensPerRow);
                Logger.LogDebug("Token bazlı chunking - TokensPerRow: {TokensPerRow}, RowsPerChunk: {RowsPerChunk}", 
                    tokensPerRow, rowsPerChunk);
            }
            else
            {
                // Kayıt bazlı: sabit 1000 kayıt per chunk
                rowsPerChunk = RecordsPerChunk;
                tokensPerRow = TokensPerRowEstimate; // Log ve EstimatedTokens için varsayılan
                Logger.LogDebug("Kayıt bazlı chunking - RowsPerChunk: {RowsPerChunk}", rowsPerChunk);
            }
            
            var totalChunks = (int)Math.Ceiling(data.Count / (double)rowsPerChunk);
            
            // Sampling gerekli mi?
            var samplingRate = 1.0;
            IList<dynamic> workingData = data;
            
            if (totalChunks > MaxChunks)
            {
                var targetRecords = MaxChunks * rowsPerChunk;
                samplingRate = targetRecords / (double)data.Count;
                workingData = ApplyStratifiedSampling(data, targetRecords);
                totalChunks = MaxChunks;
                
                Logger.LogInformation(
                    "Sampling uygulandı - OriginalCount: {Original}, SampledCount: {Sampled}, Rate: {Rate:P0}",
                    data.Count, workingData.Count, samplingRate);
            }
            
            // 2. Veri şemasını çıkar
            var dataSchema = ExtractDataSchema(workingData);
            
            // 3. Chunk'lara ayır
            var chunks = new List<DataChunk>();
            var chunkIndex = 0;
            for (var i = 0; i < workingData.Count; i += rowsPerChunk)
            {
                chunkIndex++;
                var chunkData = workingData.Skip(i).Take(rowsPerChunk).ToList();
                chunks.Add(new DataChunk
                {
                    Index = chunkIndex,
                    TotalChunks = totalChunks,
                    Data = chunkData,
                    RecordCount = chunkData.Count,
                    EstimatedTokens = chunkData.Count * tokensPerRow
                });
            }
            
            Logger.LogInformation(
                "Chunk'lar oluşturuldu - Mode: {Mode}, TotalRecords: {TotalRecords}, ChunkCount: {ChunkCount}, RowsPerChunk: {RowsPerChunk}",
                UseTokenBasedChunking ? "Token" : "Record", data.Count, chunks.Count, rowsPerChunk);
            
            // 4. Paralel chunk analizi
            var chunkSummaries = await AnalyzeChunksParallel(chunks, reportSummary, dataSchema, data.Count, conversationId, connectionId).ConfigureAwait(false);
            
            // 5. Sonuçları birleştir
            var aggregatedData = AggregateChunkResults(chunkSummaries, data.Count);
            
            // Progress: Final analiz başlıyor
            await SendProgressAsync(connectionId, new AnalysisProgress
            {
                Stage = "FinalAnalysis",
                TotalChunks = chunks.Count,
                CompletedChunks = chunks.Count,
                PercentComplete = 95,
                Message = "Sonuçlar birleştiriliyor ve final analiz yapılıyor..."
            }).ConfigureAwait(false);
            
            // 6. Final LLM analizi
            var finalInsightHtml = await GenerateFinalInsightHtml(aggregatedData, uniqueId, conversationId, reportSummary).ConfigureAwait(false);
            
            Logger.LogInformation(
                "Chunk-based insight analizi tamamlandı - UniqueId: {UniqueId}, ChunkCount: {ChunkCount}, ConversationId: {ConversationId}",
                uniqueId, chunks.Count, conversationId);
            
            return finalInsightHtml;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Chunk-based insight analizi sırasında hata - ConversationId: {ConversationId}", conversationId);
            // Fallback: Doğrudan analiz dene (sample data ile)
            var sampleData = data.Take(1000).ToList();
            var sampleJson = JsonConvert.SerializeObject(new { uniqueId, data = sampleData });
            return await GenerateInsightHtmlDirect(sampleJson, uniqueId, conversationId, reportSummary, data.Count).ConfigureAwait(false);
        }
    }
    
    /// <summary>
    /// Satır başına tahmini token sayısı
    /// </summary>
    private int EstimateTokensPerRow(IList<dynamic> data)
    {
        if (data == null || data.Count == 0) return 100;
        
        var sampleCount = Math.Min(10, data.Count);
        var totalChars = 0;
        
        foreach (var row in data.Take(sampleCount))
        {
            try
            {
                var json = JsonConvert.SerializeObject(row);
                totalChars += json.Length;
            }
            catch
            {
                totalChars += 200;
            }
        }
        
        var avgCharsPerRow = totalChars / (double)sampleCount;
        return Math.Max(10, (int)Math.Ceiling(avgCharsPerRow * 0.3)); // ~0.3 token per char
    }
    
    /// <summary>
    /// Stratified sampling uygular
    /// </summary>
    private IList<dynamic> ApplyStratifiedSampling(IList<dynamic> data, int targetCount)
    {
        var result = new List<dynamic>();
        var stratumCount = Math.Max(3, Math.Min(10, targetCount / 100));
        var stratumSize = data.Count / stratumCount;
        var samplesPerStratum = targetCount / stratumCount;
        var random = new Random(42); // Reproducible
        
        for (var s = 0; s < stratumCount; s++)
        {
            var startIdx = s * stratumSize;
            var endIdx = (s == stratumCount - 1) ? data.Count : (s + 1) * stratumSize;
            var stratum = data.Skip(startIdx).Take(endIdx - startIdx).ToList();
            var samples = stratum.OrderBy(_ => random.Next()).Take(samplesPerStratum);
            result.AddRange(samples);
        }
        
        return result.Take(targetCount).ToList();
    }
    
    /// <summary>
    /// Veri şemasını çıkarır
    /// </summary>
    private List<FieldSchemaInfo> ExtractDataSchema(IList<dynamic> data)
    {
        var schema = new List<FieldSchemaInfo>();
        if (data == null || data.Count == 0) return schema;
        
        var firstRow = data[0];
        if (firstRow is Newtonsoft.Json.Linq.JObject jObj)
        {
            foreach (var prop in jObj.Properties())
            {
                schema.Add(new FieldSchemaInfo
                {
                    FieldName = prop.Name,
                    FieldType = InferFieldType(prop.Value)
                });
            }
        }
        else if (firstRow is IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                schema.Add(new FieldSchemaInfo
                {
                    FieldName = kvp.Key,
                    FieldType = InferFieldType(kvp.Value)
                });
            }
        }
        
        return schema;
    }
    
    /// <summary>
    /// Değerden alan tipini çıkarır
    /// </summary>
    private string InferFieldType(object? value)
    {
        if (value == null) return "String";
        
        var valueStr = value.ToString();
        if (string.IsNullOrEmpty(valueStr)) return "String";
        
        if (value is Newtonsoft.Json.Linq.JValue jVal)
        {
            return jVal.Type switch
            {
                Newtonsoft.Json.Linq.JTokenType.Integer => "Integer",
                Newtonsoft.Json.Linq.JTokenType.Float => "Decimal",
                Newtonsoft.Json.Linq.JTokenType.Boolean => "Boolean",
                Newtonsoft.Json.Linq.JTokenType.Date => "DateTime",
                _ => "String"
            };
        }
        
        if (int.TryParse(valueStr, out _)) return "Integer";
        if (decimal.TryParse(valueStr, out _)) return "Decimal";
        if (DateTime.TryParse(valueStr, out _)) return "DateTime";
        
        return "String";
    }
    
    /// <summary>
    /// Chunk'ları paralel analiz eder
    /// </summary>
    private async Task<List<ChunkSummary>> AnalyzeChunksParallel(
        List<DataChunk> chunks,
        string reportSummary,
        List<FieldSchemaInfo> dataSchema,
        int totalRecords,
        Guid conversationId,
        string connectionId = "")
    {
        var semaphore = new SemaphoreSlim(MaxParallelism);
        var results = new ChunkSummary?[chunks.Count];
        var completedCount = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // İlk progress bildirimi
        await SendProgressAsync(connectionId, new AnalysisProgress
        {
            Stage = "ChunkAnalysis",
            TotalChunks = chunks.Count,
            CompletedChunks = 0,
            PercentComplete = 0,
            Message = $"Veri analizi başlıyor ({chunks.Count} parça)..."
        }).ConfigureAwait(false);
        
        var tasks = chunks.Select(async (chunk, index) =>
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // AnalyzeSingleChunk kendi içinde retry yapar
                var summary = await AnalyzeSingleChunk(chunk, reportSummary, dataSchema, totalRecords).ConfigureAwait(false);
                results[index] = summary;
                
                // Progress güncelle
                var completed = Interlocked.Increment(ref completedCount);
                var percentComplete = (int)(completed * 90.0 / chunks.Count); // %0-90 arası (final analiz için %10 bırak)
                var avgTimePerChunk = sw.ElapsedMilliseconds / completed;
                var remainingChunks = chunks.Count - completed;
                var estimatedSeconds = (int)((remainingChunks * avgTimePerChunk) / 1000);
                
                await SendProgressAsync(connectionId, new AnalysisProgress
                {
                    Stage = "ChunkAnalysis",
                    CurrentChunk = chunk.Index,
                    TotalChunks = chunks.Count,
                    CompletedChunks = completed,
                    PercentComplete = percentComplete,
                    EstimatedSecondsRemaining = estimatedSeconds,
                    Message = $"Parça {completed}/{chunks.Count} tamamlandı..."
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Bu noktaya gelmesi beklenmez (AnalyzeSingleChunk boş summary döndürür)
                // Ama yine de güvenlik için yakalıyoruz
                Logger.LogError(ex, "Chunk {Index} analizi beklenmeyen hata", chunk.Index);
                results[index] = CreateEmptyChunkSummary(chunk.Index, chunk.RecordCount);
                Interlocked.Increment(ref completedCount);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
        
        return results.Where(r => r != null).Cast<ChunkSummary>().ToList();
    }
    
    /// <summary>
    /// Tek bir chunk'ı analiz eder - Retry mekanizması ile
    /// </summary>
    private async Task<ChunkSummary> AnalyzeSingleChunk(
        DataChunk chunk,
        string reportSummary,
        List<FieldSchemaInfo> dataSchema,
        int totalRecords)
    {
        Exception? lastException = null;
        
        for (var attempt = 1; attempt <= MaxChunkRetryAttempts; attempt++)
        {
            try
            {
                var prompt = BuildChunkAnalysisPrompt(chunk, reportSummary, dataSchema, totalRecords);
                
                var history = new ChatHistory();
                history.AddSystemMessage(GetChunkAnalysisSystemPrompt());
                history.AddUserMessage(prompt);
                
                var settings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.1
                };
                
                var response = await ChatCompletionService.GetChatMessageContentsAsync(
                    history, settings, Kernel).ConfigureAwait(false);
                
                var responseText = response[0].ToString();
                var result = ParseChunkSummary(responseText ?? "", chunk.Index, chunk.RecordCount);
                
                if (attempt > 1)
                {
                    Logger.LogInformation("Chunk {Index} analizi {Attempt}. denemede başarılı oldu", chunk.Index, attempt);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.LogWarning(ex, "Chunk {Index} analizi başarısız (Deneme {Attempt}/{MaxRetries})", 
                    chunk.Index, attempt, MaxChunkRetryAttempts);
                
                if (attempt < MaxChunkRetryAttempts)
                {
                    // Exponential backoff
                    var delay = ChunkRetryBaseDelayMs * attempt;
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
        }
        
        // Tüm denemeler başarısız olduysa boş summary döndür
        Logger.LogError(lastException, "Chunk {Index} analizi {MaxRetries} denemeden sonra başarısız oldu", 
            chunk.Index, MaxChunkRetryAttempts);
        return CreateEmptyChunkSummary(chunk.Index, chunk.RecordCount);
    }
    
    /// <summary>
    /// Chunk analiz system prompt'u - Minimal, ana prompt user message'da
    /// </summary>
    private static string GetChunkAnalysisSystemPrompt()
    {
        return @"Sen bir veri analiz uzmanısın. Verilen veri parçasını analiz edecek ve sadece JSON formatında çıktı üreteceksin. JSON'u ```json ``` bloğu içinde döndür.";
    }
    
    /// <summary>
    /// Chunk analiz prompt'unu oluşturur
    /// Prompt'u .md dosyasından okur ve placeholder'ları değiştirir
    /// </summary>
    private string BuildChunkAnalysisPrompt(DataChunk chunk, string reportSummary, List<FieldSchemaInfo> dataSchema, int totalRecords)
    {
        // TOON formatı JSON'a göre ~%40-60 daha az token kullanır
        var dataToon = ToonSerializer.Serialize(new { results = chunk.Data });
        
        // Prompt'u dosyadan oku
        var promptTemplate = Helper.ReadFileContent(PromptFolder, ChunkAnalysisPromptFile);
        
        // Placeholder'ları değiştir
        var prompt = promptTemplate
            .Replace("{{total_records}}", totalRecords.ToString())
            .Replace("{{chunk_number}}", chunk.Index.ToString())
            .Replace("{{chunk_size}}", chunk.RecordCount.ToString())
            .Replace("{{user_prompt}}", reportSummary);
        
        // Veriyi TOON formatında sona ekle
        prompt += $"\n\n## Veri (TOON format)\n{dataToon}";
        
        return prompt;
    }
    
    /// <summary>
    /// Chunk summary'i parse eder
    /// </summary>
    private ChunkSummary ParseChunkSummary(string responseText, int chunkIndex, int recordCount)
    {
        try
        {
            var jsonText = ExtractJsonFromResponse(responseText);
            if (string.IsNullOrWhiteSpace(jsonText))
                return CreateEmptyChunkSummary(chunkIndex, recordCount);
            
            var summary = System.Text.Json.JsonSerializer.Deserialize<ChunkSummary>(jsonText,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (summary == null)
                return CreateEmptyChunkSummary(chunkIndex, recordCount);
            
            summary.ChunkIndex = chunkIndex;
            summary.RecordCount = recordCount;
            return summary;
        }
        catch
        {
            return CreateEmptyChunkSummary(chunkIndex, recordCount);
        }
    }
    
    /// <summary>
    /// Response'tan JSON çıkarır
    /// </summary>
    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return string.Empty;
        
        var jsonMatch = Regex.Match(response, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        if (jsonMatch.Success) return jsonMatch.Groups[1].Value.Trim();
        
        var trimmed = response.Trim();
        if (trimmed.StartsWith("{") && trimmed.EndsWith("}")) return trimmed;
        
        return string.Empty;
    }
    
    /// <summary>
    /// Boş chunk summary oluşturur
    /// </summary>
    private static ChunkSummary CreateEmptyChunkSummary(int chunkIndex, int recordCount)
    {
        return new ChunkSummary
        {
            ChunkIndex = chunkIndex,
            RecordCount = recordCount,
            Statistics = new ChunkStatistics(),
            Rankings = new ChunkRankings(),
            Patterns = new List<string>(),
            Anomalies = new List<ChunkAnomaly>(),
            Insights = new List<string>()
        };
    }
    
    /// <summary>
    /// Chunk sonuçlarını birleştirir - Tema, mağaza, metrik birleştirme desteği
    /// </summary>
    private AggregatedInsightData AggregateChunkResults(
        List<ChunkSummary> chunkSummaries,
        int totalRecords)
    {
        var result = new AggregatedInsightData
        {
            TotalRecords = totalRecords,
            ChunkCount = chunkSummaries.Count,
            ChunkSummaries = chunkSummaries,
            AllThemes = new Dictionary<string, MergedTheme>(),
            AllCriticalCases = new List<CriticalCase>(),
            AllPatterns = new List<string>(),
            CategoryMetrics = new Dictionary<string, int>(),
            SeverityMetrics = new Dictionary<string, int>(),
            StoresMentioned = new Dictionary<string, StoreInfo>()
        };
        
        foreach (var chunk in chunkSummaries)
        {
            // Temaları birleştir
            if (chunk.Themes != null)
            {
                foreach (var theme in chunk.Themes)
                {
                    var normalizedName = NormalizeThemeName(theme.Name ?? "Diğer");
                    
                    if (!result.AllThemes.ContainsKey(normalizedName))
                    {
                        result.AllThemes[normalizedName] = new MergedTheme
                        {
                            Name = normalizedName,
                            TotalCount = 0,
                            Severity = theme.Severity ?? "medium",
                            Keywords = new HashSet<string>(),
                            Examples = new List<string>(),
                            ChunksFound = new List<int>()
                        };
                    }
                    
                    var mergedTheme = result.AllThemes[normalizedName];
                    mergedTheme.TotalCount += theme.Count;
                    mergedTheme.ChunksFound.Add(chunk.ChunkId);
                    
                    // En yüksek severity'yi al
                    if (GetSeverityLevel(theme.Severity) > GetSeverityLevel(mergedTheme.Severity))
                    {
                        mergedTheme.Severity = theme.Severity ?? "medium";
                    }
                    
                    if (theme.Keywords != null)
                    {
                        foreach (var kw in theme.Keywords)
                        {
                            mergedTheme.Keywords.Add(kw);
                        }
                    }
                    
                    if (theme.RepresentativeExamples != null)
                    {
                        mergedTheme.Examples.AddRange(theme.RepresentativeExamples.Take(2));
                    }
                }
            }
            
            // Kritik vakaları topla
            if (chunk.CriticalCases != null)
            {
                result.AllCriticalCases.AddRange(chunk.CriticalCases);
            }
            
            // Pattern'ları topla
            if (chunk.Patterns != null)
            {
                result.AllPatterns.AddRange(chunk.Patterns);
            }
            
            // Metrikleri topla
            if (chunk.Metrics?.ByCategory != null)
            {
                foreach (var kvp in chunk.Metrics.ByCategory)
                {
                    if (!result.CategoryMetrics.ContainsKey(kvp.Key))
                        result.CategoryMetrics[kvp.Key] = 0;
                    result.CategoryMetrics[kvp.Key] += kvp.Value;
                }
            }
            
            if (chunk.Metrics?.BySeverity != null)
            {
                foreach (var kvp in chunk.Metrics.BySeverity)
                {
                    if (!result.SeverityMetrics.ContainsKey(kvp.Key))
                        result.SeverityMetrics[kvp.Key] = 0;
                    result.SeverityMetrics[kvp.Key] += kvp.Value;
                }
            }
            
            // Mağaza bilgilerini topla
            if (chunk.Entities?.StoresMentioned != null)
            {
                foreach (var store in chunk.Entities.StoresMentioned)
                {
                    var storeName = store.Name ?? "Bilinmeyen";
                    if (!result.StoresMentioned.ContainsKey(storeName))
                    {
                        result.StoresMentioned[storeName] = new StoreInfo
                        {
                            Name = storeName,
                            Count = 0,
                            MainIssues = new HashSet<string>()
                        };
                    }
                    result.StoresMentioned[storeName].Count += store.Count;
                    if (store.MainIssues != null)
                    {
                        foreach (var issue in store.MainIssues)
                        {
                            result.StoresMentioned[storeName].MainIssues.Add(issue);
                        }
                    }
                }
            }
        }
        
        // Pattern'ları deduplicate et ve frekansa göre sırala
        result.AllPatterns = result.AllPatterns
            .GroupBy(p => p.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Select(g => g.First())
            .Take(30)
            .ToList();
        
        // En kritik vakaları seç (maksimum 20)
        result.AllCriticalCases = result.AllCriticalCases.Take(20).ToList();
        
        return result;
    }
    
    /// <summary>
    /// Tema isimlerini normalize eder (benzer isimleri birleştirir)
    /// </summary>
    private static string NormalizeThemeName(string name)
    {
        var normalized = name.ToLowerInvariant().Trim();
        
        // Static mapping'den kontrol et
        foreach (var (themeName, keywords) in ThemeMappings)
        {
            if (keywords.Any(k => normalized.Contains(k)))
            {
                return themeName;
            }
        }
        
        // Başlık formatına çevir
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
    }
    
    /// <summary>
    /// Severity seviyesini sayısal değere çevirir
    /// </summary>
    private static int GetSeverityLevel(string? severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }
    
    /// <summary>
    /// Final insight HTML üretimi - chunk_merge_prompt.md benzeri placeholder yaklaşımı
    /// </summary>
    private async Task<string> GenerateFinalInsightHtml(
        AggregatedInsightData aggregatedData,
        string uniqueId,
        Guid conversationId,
        string userPrompt)
    {
        // Merged data'yı toon formatında formatla
        var mergedDataText = FormatMergedDataForPrompt(aggregatedData);
        var criticalCasesText = FormatCriticalCasesForPrompt(aggregatedData);
        
        // Prompt template'i oku ve placeholder'ları değiştir
        var promptTemplate = Helper.ReadFileContent(PromptFolder, InsightAnalysisPromptFile);
        var systemPrompt = promptTemplate
            .Replace("{{total_records}}", aggregatedData.TotalRecords.ToString())
            .Replace("{{chunk_count}}", aggregatedData.ChunkCount.ToString())
            .Replace("{{user_prompt}}", userPrompt)
            .Replace("{{merged_data}}", mergedDataText)
            .Replace("{{critical_cases}}", criticalCasesText)
            .Replace("{{unique_id}}", uniqueId);
        
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage("Yukarıdaki birleştirilmiş verileri kullanarak kapsamlı HTML analiz raporunu oluştur.");

        var openAiSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = ChartGenerationTemperature
        };

        var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(
            history, openAiSettings, Kernel).ConfigureAwait(false);

        var insightHtml = resultPrompt[0].ToString()!;
        insightHtml = CleanHtmlResponse(insightHtml);
        
        Logger.LogInformation(
            "Final insight HTML üretildi (chunk-based) - UniqueId: {UniqueId}, ConversationId: {ConversationId}",
            uniqueId, conversationId);
        
        return insightHtml;
    }
    
    /// <summary>
    /// Aggregated data'yı toon formatında prompt için formatlar
    /// ToonSharp kütüphanesi kullanılarak serialize edilir
    /// </summary>
    private static string FormatMergedDataForPrompt(AggregatedInsightData data)
    {
        // Toon formatı için veri objesi oluştur
        var mergedDataObject = new
        {
            genel_bilgiler = new
            {
                toplam_kayit = data.TotalRecords,
                chunk_sayisi = data.ChunkCount
            },
            temalar = data.AllThemes?.OrderByDescending(t => t.Value.TotalCount)
                .Take(15)
                .Select(t => new
                {
                    tema = t.Value.Name,
                    adet = t.Value.TotalCount,
                    severity = t.Value.Severity,
                    anahtar_kelimeler = t.Value.Keywords.Take(10).ToList(),
                    ornekler = t.Value.Examples.Take(3).ToList(),
                    parca_sayisi = t.Value.ChunksFound.Distinct().Count()
                }).ToList(),
            kategori_metrikleri = data.CategoryMetrics?.OrderByDescending(k => k.Value).Take(10).ToDictionary(k => k.Key, k => k.Value),
            severity_metrikleri = data.SeverityMetrics?.ToDictionary(k => k.Key, k => k.Value),
            magazalar = data.StoresMentioned?.OrderByDescending(s => s.Value.Count)
                .Take(10)
                .Select(s => new
                {
                    magaza = s.Value.Name,
                    sikayet_sayisi = s.Value.Count,
                    ana_sorunlar = s.Value.MainIssues.Take(3).ToList()
                }).ToList(),
            patternlar = data.AllPatterns?.Take(15).ToList() ?? new List<string>(),
            tum_icgoruler = data.ChunkSummaries?
                .SelectMany(c => c.Insights ?? new List<string>())
                .Distinct()
                .Take(20)
                .ToList() ?? new List<string>()
        };
        
        var dataToon = ToonSerializer.Serialize(mergedDataObject);
        return dataToon;
    }
    
    /// <summary>
    /// Kritik vakaları prompt için formatlar
    /// </summary>
    private static string FormatCriticalCasesForPrompt(AggregatedInsightData data)
    {
        var sb = new StringBuilder();
        
        // Kritik vakaları formatla
        if (data.AllCriticalCases != null && data.AllCriticalCases.Count > 0)
        {
            sb.AppendLine("### Kritik Vakalar:");
            var index = 1;
            foreach (var c in data.AllCriticalCases.Take(10))
            {
                sb.AppendLine($"{index}. [{c.Category ?? "Genel"}] {c.Text}");
                sb.AppendLine($"   Neden kritik: {c.Reason}");
                index++;
            }
            sb.AppendLine();
        }
        
        if (sb.Length == 0)
            return "Kritik vaka tespit edilmedi.";
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Veritabanı verisinden LLM için optimize edilmiş veri yapısı oluşturur.
    /// Tam veri yerine şema + istatistik + örnek veri döndürür.
    /// Token kullanımını %90-95 azaltır.
    /// </summary>
    /// <param name="instructions">Talimatlar</param>
    /// <param name="uniqueId">Benzersiz ID</param>
    /// <param name="summary">Rapor özeti</param>
    /// <param name="data">Veritabanından gelen tam veri</param>
    /// <param name="sampleSize">Örnek veri satır sayısı (varsayılan 20)</param>
    /// <returns>LLM için optimize edilmiş veri</returns>
    protected virtual LlmOptimizedData BuildLlmOptimizedData(
        string? instructions, 
        string uniqueId, 
        string? summary, 
        dynamic? data,
        int sampleSize = 20)
    {
        var result = new LlmOptimizedData
        {
            Instructions = instructions,
            UniqueId = uniqueId,
            Summary = summary,
            TotalRecords = 0,
            DataSchema = new List<FieldSchema>(),
            SampleData = null
        };

        if (data == null)
            return result;

        try
        {
            // Data'yı List<dynamic> olarak işle
            var dataList = data as IEnumerable<object>;
            if (dataList == null)
                return result;

            var itemsList = dataList.ToList();
            result.TotalRecords = itemsList.Count;

            if (itemsList.Count == 0)
                return result;

            // İlk öğeden şemayı çıkar
            var firstItem = itemsList.First();
            var properties = GetDynamicProperties(firstItem);

            // Her alan için şema ve istatistik hesapla
            foreach (var propName in properties)
            {
                var fieldSchema = new FieldSchema
                {
                    FieldName = propName,
                    SampleValues = new List<string>()
                };

                // Tüm değerleri topla (null olmayanlar)
                var values = itemsList
                    .Select(item => GetPropertyValue(item, propName))
                    .Where(v => v != null)
                    .ToList();

                if (values.Count == 0)
                {
                    fieldSchema.FieldType = "String";
                    result.DataSchema.Add(fieldSchema);
                    continue;
                }

                // Tip belirleme ve istatistik hesaplama
                var firstValue = values.First();
                var fieldType = DetermineFieldType(firstValue);
                fieldSchema.FieldType = fieldType;

                if (fieldType == "Number")
                {
                    // Sayısal istatistikler
                    var numericValues = values
                        .Select(v => ConvertToDouble(v))
                        .Where(v => v.HasValue)
                        .Select(v => v!.Value)
                        .ToList();

                    if (numericValues.Count > 0)
                    {
                        fieldSchema.Min = Math.Round(numericValues.Min(), 2);
                        fieldSchema.Max = Math.Round(numericValues.Max(), 2);
                        fieldSchema.Avg = Math.Round(numericValues.Average(), 2);
                        fieldSchema.Sum = Math.Round(numericValues.Sum(), 2);
                    }

                    // Örnek değerler
                    fieldSchema.SampleValues = values.Take(5).Select(v => v?.ToString() ?? "").ToList();
                }
                else if (fieldType == "DateTime")
                {
                    // Tarih alanları için örnek değerler
                    fieldSchema.SampleValues = values.Take(5).Select(v => v?.ToString() ?? "").ToList();
                    
                    // Tarih aralığı
                    var dateValues = values
                        .Select(v => ConvertToDateTime(v))
                        .Where(v => v.HasValue)
                        .Select(v => v!.Value)
                        .ToList();

                    if (dateValues.Count > 0)
                    {
                        fieldSchema.SampleValues = new List<string>
                        {
                            $"Min: {dateValues.Min():yyyy-MM-dd}",
                            $"Max: {dateValues.Max():yyyy-MM-dd}"
                        };
                    }
                }
                else
                {
                    // String/kategori alanları için benzersiz değerler
                    var distinctValues = values
                        .Select(v => v?.ToString() ?? "")
                        .Where(v => !string.IsNullOrEmpty(v))
                        .Distinct()
                        .ToList();

                    fieldSchema.DistinctCount = distinctValues.Count;
                    
                    // Benzersiz değer sayısı az ise (kategorik alan) tümünü göster
                    if (distinctValues.Count <= 20)
                    {
                        fieldSchema.DistinctValues = distinctValues;
                    }
                    else
                    {
                        // Çok fazla benzersiz değer varsa sadece ilk 20'yi göster
                        fieldSchema.DistinctValues = distinctValues.Take(20).ToList();
                    }

                    // Örnek değerler
                    fieldSchema.SampleValues = values.Take(5).Select(v => v?.ToString() ?? "").ToList();
                }

                result.DataSchema.Add(fieldSchema);
            }

            // Örnek veri - ilk N satır
            result.SampleData = itemsList.Take(sampleSize).ToList();

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "LLM optimize veri oluşturulurken hata, boş şema döndürülüyor");
            return result;
        }
    }

    /// <summary>
    /// Dynamic nesnenin property isimlerini alır
    /// </summary>
    private static IEnumerable<string> GetDynamicProperties(object obj)
    {
        if (obj is IDictionary<string, object> dict)
        {
            return dict.Keys;
        }
        
        if (obj is System.Dynamic.ExpandoObject expando)
        {
            return ((IDictionary<string, object?>)expando).Keys;
        }

        // Normal object için reflection
        return obj.GetType().GetProperties().Select(p => p.Name);
    }

    /// <summary>
    /// Dynamic nesneden property değerini alır
    /// </summary>
    private static object? GetPropertyValue(object obj, string propertyName)
    {
        try
        {
            if (obj is IDictionary<string, object> dict)
            {
                return dict.TryGetValue(propertyName, out var value) ? value : null;
            }

            if (obj is System.Dynamic.ExpandoObject expando)
            {
                var expandoDict = (IDictionary<string, object?>)expando;
                return expandoDict.TryGetValue(propertyName, out var value) ? value : null;
            }

            // Normal object için reflection
            var prop = obj.GetType().GetProperty(propertyName);
            return prop?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Değerin tipini belirler
    /// </summary>
    private static string DetermineFieldType(object? value)
    {
        if (value == null) return "String";

        var type = value.GetType();
        
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return "DateTime";
        
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(decimal) || type == typeof(double) || type == typeof(float) ||
            type == typeof(byte) || type == typeof(uint) || type == typeof(ulong))
            return "Number";
        
        if (type == typeof(bool))
            return "Boolean";

        // String olarak gelen sayısal değerleri kontrol et
        if (value is string strValue)
        {
            if (double.TryParse(strValue, out _))
                return "Number";
            if (DateTime.TryParse(strValue, out _))
                return "DateTime";
        }

        return "String";
    }

    /// <summary>
    /// Değeri double'a çevirir
    /// </summary>
    private static double? ConvertToDouble(object? value)
    {
        if (value == null) return null;

        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            if (value is string strValue && double.TryParse(strValue, out var result))
                return result;
            return null;
        }
    }

    /// <summary>
    /// Değeri DateTime'a çevirir
    /// </summary>
    private static DateTime? ConvertToDateTime(object? value)
    {
        if (value == null) return null;

        try
        {
            if (value is DateTime dt) return dt;
            if (value is DateTimeOffset dto) return dto.DateTime;
            if (value is string strValue && DateTime.TryParse(strValue, out var result))
                return result;
            return Convert.ToDateTime(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// LLM yanıtından HTML bloğunu temizler
    /// </summary>
    private static string CleanHtmlResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
            return response;
            
        // ```html ... ``` bloğunu temizle
        var htmlBlockRegex = new Regex(@"```html\s*(.*?)\s*```", RegexOptions.Singleline);
        var match = htmlBlockRegex.Match(response);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        
        // ``` ... ``` bloğunu temizle
        var codeBlockRegex = new Regex(@"```\s*(.*?)\s*```", RegexOptions.Singleline);
        match = codeBlockRegex.Match(response);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        
        return response.Trim();
    }

    #endregion

    #region Private Methods (Override edilemez)

    /// <summary>
    /// Exponential backoff ile retry mekanizması
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, int, int, Task>? onRetryAsync,
        string connectionId,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var exceptions = new List<Exception>();

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts)
            {
                exceptions.Add(ex);

                var delay = CalculateDelay(attempt);

                Logger.LogWarning(
                    ex,
                    "{OperationName} başarısız oldu. Deneme: {Attempt}/{MaxRetries}, " +
                    "Bekleme süresi: {Delay}ms - ConnectionId: {ConnectionId}",
                    operationName, attempt, MaxRetryAttempts, delay, connectionId);

                // Retry callback'i çağır (varsa)
                if (onRetryAsync != null)
                {
                    try
                    {
                        await onRetryAsync(ex, attempt, delay).ConfigureAwait(false);
                    }
                    catch (Exception callbackEx)
                    {
                        Logger.LogWarning(callbackEx, "OnRetry callback hatası - ConnectionId: {ConnectionId}", connectionId);
                    }
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);

                Logger.LogError(
                    ex,
                    "{OperationName} kalıcı hata ile başarısız oldu. Deneme: {Attempt}/{MaxRetries} - ConnectionId: {ConnectionId}",
                    operationName, attempt, MaxRetryAttempts, connectionId);

                throw new AggregateException(
                    $"{operationName} {MaxRetryAttempts} deneme sonrasında başarısız oldu.",
                    exceptions);
            }
        }

        // Bu noktaya ulaşılmamalı, ancak güvenlik için
        throw new AggregateException(
            $"{operationName} maksimum deneme sayısına ulaştı.",
            exceptions);
    }

    /// <summary>
    /// Exponential backoff ile gecikme hesaplar (jitter ile)
    /// </summary>
    private static int CalculateDelay(int attempt)
    {
        // Exponential backoff: 2^attempt * baseDelay
        var exponentialDelay = (int)Math.Pow(2, attempt - 1) * BaseDelayMs;

        // Jitter ekle (±%25 rastgele değişkenlik)
        var jitter = Random.Shared.Next(-exponentialDelay / 4, exponentialDelay / 4);

        var totalDelay = exponentialDelay + jitter;

        // Max delay sınırını uygula
        return Math.Min(totalDelay, MaxDelayMs);
    }

    /// <summary>
    /// JSON string'ini temizler
    /// </summary>
    private static string CleanJsonString(string jsonString)
    {
        return LineBreakRegex.Replace(jsonString, " ");
    }

    /// <summary>
    /// Loading mesajı gönderir
    /// </summary>
    private async Task SendLoadingMessage(string connectionId, int reportIndex, string summary)
    {
        try
        {
            await HubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveLoadingMessage", $"{reportIndex}. Rapor oluşturuluyor: {summary}")
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Loading mesajı gönderilemedi - ConnectionId: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Rapor mesajı gönderir
    /// </summary>
    private async Task SendReportMessage(string connectionId, Result<LLmResponseModel> apiResult)
    {
        try
        {
            await HubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveMessage", apiResult)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Rapor mesajı gönderilemedi - ConnectionId: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Chunk analizi progress bildirimi gönderir
    /// </summary>
    private async Task SendProgressAsync(string connectionId, AnalysisProgress progress)
    {
        if (string.IsNullOrEmpty(connectionId))
            return;
            
        try
        {
            await HubContext.Clients.Client(connectionId)
                .SendAsync("OnProgress", progress)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Progress bildirimi gönderilemedi - ConnectionId: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Hata yanıtı oluşturur
    /// </summary>
    private static Result<LLmResponseModel> CreateErrorResponse(string summary, string? conversationId = null)
    {
        var responseModel = new LLmResponseModel
        {
            ConversationId = conversationId,
            Summary = summary,
        };

        var result = Result<LLmResponseModel>.Error(summary);
        result.ResultData = responseModel;
        return result;
    }

    #endregion
}
