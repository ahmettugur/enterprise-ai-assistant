using AI.Application.Common.Helpers;
using AI.Application.DTOs.AgentCore;
using AI.Application.DTOs.Chat;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AgentCore;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.Results;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases.ActionAgents;

/// <summary>
/// Ask action agent — Kullanıcıdan bilgi isteme ve template gösterme modunu yönetir
/// Fallback agent olarak da çalışır (bilinmeyen action'lar için)
/// Hexagonal Architecture: Use Case Implementation (Agent)
/// </summary>
public class AskActionAgent : IActionAgent
{
    private readonly IConversationUseCase _historyService;
    private readonly ISignalRHubContext _hubContext;
    private readonly IDocumentMetadataUseCase _documentMetadataService;
    private readonly IReportMetadataUseCase _reportMetadataService;
    private readonly ILogger<AskActionAgent> _logger;

    public AskActionAgent(
        IConversationUseCase historyService,
        ISignalRHubContext hubContext,
        IDocumentMetadataUseCase documentMetadataService,
        IReportMetadataUseCase reportMetadataService,
        ILogger<AskActionAgent> logger)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _documentMetadataService = documentMetadataService ?? throw new ArgumentNullException(nameof(documentMetadataService));
        _reportMetadataService = reportMetadataService ?? throw new ArgumentNullException(nameof(reportMetadataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Birincil action adı — ancak CanHandle ile tüm ask_* ve bilinmeyen action'ları da yakalar
    /// </summary>
    public string ActionName => "ask";

    /// <summary>
    /// ask_* prefix ile başlayan action'ları ve bilinmeyen action'ları kabul eder (fallback)
    /// </summary>
    public bool CanHandle(string action)
    {
        return action == ActionName
               || action.StartsWith("ask_")
               || action == "welcome"
               || action == "error";
    }

    public async Task<Result<dynamic>> HandleAsync(
        ActionContext context,
        CancellationToken cancellationToken = default)
    {
        var llmResult = context.RoutingResult;
        var request = context.Request;

        try
        {
            _logger.LogInformation(
                "AskActionAgent: Template işleniyor '{TemplateName}' - Action: {Action}, ConnectionId: {ConnectionId}",
                llmResult.TemplateName, llmResult.Action, request.ConnectionId);

            var htmlMessage = await GetTemplateContentAsync(llmResult.TemplateName, llmResult.Message);

            var historyResult = await _historyService.AddAssistantMessageAsync(
                request,
                htmlMessage,
                new Dictionary<string, object> { { "SignalRJsFunction", "ReceiveMessage" } }
            ).ConfigureAwait(false);

            var message = llmResult.Message ?? "İşlem iptal edildi";
            var responseModel = new LLmResponseModel
            {
                IsSuccess = false,
                ConversationId = request.ConversationId,
                MessageId = historyResult.MessageId.ToString(),
                HtmlMessage = htmlMessage,
                Summary = message,
            };

            var apiResult = Result<dynamic>.Success(responseModel, message, llmResult.Action);
            await _hubContext.Clients.Client(request.ConnectionId)
                .SendAsync("ReceiveMessage", apiResult)
                .ConfigureAwait(false);

            return Result<dynamic>.Success(responseModel, message, llmResult.Action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AskActionAgent: Mesaj gönderilemedi - ConnectionId: {ConnectionId}", request.ConnectionId);
            return Result<dynamic>.Error(ex, nameof(AskActionAgent), "İptal mesajı gönderilemedi");
        }
    }

    #region Template Content Resolution

    /// <summary>
    /// Template içeriğini dosyadan okur veya dinamik oluşturur
    /// </summary>
    private async Task<string> GetTemplateContentAsync(string templateName, string fallbackMessage)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return fallbackMessage;

        // === DOKÜMAN İŞLEMLERİ ===
        if (templateName == "ask_document")
            return await _documentMetadataService.GenerateDynamicCategorySelectionTemplateAsync();

        if (templateName.StartsWith("ask_document_category_"))
        {
            var category = templateName.Replace("ask_document_category_", "");
            return await _documentMetadataService.GenerateDynamicDocumentTemplateAsync(category);
        }

        // === RAPOR İŞLEMLERİ ===
        if (templateName == "ask_report" || templateName == "ask_database")
            return _reportMetadataService.GenerateDynamicDatabaseSelectionTemplate();

        if (templateName.StartsWith("ask_report_type_"))
        {
            var databaseId = templateName.Replace("ask_report_type_", "");
            return _reportMetadataService.GenerateDynamicReportTypeTemplate(databaseId);
        }

        if (templateName.StartsWith("ask_dynamic_report_type_"))
        {
            var databaseId = templateName.Replace("ask_dynamic_report_type_", "");
            return _reportMetadataService.GenerateDynamicReportCategoryTemplate(databaseId);
        }

        if (templateName == "ask_dynamic_report_type")
            return _reportMetadataService.GenerateDynamicReportCategoryTemplate("adventureworks");

        if (templateName == "ask_ready_report")
            return _reportMetadataService.GenerateReadyReportTemplate();

        // Statik template dosyasından oku
        var htmlContent = Helper.ReadFileContent("Common/Resources/Templates", $"{templateName}.html");
        if (!string.IsNullOrWhiteSpace(htmlContent))
            return htmlContent;

        var txtContent = Helper.ReadFileContent("Common/Resources/Templates", $"{templateName}.txt");
        if (!string.IsNullOrWhiteSpace(txtContent))
            return txtContent;

        return fallbackMessage;
    }

    #endregion
}
