namespace AI.Application.DTOs.AdvancedRag;

/// <summary>
/// Self-Query sonucu
/// </summary>
public class SelfQueryResult
{
    /// <summary>
    /// Semantik arama için kullanılacak sorgu (metadata filtreleri çıkarılmış)
    /// </summary>
    public string SemanticQuery { get; set; } = string.Empty;

    /// <summary>
    /// Çıkarılan metadata filtreleri
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();

    /// <summary>
    /// Orijinal sorgu
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// Filtre çıkarma başarılı mı
    /// </summary>
    public bool HasFilters => Filters.Count > 0;

    /// <summary>
    /// İşlem açıklaması (debug için)
    /// </summary>
    public string? Explanation { get; set; }
}