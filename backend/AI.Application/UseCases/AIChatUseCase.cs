using System.Diagnostics;
using AI.Application.Common.Resources.Prompts;
using AI.Application.Common.Telemetry;
using AI.Application.DTOs;
using AI.Application.DTOs.History;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.Results;
using AI.Application.Ports.Secondary.Services.Document;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.DTOs.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;

namespace AI.Application.UseCases;

/// <summary>
/// AI Chat servisi implementasyonu
/// AdventureWorksReportService pattern'ini takip eder
/// Long-Term Memory desteği ile kişiselleştirilmiş yanıtlar üretir
/// </summary>
public class AIChatUseCase : IAIChatUseCase
{
    #region Fields

    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly ISignalRHubContext _hubContext;
    private readonly IConversationUseCase _historyService;
    private readonly IContextSummarizationUseCase _contextSummarizationService;
    private readonly ILogger<AIChatUseCase> _logger;
    private readonly IRagSearchUseCase _ragSearchService;
    private readonly IExcelAnalysisUseCase _excelAnalysisUseCase;
    private readonly IExcelAnalysisService _excelAnalysisService;
    private readonly IUserMemoryUseCase _userMemoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentTextExtractor _documentTextExtractor;
    private readonly IReActUseCase _reactUseCase;

    #endregion

    #region Constructor

    public AIChatUseCase(
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ISignalRHubContext hubContext,
        IConversationUseCase historyService,
        IContextSummarizationUseCase contextSummarizationService,
        IRagSearchUseCase ragSearchService,
        IExcelAnalysisUseCase excelAnalysisUseCase,
        IExcelAnalysisService excelAnalysisService,
        IUserMemoryUseCase userMemoryService,
        ICurrentUserService currentUserService,
        IDocumentTextExtractor documentTextExtractor,
        IReActUseCase reactUseCase,
        ILogger<AIChatUseCase> logger)
    {
        _chatCompletionService =
            chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _contextSummarizationService = contextSummarizationService ?? throw new ArgumentNullException(nameof(contextSummarizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ragSearchService = ragSearchService ?? throw new ArgumentNullException(nameof(ragSearchService));
        _excelAnalysisUseCase = excelAnalysisUseCase ?? throw new ArgumentNullException(nameof(excelAnalysisUseCase));
        _excelAnalysisService = excelAnalysisService ?? throw new ArgumentNullException(nameof(excelAnalysisService));
        _userMemoryService = userMemoryService ?? throw new ArgumentNullException(nameof(userMemoryService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _documentTextExtractor = documentTextExtractor ?? throw new ArgumentNullException(nameof(documentTextExtractor));
        _reactUseCase = reactUseCase ?? throw new ArgumentNullException(nameof(reactUseCase));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Streaming chat yanıtı üretir
    /// </summary>
    public async Task<Result<LLmResponseModel>> GetStreamingChatResponseAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Chat.StartActivity("GetStreamingChatResponse");
        if (activity != null)
        {
            activity.SetTag("conversation.id", request?.ConversationId);
            activity.SetTag("connection.id", request?.ConnectionId);
            activity.SetTag("has_file", !string.IsNullOrWhiteSpace(request?.FileBase64));
            BaggageHelper.AddBaggageToActivity(activity);
        }

        try
        {
            ValidateRequest(request!);

            // ===== ReAct: THOUGHT =====
            await _reactUseCase.SendThoughtAsync(
                request!.ConnectionId,
                request!.Prompt,
                "Kullanıcının sohbet isteğini işliyorum. Streaming yanıt üretilecek."
            ).ConfigureAwait(false);

            // Excel/CSV dosyası kontrolü - DuckDB ile işle
            if (!string.IsNullOrWhiteSpace(request!.FileBase64) &&
                !string.IsNullOrWhiteSpace(request.FileName) &&
                _excelAnalysisService.IsSupported(request.FileName))
            {
                return await _excelAnalysisUseCase.ProcessExcelQueryAsync(request, cancellationToken).ConfigureAwait(false);
            }

            // FileBase64 varsa, dosya içeriğini ekle
            var systemPrompt = await SystemPrompt.GetNewReportSelectionPromptAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(request!.FileBase64))
            {
                // Dosya analiz prompt'unu al
                systemPrompt = await SystemPrompt.FileAnalysisPromptAsync().ConfigureAwait(false);
                var finalPrompt = ProcessDocumentFile(request!.FileBase64, request!.Prompt, request!.FileName);
                await _historyService.AddUserMessageAsync(request, finalPrompt, MessageType.Temporary, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Long-Term Memory: Kullanıcı bağlamını system prompt'a ekle
            var memoryContext = await _userMemoryService.BuildMemoryContextAsync(request.Prompt, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(memoryContext))
            {
                systemPrompt = $"{systemPrompt}\n\n## User Context\n{memoryContext}";
            }

            await _historyService.ReplaceSystemPromptAsync(request!, systemPrompt, cancellationToken).ConfigureAwait(false);
            var chatHistory = await _historyService.GetChatHistoryAsync(request, true, cancellationToken).ConfigureAwait(false);

            // Context Summarization - uzun konuşmalarda token tasarrufu sağlar
            if (Guid.TryParse(request.ConversationId, out var conversationGuid))
            {
                chatHistory = await _contextSummarizationService.GetSummarizedChatHistoryAsync(
                    conversationGuid, chatHistory, cancellationToken).ConfigureAwait(false);
            }

            var openAiSettings = CreateOpenAiSettings();

            var responseBuilder = new System.Text.StringBuilder();

            await foreach (var content in _chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory!,
                               openAiSettings, _kernel, cancellationToken))
            {
                if (!string.IsNullOrEmpty(content.Content))
                {
                    responseBuilder.Append(content.Content);

                    // Create streaming response with ConversationId
                    var streamingResponse = new LLmResponseModel
                    {
                        IsSuccess = true,
                        ConversationId = request.ConversationId,
                        HtmlMessage = content.Content
                    };

                    var message = Result<LLmResponseModel>.Success(streamingResponse, "chat");
                    // SignalR ile streaming gönder
                    await _hubContext.Clients.Client(request.ConnectionId)
                        .SendAsync("ReceiveStreamingMessage", message, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            await _historyService.RemoveMessagesByTypeAsync(request, MessageType.Temporary, cancellationToken).ConfigureAwait(false);
            var fullResponse = responseBuilder.ToString();
            _logger.LogInformation("Streaming chat yanıtı üretildi - ConnectionId: {ConnectionId}, Yanıt: {Response}",
                request.ConnectionId, fullResponse);

            if (activity != null)
            {
                activity.SetTag("response.length", fullResponse.Length);
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            // Save to history and get MessageId
            var historyResult = await _historyService.AddAssistantMessageAsync(request, fullResponse, new Dictionary<string, object> { { "SignalRJsFunction", "ReceiveMessage" } }, cancellationToken).ConfigureAwait(false);

            // Create final response with ConversationId and MessageId
            var finalResponse = new LLmResponseModel
            {
                IsSuccess = true,
                ConversationId = request.ConversationId,
                MessageId = historyResult.MessageId.ToString(),
                HtmlMessage = fullResponse
            };

            var jsonResponse = Result<LLmResponseModel>.Success(finalResponse, "chat");

            // Send final message to client with MessageId
            await _hubContext.Clients.Client(request.ConnectionId)
                .SendAsync("ReceiveMessage", jsonResponse).ConfigureAwait(false);

            // Long-Term Memory: Konuşmadan kullanıcı bilgilerini çıkar ve sakla (fire-and-forget)
            // UserId'yi background thread'e geçirmeden önce yakala (scoped service'e erişim için)
            var currentUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _userMemoryService.ExtractAndStoreMemoriesAsync(request.Prompt, fullResponse, currentUserId, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Memory extraction failed, continuing without it");
                    }
                }, CancellationToken.None);
            }

            // ===== ReAct: OBSERVATION =====
            await _reactUseCase.SendObservationAsync(
                request!.ConnectionId,
                "Sohbet yanıtı başarıyla üretildi."
            ).ConfigureAwait(false);

            return Result<LLmResponseModel>.Success(finalResponse, "Streaming chat yanıtı başarıyla döndürüldü.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming chat yanıtı üretilirken hata oluştu - ConnectionId: {ConnectionId}",
                request?.ConnectionId ?? "NULL");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().Name);
            activity?.SetTag("exception.message", ex.Message);
            return Result<LLmResponseModel>.Error("Streaming chat yanıtı üretilirken bir hata oluştu.",
                ex.Message);
        }
    }

    /// <summary>
    /// Vector store'dan arama yapar ve sonuçları döndürür
    /// </summary>
    public async Task<Result<LLmResponseModel>> SearchVectorStoreAsync(ChatRequest request, string documentName, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Chat.StartActivity(documentName);
        if (activity != null)
        {
            activity.SetTag("conversation.id", request?.ConversationId);
            activity.SetTag("connection.id", request?.ConnectionId);
            activity.SetTag("query", request?.Prompt);
            BaggageHelper.AddBaggageToActivity(activity);
        }

        try
        {
            ValidateRequest(request!);

            // ===== ReAct: THOUGHT =====
            await _reactUseCase.SendThoughtAsync(
                request!.ConnectionId,
                request!.Prompt,
                $"Döküman araması yapılacak. Hedef döküman: {documentName}"
            ).ConfigureAwait(false);

            var searchRequest = new SearchRequestDto
            {
                Query = request!.Prompt,
                DocumentName = documentName
            };
            var response = await _ragSearchService.SearchAsync(searchRequest, cancellationToken);
            var searchResultDtos = response.Results.Select(r => new SearchResultDto
            {
                DocumentTitle = r.DocumentTitle,
                Content = r.Content,
                Score = r.SimilarityScore,
                Metadata = r.Metadata
            }).OrderByDescending(r => r.Score).ToList();

            if (activity != null)
            {
                activity.SetTag("results.count", searchResultDtos.Count);
                if (searchResultDtos.Any())
                {
                    activity.SetTag("top.score", searchResultDtos.First().Score);
                }
            }

            if (searchResultDtos.Any())
            {
                // Save to history first and get MessageId
                var historyResult = await _historyService.AddAssistantMessageAsync(request, JsonConvert.SerializeObject(searchResultDtos), new Dictionary<string, object> { { "SignalRJsFunction", "ReceiveMessage" } }, cancellationToken).ConfigureAwait(false);

                var streamingResponse = new LLmResponseModel
                {
                    IsSuccess = true,
                    ConversationId = request.ConversationId,
                    MessageId = historyResult.MessageId.ToString(),
                    Data = searchResultDtos
                };
                var apiResult =
                    Result<LLmResponseModel>.Success(streamingResponse, "İşlem tamamlandı.",
                        "document");
                await _hubContext.Clients.Client(request.ConnectionId)
                    .SendAsync("ReceiveMessage", apiResult, cancellationToken: cancellationToken).ConfigureAwait(false);
                activity?.SetStatus(ActivityStatusCode.Ok);

                // ===== ReAct: OBSERVATION =====
                await _reactUseCase.SendObservationAsync(
                    request.ConnectionId,
                    $"Döküman araması tamamlandı. {searchResultDtos.Count} sonuç bulundu."
                ).ConfigureAwait(false);

                return apiResult;
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            // ===== ReAct: OBSERVATION =====
            await _reactUseCase.SendObservationAsync(
                request!.ConnectionId,
                "Döküman araması tamamlandı, sonuç bulunamadı."
            ).ConfigureAwait(false);

            return Result<LLmResponseModel>.Success("Arama sonucu bulunamadı.", "document");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector store arama işlemi sırasında hata oluştu - ConnectionId: {ConnectionId}",
                request?.ConnectionId ?? "NULL");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().Name);
            activity?.SetTag("exception.message", ex.Message);
            return Result<LLmResponseModel>.Error("Vector store arama işlemi sırasında bir hata oluştu.",
                ex.Message);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Base64 dosyayı işler ve prompt'a ekler
    /// Tüm dosyaları doğrudan işler (chunking'e gerek yok)
    /// </summary>
    private string ProcessDocumentFile(string fileBase64, string originalPrompt, string fileName)
    {
        using var activity = ActivitySources.DocumentProcessing.StartActivity("ProcessDocumentFile");
        if (activity != null)
        {
            activity.SetTag("file.name", fileName);
            BaggageHelper.AddBaggageToActivity(activity);
        }

        try
        {
            // Base64 → Bytes
            byte[] fileBytes = Convert.FromBase64String(fileBase64);
            using var stream = new System.IO.MemoryStream(fileBytes);

            // IDocumentTextExtractor ile metni çıkar
            string extractedText = _documentTextExtractor.ExtractText(fileName, stream);

            if (activity != null)
            {
                activity.SetTag("file.size.bytes", fileBytes.Length);
                activity.SetTag("extracted.chars", extractedText.Length);
            }

            // Geçersiz dosya kontrolü
            if (string.IsNullOrWhiteSpace(extractedText) ||
                extractedText.StartsWith("Desteklenmeyen") ||
                extractedText.StartsWith("Dosya işleme hatası"))
            {
                return originalPrompt;
            }

            var formattedResponse = FormatDocumentResponse(extractedText, originalPrompt);

            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            _logger.LogInformation("Dosya işlendi: {FileName} ({CharCount} chars)",
                fileName, extractedText.Length);

            return formattedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya işleme hatası - {FileName}", fileName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return originalPrompt;
        }
    }

    /// <summary>
    /// Dosya içeriğini formatlanmış yanıt olarak döndürür
    /// </summary>
    private static string FormatDocumentResponse(string content, string originalPrompt)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📊 DOSYA İÇERİĞİ:");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine(content);
        sb.AppendLine("=".PadRight(50, '='));
        return sb.ToString();
    }


    /// <summary>
    /// İsteği doğrular
    /// </summary>
    private static void ValidateRequest(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            throw new ArgumentException("ConnectionId boş olamaz.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt boş olamaz.", nameof(request));
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