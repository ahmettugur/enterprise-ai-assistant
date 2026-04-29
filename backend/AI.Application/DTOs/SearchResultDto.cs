namespace AI.Application.DTOs;

/// <summary>
/// Arama sonucu DTO'su
/// </summary>
public class SearchResultDto
{
    /// <summary>
    /// Doküman başlığı
    /// </summary>
    public string DocumentTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Chunk içeriği
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Benzerlik skoru (0.0 - 1.0)
    /// </summary>
    public float Score { get; set; }
    
    /// <summary>
    /// Metadata bilgileri
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
