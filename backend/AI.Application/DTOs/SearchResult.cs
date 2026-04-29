namespace AI.Application.DTOs;

/// <summary>
/// Arama sonucu DTO'su
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Chunk ID'si
    /// </summary>
    public Guid ChunkId { get; set; }
    
    /// <summary>
    /// Doküman ID'si
    /// </summary>
    public Guid DocumentId { get; set; }
    
    /// <summary>
    /// Doküman başlığı
    /// </summary>
    public string DocumentTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Doküman kategorisi
    /// </summary>
    public string? DocumentCategory { get; set; }
    
    /// <summary>
    /// Chunk içeriği
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Benzerlik skoru (0.0 - 1.0)
    /// </summary>
    public float SimilarityScore { get; set; }
    
    /// <summary>
    /// Qdrant'tan gelen ham skor
    /// </summary>
    public float Score { get; set; }
    
    /// <summary>
    /// Chunk sıra numarası
    /// </summary>
    public int ChunkIndex { get; set; }
    
    /// <summary>
    /// Chunk başlangıç pozisyonu
    /// </summary>
    public int StartPosition { get; set; }
    
    /// <summary>
    /// Chunk bitiş pozisyonu
    /// </summary>
    public int EndPosition { get; set; }
    
    /// <summary>
    /// Doküman yükleme tarihi
    /// </summary>
    public DateTime? UploadedAt { get; set; }
    
    /// <summary>
    /// Doküman adı
    /// </summary>
    public string? DocumentName { get; set; }
    
    /// <summary>
    /// Kategori
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Metadata bilgileri
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Arama yanıtı DTO'su
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// Arama sorgusu
    /// </summary>
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// Toplam bulunan sonuç sayısı
    /// </summary>
    public int TotalResults { get; set; }
    
    /// <summary>
    /// Arama süresi (milisaniye)
    /// </summary>
    public int ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// Arama sonuçları
    /// </summary>
    public List<SearchResult> Results { get; set; } = new();
    
    /// <summary>
    /// Arama metadata bilgileri
    /// </summary>
    public Dictionary<string, object> SearchMetadata { get; set; } = new();
}