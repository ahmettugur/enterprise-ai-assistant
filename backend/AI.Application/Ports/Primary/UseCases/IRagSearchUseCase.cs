using AI.Application.DTOs;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// RAG arama Use Case interface'i
/// </summary>
public interface IRagSearchUseCase
{
    /// <summary>
    /// Semantic search yapar ve referanslarla birlikte sonuçları döndürür
    /// </summary>
    /// <param name="request">Arama isteği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Arama sonuçları</returns>
    Task<SearchResponse> SearchAsync(SearchRequestDto request, CancellationToken cancellationToken = default);
}
