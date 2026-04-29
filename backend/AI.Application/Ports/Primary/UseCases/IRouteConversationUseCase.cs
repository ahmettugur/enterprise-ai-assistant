using AI.Application.Results;
using AI.Application.DTOs.Chat;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Kullanıcı isteklerini analiz ederek uygun işlem moduna (Chat, Document, Report) yönlendirir
/// Operasyonel Çok Modlu İstek Analizi ve Yönlendirme Sistemi
/// </summary>
public interface IRouteConversationUseCase
{
    /// <summary>
    /// İstek analizi yaparak uygun moda yönlendir
    /// </summary>
    /// <param name="request">Kullanıcı isteği</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Yönlendirme sonucu</returns>
    Task<Result<dynamic>> OrchestrateAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
