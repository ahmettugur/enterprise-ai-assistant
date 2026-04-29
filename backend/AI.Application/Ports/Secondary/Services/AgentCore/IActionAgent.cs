using AI.Application.DTOs.AgentCore;
using AI.Application.Results;

namespace AI.Application.Ports.Secondary.Services.AgentCore;

/// <summary>
/// Her action türü için agent interface — Strategy pattern
/// RouteConversationUseCase'deki if/else zincirini ortadan kaldırır
/// Hexagonal Architecture: Secondary Port
/// </summary>
public interface IActionAgent
{
    /// <summary>
    /// Bu agent'ın desteklediği birincil action adı ("chat", "document", "report")
    /// </summary>
    string ActionName { get; }

    /// <summary>
    /// Bu agent verilen action'ı işleyebilir mi?
    /// Birincil eşleşme veya prefix tabanlı (ask_*) eşleşme yapılabilir
    /// </summary>
    bool CanHandle(string action);

    /// <summary>
    /// Action'ı çalıştır ve sonuç döndür
    /// </summary>
    Task<Result<dynamic>> HandleAsync(
        ActionContext context,
        CancellationToken cancellationToken = default);
}
