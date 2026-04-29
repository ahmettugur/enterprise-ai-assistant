using AI.Application.DTOs.AgentCore;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AgentCore;
using AI.Application.Results;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases.ActionAgents;

/// <summary>
/// Document action agent — Doküman arama modunu yönetir
/// Hexagonal Architecture: Use Case Implementation (Agent)
/// </summary>
public class DocumentActionAgent : IActionAgent
{
    private readonly IAIChatUseCase _chatService;
    private readonly ILogger<DocumentActionAgent> _logger;

    public DocumentActionAgent(
        IAIChatUseCase chatService,
        ILogger<DocumentActionAgent> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ActionName => "document";

    public bool CanHandle(string action) => action == ActionName;

    public async Task<Result<dynamic>> HandleAsync(
        ActionContext context,
        CancellationToken cancellationToken = default)
    {
        var documentName = context.RoutingResult.DocumentName;

        if (string.IsNullOrWhiteSpace(documentName))
        {
            _logger.LogWarning("DocumentActionAgent: DocumentName boş - ConnectionId: {ConnectionId}",
                context.Request.ConnectionId);
            return Result<dynamic>.Success("Lütfen döküman belirleyin.", context.RoutingResult.Action);
        }

        _logger.LogInformation("DocumentActionAgent: Doküman aranıyor '{DocumentName}' - ConnectionId: {ConnectionId}",
            documentName, context.Request.ConnectionId);

        var searchResponse = await _chatService
            .SearchVectorStoreAsync(context.Request, documentName, cancellationToken)
            .ConfigureAwait(false);

        return Result<dynamic>.Success(
            searchResponse.ResultData!,
            searchResponse.UserMessage,
            context.RoutingResult.Action);
    }
}
