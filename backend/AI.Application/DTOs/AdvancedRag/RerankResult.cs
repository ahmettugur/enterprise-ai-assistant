namespace AI.Application.DTOs.AdvancedRag;

/// <summary>
/// Reranking sonucu
/// </summary>
public class RerankResult
{
    /// <summary>
    /// Orijinal sonuç
    /// </summary>
    public SearchResult Result { get; set; } = null!;

    /// <summary>
    /// Reranking skoru (0.0 - 1.0)
    /// </summary>
    public float RerankScore { get; set; }

    /// <summary>
    /// LLM tarafından verilen relevance açıklaması
    /// </summary>
    public string? Explanation { get; set; }
}