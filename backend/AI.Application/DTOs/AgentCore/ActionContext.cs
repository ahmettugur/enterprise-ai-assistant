using AI.Application.DTOs.Chat;
using AI.Application.UseCases;
namespace AI.Application.DTOs.AgentCore;

/// <summary>
/// Handler'lara aktarılan bağlam — ChatRequest + LLM routing sonucu
/// RouteConversationUseCase tarafından oluşturulur
/// </summary>
public record ActionContext
{
    /// <summary>
    /// Kullanıcının orijinal isteği
    /// </summary>
    public ChatRequest Request { get; init; } = null!;

    /// <summary>
    /// LLM tarafından belirlenen yönlendirme sonucu
    /// Action, ReportName, DocumentName, TemplateName, Message bilgilerini içerir
    /// </summary>
    public IntentRoutingResult RoutingResult { get; init; } = null!;
}
