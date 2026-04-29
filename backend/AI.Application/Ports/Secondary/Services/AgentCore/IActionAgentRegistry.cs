namespace AI.Application.Ports.Secondary.Services.AgentCore;

/// <summary>
/// Action agent'ları keşfetmek ve yönetmek için registry — Secondary Port
/// DI container üzerinden inject edilen agent'ları merkezi olarak yönetir
/// Hexagonal Architecture: Secondary Port
/// </summary>
public interface IActionAgentRegistry
{
    /// <summary>
    /// Verilen action adına uygun agent'ı bulur
    /// Önce tam eşleşme (ActionName), sonra CanHandle kontrolü yapar
    /// </summary>
    /// <param name="action">Aranacak action adı (ör: "chat", "report", "ask_document")</param>
    /// <returns>Uygun agent veya null</returns>
    IActionAgent? FindAgent(string action);

    /// <summary>
    /// Kayıtlı tüm agent'ları döndürür
    /// </summary>
    IReadOnlyList<IActionAgent> GetAllAgents();
}
