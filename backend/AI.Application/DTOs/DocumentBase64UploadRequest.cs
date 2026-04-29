using System.ComponentModel.DataAnnotations;

namespace AI.Application.DTOs;

/// <summary>
/// Base64 formatında doküman yükleme isteği DTO'su
/// </summary>
public class DocumentBase64UploadRequest
{
    /// <summary>
    /// Base64 formatında dosya içeriği
    /// </summary>
    [Required]
    public string FileContent { get; set; } = null!;
    
    /// <summary>
    /// Dosya adı (uzantı ile birlikte)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = null!;
    
    /// <summary>
    /// Dosya MIME türü (opsiyonel, dosya uzantısından çıkarılabilir)
    /// </summary>
    [MaxLength(100)]
    public string? MimeType { get; set; }
    
    /// <summary>
    /// Doküman başlığı (opsiyonel)
    /// </summary>
    [MaxLength(500)]
    public string? Title { get; set; }
    
    /// <summary>
    /// Doküman açıklaması (opsiyonel)
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Doküman kategorisi (opsiyonel)
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }
    
    /// <summary>
    /// Doküman dili (varsayılan: tr)
    /// </summary>
    [MaxLength(5)]
    public string Language { get; set; } = "tr";
    
    /// <summary>
    /// Yükleyici bilgisi (opsiyonel)
    /// </summary>
    [MaxLength(100)]
    public string? UploadedBy { get; set; }
}