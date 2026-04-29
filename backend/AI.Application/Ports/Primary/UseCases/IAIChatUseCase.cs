using AI.Application.Results;
using AI.Application.DTOs.Chat;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// AI Chat Use Case arayüzü - Primary Port
/// API/Controller tarafından çağrılır
/// </summary>
public interface IAIChatUseCase
{
    /// <summary>
    /// Streaming chat yanıtı üretir
    /// </summary>
    /// <param name="request">Chat isteği</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat yanıtı</returns>
    Task<Result<LLmResponseModel>> GetStreamingChatResponseAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vector store'dan arama yapar ve sonuçları döndürür
    /// </summary>
    /// <param name="request">Chat isteği</param>
    /// <param name="documentName">Döküman adı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Arama sonuçları</returns>
    Task<Result<LLmResponseModel>> SearchVectorStoreAsync(ChatRequest request, string documentName, CancellationToken cancellationToken = default);
}
