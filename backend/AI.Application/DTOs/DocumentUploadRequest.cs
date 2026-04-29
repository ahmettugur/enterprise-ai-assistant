using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AI.Application.DTOs;

/// <summary>
/// Doküman yükleme isteği DTO'su
/// </summary>
public class DocumentUploadRequest
{
    /// <summary>
    /// Yüklenecek dosya
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;
    
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