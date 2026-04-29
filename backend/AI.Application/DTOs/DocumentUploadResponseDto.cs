namespace AI.Application.DTOs;

/// <summary>
/// Doküman yükleme yanıtı DTO'su
/// </summary>
public class DocumentUploadResponseDto
{
    /// <summary>
    /// Yüklenen dokümanın ID'si
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Dosya adı
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// Dosya türü
    /// </summary>
    public string FileType { get; set; } = string.Empty;
    
    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Doküman kategorisi
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// İşlenme durumu
    /// </summary>
    public DocumentStatus Status { get; set; }
    
    /// <summary>
    /// Toplam chunk sayısı
    /// </summary>
    public int TotalChunks { get; set; }
    
    /// <summary>
    /// Yükleme tarihi
    /// </summary>
    public DateTime UploadedAt { get; set; }
    
    /// <summary>
    /// İşlenme tarihi
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Doküman durumu enum'u
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// İşlenmeyi bekliyor
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// İşleniyor
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Başarıyla tamamlandı
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Başarısız
    /// </summary>
    Failed = 3
}