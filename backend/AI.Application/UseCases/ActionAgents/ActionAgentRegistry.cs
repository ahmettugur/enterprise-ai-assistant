using AI.Application.Ports.Secondary.Services.AgentCore;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases.ActionAgents;

/// <summary>
/// Action agent'ları yöneten merkezi registry
/// DI container'dan inject edilen tüm IActionAgent'ları tutar
/// Önce tam eşleşme (ActionName), sonra CanHandle ile keşif yapar
/// Hexagonal Architecture: Use Case Implementation
/// </summary>
public class ActionAgentRegistry : IActionAgentRegistry
{
    private readonly IReadOnlyList<IActionAgent> _agents;
    private readonly ILogger<ActionAgentRegistry> _logger;

    public ActionAgentRegistry(
        IEnumerable<IActionAgent> agents,
        ILogger<ActionAgentRegistry> logger)
    {
        _agents = agents?.ToList() ?? throw new ArgumentNullException(nameof(agents));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("ActionAgentRegistry: {Count} agent kayıtlı - [{AgentNames}]",
            _agents.Count,
            string.Join(", ", _agents.Select(a => a.ActionName)));
    }

    /// <summary>
    /// Verilen action adına uygun agent'ı bulur
    /// 1. Önce ActionName tam eşleşme (chat, document, report)
    /// 2. Sonra CanHandle ile esnek eşleşme (ask_* prefix, fallback)
    /// </summary>
    public IActionAgent? FindAgent(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            _logger.LogWarning("ActionAgentRegistry: Boş action ile agent arandı");
            return null;
        }

        // 1. Tam eşleşme (chat, document, report)
        var agent = _agents.FirstOrDefault(a => a.ActionName == action);
        if (agent != null)
        {
            _logger.LogDebug("ActionAgentRegistry: Tam eşleşme — Action: {Action} → {Agent}",
                action, agent.GetType().Name);
            return agent;
        }

        // 2. CanHandle kontrolü (ask_*, welcome, error vb.)
        agent = _agents.FirstOrDefault(a => a.CanHandle(action));
        if (agent != null)
        {
            _logger.LogDebug("ActionAgentRegistry: CanHandle eşleşme — Action: {Action} → {Agent}",
                action, agent.GetType().Name);
            return agent;
        }

        _logger.LogWarning("ActionAgentRegistry: Agent bulunamadı — Action: {Action}", action);
        return null;
    }

    /// <summary>
    /// Kayıtlı tüm agent'ları döndürür
    /// </summary>
    public IReadOnlyList<IActionAgent> GetAllAgents() => _agents;
}
