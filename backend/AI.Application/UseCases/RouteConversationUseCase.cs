using System.Diagnostics;
using System.Text.RegularExpressions;
using AI.Application.Common.Helpers;
using AI.Application.Common.Telemetry;
using AI.Application.DTOs.AgentCore;
using AI.Application.DTOs.History;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AgentCore;
using AI.Application.Results;
using AI.Application.DTOs.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;

namespace AI.Application.UseCases;

/// <summary>
/// Kullanıcı isteklerini analiz ederek uygun işlem moduna (Chat, Document, Report) yönlendirir
/// Operasyonel Çok Modlu İstek Analizi ve Yönlendirme Sistemi
/// Best practices ve SOLID prensiplere uygun olarak optimize edilmiştir
/// 2012 yılında Central bölgesinde aylık satış trendini göster. Her ay için satılan ürün miktarı ve toplam satış tutarını raporla.
/// 2012 yılında "Central" adlı bölgede yapılan aylık satış trendini göster. Her ay için satılan ürün miktarı ve toplam satış tutarını raporla.
/// Detaylar: /Common/Resources/Prompts/operasyonel-çok-modlu-istek-analizi-ve-yönlendirme.md
/// </summary>
public class RouteConversationUseCase : IRouteConversationUseCase
{
    #region Constants

    private static readonly Regex JsonBlockRegex = new(@"```json\s*(\{.*?\})\s*```",
        RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex LineBreakRegex = new(@"\r\n|\r|\n",
        RegexOptions.Compiled);

    private const int MaxRetries = 3;        // Retry sayısını 3'ten 2'ye düşür (daha hızlı)
    private const int BaseDelayMs = 500;     // Delay'ı 1000ms'den 500ms'ye düşür
    private const string ReportAction = "report";
    private const string ChatAction = "chat";
    private const string DocumentAction = "document";
    private const string HandleErrorAction = "error";
    private const string HandleAskAction = "ask";

    #endregion

    #region Fields

    private readonly ILogger<RouteConversationUseCase> _logger;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly IConversationUseCase _historyService;
    private readonly IDocumentMetadataUseCase _documentMetadataService;
    private readonly IReportMetadataUseCase _reportMetadataService;
    private readonly IUserMemoryUseCase _userMemoryService;
    private readonly IReActUseCase _reactUseCase;
    private readonly IActionAgentRegistry _agentRegistry;

    #endregion

    #region Constructor

    public RouteConversationUseCase(
        ILogger<RouteConversationUseCase> logger,
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        IConversationUseCase historyService,
        IDocumentMetadataUseCase documentMetadataService,
        IReportMetadataUseCase reportMetadataService,
        IUserMemoryUseCase userMemoryService,
        IReActUseCase reactUseCase,
        IActionAgentRegistry agentRegistry)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _documentMetadataService = documentMetadataService ?? throw new ArgumentNullException(nameof(documentMetadataService));
        _reportMetadataService = reportMetadataService ?? throw new ArgumentNullException(nameof(reportMetadataService));
        _userMemoryService = userMemoryService ?? throw new ArgumentNullException(nameof(userMemoryService));
        _reactUseCase = reactUseCase ?? throw new ArgumentNullException(nameof(reactUseCase));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// İstek analizi yaparak uygun işlem moduna yönlendir
    /// Chat, Document, Report veya Ask modlarından birine yönlendirir
    /// </summary>
    /// <param name="request">Kullanıcı isteği</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Yönlendirme sonucu</returns>
    public async Task<Result<dynamic>> OrchestrateAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Chat.StartActivity("ChatRoute");
        if (activity != null)
        {
            activity.SetTag("conversation.id", request?.ConversationId);
            activity.SetTag("connection.id", request?.ConnectionId);
            activity.SetTag("prompt", request?.Prompt);
            BaggageHelper.SetContextBaggage(conversationId: request?.ConversationId, requestId: request?.ConnectionId);
            BaggageHelper.AddBaggageToActivity(activity);
        }

        try
        {
            ValidateRequest(request!);

            // ===== ReAct STEP 1: THOUGHT (Merkezi ReAct servisi) =====
            await _reactUseCase.SendThoughtAsync(
                request!.ConnectionId,
                request!.Prompt,
                "Kullanıcı isteğini analiz ediyorum. Sistem sohbet, rapor üretme ve döküman arama yapabilir."
            ).ConfigureAwait(false);

            // ===== ReAct STEP 2: ACTION (Intent belirleme) =====
            var llmSelectionResult = await ClassifyIntentAsync(request!).ConfigureAwait(false);
            if (llmSelectionResult == null)
            {
                return Result<dynamic>.Error("İstek analiz edilemedi.");
            }

            // ===== ReAct STEP 2: OBSERVATION (Sonuç) =====
            var observationMsg = llmSelectionResult.Action switch
            {
                "chat" => "Sohbet moduna yönlendiriliyor...",
                "document" => $"Doküman aranıyor: {llmSelectionResult.DocumentName}",
                "report" => $"Rapor oluşturuluyor: {llmSelectionResult.ReportName}",
                _ when llmSelectionResult.Action.StartsWith("ask_") => $"Kullanıcıdan bilgi bekleniyor. {llmSelectionResult.Message}",
                _ => $"İşlem: {llmSelectionResult.Action}"
            };
            await _reactUseCase.SendObservationAsync(request!.ConnectionId, observationMsg).ConfigureAwait(false);

            var action = llmSelectionResult.Action.Replace("ask_", "");
            var actionMessage = $"Action: {action}";

            if (llmSelectionResult.Action == HandleErrorAction)
            {
                llmSelectionResult.Action = llmSelectionResult.Suggestion;
            }


            if (activity != null)
            {
                activity.SetTag("action", action);
            }

            // Registry pattern: Agent'ı bul ve çalıştır
            var context = new ActionContext
            {
                Request = request!,
                RoutingResult = llmSelectionResult
            };

            var agent = _agentRegistry.FindAgent(llmSelectionResult.Action)
                ?? throw new InvalidOperationException($"Action agent bulunamadı: {llmSelectionResult.Action}");

            _logger.LogInformation("Action dispatch: {Action} → {Agent}",
                llmSelectionResult.Action, agent.GetType().Name);

            var apiResult = await agent.HandleAsync(context, cancellationToken).ConfigureAwait(false);

            await _historyService.AddUserMessageAsync(request!, actionMessage, MessageType.Action, cancellationToken: cancellationToken).ConfigureAwait(false);
            return apiResult;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Geçersiz parametre - ConnectionId: {ConnectionId}", request?.ConnectionId);
            activity?.SetStatus(ActivityStatusCode.Error, "Geçersiz parametre");
            return Result<dynamic>.Error("Geçersiz parametre.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrchestrateAsync metodunda hata oluştu - ConnectionId: {ConnectionId}, ConversationId: {ConversationId}",
                request?.ConnectionId, request?.ConversationId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().Name);
            activity?.SetTag("exception.message", ex.Message);
            return Result<dynamic>.Error("İstek işlenirken bir hata oluştu. Lütfen tekrar deneyin.");

        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Request parametrelerini validate eder
    /// </summary>
    private static void ValidateRequest(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ConnectionId))
            throw new ArgumentException("ConnectionId boş olamaz", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt boş olamaz", nameof(request));
    }

    /// <summary>
    /// LLM ile istek analizi yaparak moda yönlendir
    /// </summary>
    private async Task<IntentRoutingResult?> ClassifyIntentAsync(ChatRequest request)
    {
        var lastResult = string.Empty;

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                var result = await ExecuteLlmAnalysisAsync(request, attempt).ConfigureAwait(false);
                lastResult = result;

                var analysisResult = await ParseLlmResponseAsync(result, request).ConfigureAwait(false);

                LogSuccessfulAnalysis(analysisResult, attempt);
                return analysisResult;
            }
            catch (Exception ex)
            {
                if (attempt < MaxRetries - 1)
                {
                    await HandleRetryAsync(ex, attempt, lastResult).ConfigureAwait(false);
                    continue;
                }

                _logger.LogError(ex, "ClassifyIntentAsync metodunda hata oluştu - Tüm denemeler başarısız. Son AI Yanıtı: {LastResult}",
                    lastResult);
            }
        }

        return null;
    }

    /// <summary>
    /// LLM istek analiz işlemini execute eder
    /// </summary>
    private async Task<string> ExecuteLlmAnalysisAsync(ChatRequest request, int attempt = 0)
    {
        MessageType messageType = MessageType.User;
        if (attempt > 0)
        {
            messageType = MessageType.Temporary;
        }

        var basePrompt = Helper.ReadFileContent("Common/Resources/Prompts", "conversation-orchestrator.md");

        // Dinamik kategori ve doküman listelerini inject et
        var categoryList = await _documentMetadataService.GenerateCategoryListForPromptAsync();
        var documentList = await _documentMetadataService.GenerateDocumentListForPromptAsync();

        // Dinamik veritabanı ve rapor listelerini inject et
        var databaseList = _reportMetadataService.GenerateDatabaseListForPrompt();
        var reportTypeList = _reportMetadataService.GenerateReportTypeListForPrompt();
        var dynamicReportCategoryList = _reportMetadataService.GenerateDynamicReportCategoryListForPrompt();

        // Long-Term Memory: Kullanıcı bağlamını al
        var memoryContext = await _userMemoryService.BuildMemoryContextAsync(request.Prompt).ConfigureAwait(false);

        var fullSystemPrompt = basePrompt
            .Replace("{{CATEGORY_LIST}}", categoryList)
            .Replace("{{DOCUMENT_LIST}}", documentList)
            .Replace("{{DATABASE_LIST}}", databaseList)
            .Replace("{{REPORT_TYPE_LIST}}", reportTypeList)
            .Replace("{{DYNAMIC_REPORT_CATEGORY_LIST}}", dynamicReportCategoryList);

        // Long-Term Memory: Kullanıcı bağlamını system prompt'a ekle
        if (!string.IsNullOrEmpty(memoryContext))
        {
            fullSystemPrompt = $"{fullSystemPrompt}\n\n## User Context\n{memoryContext}";
        }

        var conversationDto = await _historyService.ReplaceSystemPromptAsync(request, fullSystemPrompt).ConfigureAwait(false);
        request.ConversationId = conversationDto.Id.ToString();
        var fileInfo = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.FileName))
        {
            var prompt = $"Filename: {request.FileName}";
            fileInfo = $"[Yüklenen Dosya: {request.FileName}] Kullanıcı isteği:";
            await _historyService.AddUserMessageAsync(request, prompt, MessageType.Temporary).ConfigureAwait(false);
        }

        var message = $"{fileInfo} {request.Prompt}".Trim();
        await _historyService.AddUserMessageAsync(request, message, messageType).ConfigureAwait(false);

        var chatHistory = await _historyService.GetChatHistoryAsync(request).ConfigureAwait(false);

        _logger.LogDebug("ChatHistory message count: {Count}, ConversationId: {ConversationId}",
            chatHistory?.Count ?? 0, request.ConversationId);

        if (chatHistory == null || chatHistory.Count == 0)
        {
            _logger.LogError("ChatHistory is empty! ConversationId: {ConversationId}, ConnectionId: {ConnectionId}, Attempt: {Attempt}",
                request.ConversationId, request.ConnectionId, attempt);
            throw new InvalidOperationException($"ChatHistory is empty for ConversationId: {request.ConversationId}. Messages may not have been persisted.");
        }

        var openAiSettings = CreateOpenAiSettings();
        var resultPrompt = await _chatCompletionService.GetChatMessageContentsAsync(chatHistory, openAiSettings, _kernel).ConfigureAwait(false);

        await _historyService.RemoveMessagesByTypeAsync(request, MessageType.Temporary).ConfigureAwait(false);

        _logger.LogInformation("LLM Yanıtı: {ResultPrompt}", resultPrompt[0].ToString()!);
        return resultPrompt[0].ToString()!;
    }

    /// <summary>
    /// LLM yanıtını parse eder - hem markdown kod bloğu içindeki hem de direkt JSON formatını destekler
    /// </summary>
    private async Task<IntentRoutingResult> ParseLlmResponseAsync(string result, ChatRequest request)
    {
        var chatHistory = await _historyService.GetChatHistoryAsync(request).ConfigureAwait(false);
        string jsonString = result;

        // Önce markdown kod bloğu içinde JSON arayalım
        var match = JsonBlockRegex.Match(result);
        if (match.Success)
        {
            // Markdown kod bloğu içinde JSON bulundu
            jsonString = match.Groups[1].Value;
        }

        jsonString = CleanJsonString(jsonString);
        try
        {
            var jsonResponseModel = JsonConvert.DeserializeObject<IntentRoutingResult>(jsonString)!;
            chatHistory.AddAssistantMessage(jsonResponseModel.Message);

            return jsonResponseModel;
        }
        catch (JsonException)
        {
            await _historyService.AddUserMessageAsync(request, "JSON formatı bulunamadı. Tekrar dene ve Json formatında bir yanıt ver.", MessageType.Temporary).ConfigureAwait(false);
            throw new InvalidOperationException($"JSON formatı bulunamadı. AI Yanıtı: {result}");
        }
    }

    /// <summary>
    /// JSON string'ini temizler
    /// </summary>
    private static string CleanJsonString(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        return LineBreakRegex.Replace(json, " ");
    }

    /// <summary>
    /// OpenAI ayarlarını oluşturur
    /// </summary>
    private static OpenAIPromptExecutionSettings CreateOpenAiSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.1F
        };
    }

    /// <summary>
    /// Başarılı analizi loglar
    /// </summary>
    private void LogSuccessfulAnalysis(IntentRoutingResult analysisResult, int attempt)
    {
        _logger.LogInformation("İstek analizi yapıldı. Seçilen Modu: {Action}, ReportName: {ReportName}, Message: {Message}",
            analysisResult.Action, analysisResult.ReportName, analysisResult.Message);

        if (attempt > 0)
        {
            _logger.LogInformation("ClassifyIntentAsync başarılı - Deneme: {Attempt}/{MaxRetries}",
                attempt + 1, MaxRetries);
        }
    }

    /// <summary>
    /// Retry işlemini handle eder
    /// </summary>
    private async Task HandleRetryAsync(Exception ex, int attempt, string lastResult)
    {
        var delay = BaseDelayMs * (int)Math.Pow(2, attempt);

        _logger.LogWarning(ex, "ClassifyIntentAsync metodunda hata oluştu, tekrar denenecek. Deneme: {Attempt}/{MaxRetries}, Bekleme: {Delay}ms, Hata: {Error}",
            attempt + 1, MaxRetries, delay, ex.Message);

        await Task.Delay(delay).ConfigureAwait(false);
    }

    #endregion

}

/// <summary>
/// Router yanıtı modeli
/// Operasyonel Çok Modlu İstek Analizi prompt'undan türetilir
/// </summary>
public class IntentRoutingResult
{
    /// <summary>
    /// LLM'in düşünce süreci - ReAct pattern için
    /// </summary>
    [JsonProperty("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Seçilen işlem türü (action)
    /// Değerler: welcome, chat, document, report, ask_chat, ask_document,
    ///          ask_report, ask_database, ask_report_type, ask_mode
    /// </summary>
    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Seçili veritabanı adı
    /// Değerler: "", "adventureworks", "northwind"
    /// </summary>
    [JsonProperty("reportName")]
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// Seçili doküman adı
    /// </summary>
    [JsonProperty("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcıya gösterilecek mesaj
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Kullanılacak HTML template adı
    /// Değerler: welcome, ask_report, ask_database, ask_mode, ask_document, 
    ///          ask_document_category_genel,
    ///          ask_dynamic_report_type, ask_ready_report,
    ///          ask_report_type_adventureworks, ask_chat
    /// </summary>
    [JsonProperty("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Hata mesajı
    /// </summary>
    [JsonProperty("errorType")]
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Hata mesajı önerisi
    /// </summary>
    [JsonProperty("suggestion")]
    public string Suggestion { get; set; } = string.Empty;
}