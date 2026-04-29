using AI.Application.DTOs;

namespace AI.Application.Ports.Secondary.Services.AIChat;

/// <summary>
/// Arama sonuçlarını yeniden sıralama servisi interface'i
/// Cross-encoder veya LLM tabanlı reranking için kullanılır
/// </summary>
public interface IReranker
{
    /// <summary>
    /// Arama sonuçlarını sorguya göre yeniden sıralar
    /// </summary>
    /// <param name="query">Orijinal kullanıcı sorgusu</param>
    /// <param name="candidates">Yeniden sıralanacak aday sonuçlar</param>
    /// <param name="topK">Döndürülecek en iyi sonuç sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Yeniden sıralanmış sonuçlar</returns>
    Task<List<SearchResult>> RerankAsync(
        string query,
        List<SearchResult> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reranking aktif mi kontrolü
    /// </summary>
    bool IsEnabled { get; }
}
