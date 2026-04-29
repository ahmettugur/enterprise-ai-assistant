using AI.Application.DTOs.AgentCore;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AgentCore;
using AI.Application.Results;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases.ActionAgents;

/// <summary>
/// Chat action agent — Sohbet modunu yönetir
/// Hexagonal Architecture: Use Case Implementation (Agent)
/// </summary>
public class ChatActionAgent : IActionAgent
{
    private readonly IAIChatUseCase _chatService;
    private readonly ILogger<ChatActionAgent> _logger;

    public ChatActionAgent(
        IAIChatUseCase chatService,
        ILogger<ChatActionAgent> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ActionName => "chat";

    public bool CanHandle(string action) => action == ActionName;

    public async Task<Result<dynamic>> HandleAsync(
        ActionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ChatActionAgent: Sohbet modu işleniyor - ConnectionId: {ConnectionId}",
            context.Request.ConnectionId);

        var chatResponse = await _chatService
            .GetStreamingChatResponseAsync(context.Request, cancellationToken)
            .ConfigureAwait(false);

        return Result<dynamic>.Success(
            chatResponse.ResultData!,
            chatResponse.UserMessage,
            context.RoutingResult.Action);
    }
}
