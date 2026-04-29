namespace AI.Application.DTOs.History;

/// <summary>
/// Özet cache entry modeli
/// </summary>
public class SummaryCacheEntry
{
    public string Summary { get; set; } = string.Empty;
    public int LastSummarizedIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}