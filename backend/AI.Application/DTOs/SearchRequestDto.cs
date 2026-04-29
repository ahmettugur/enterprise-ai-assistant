using System.ComponentModel.DataAnnotations;

namespace AI.Application.DTOs;

/// <summary>
/// Arama isteği DTO'su
/// </summary>
public class SearchRequestDto
{
    /// <summary>
    /// Arama sorgusu
    /// </summary>
    [Required(ErrorMessage = "Arama sorgusu gereklidir.")]
    [MinLength(1, ErrorMessage = "Arama sorgusu en az 1 karakter olmalıdır.")]
    [MaxLength(1000, ErrorMessage = "Arama sorgusu en fazla 1000 karakter olabilir.")]
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// Döndürülecek maksimum sonuç sayısı
    /// </summary>
    [Range(1, 100, ErrorMessage = "Limit 1-100 arasında olmalıdır.")]
    public int Limit { get; set; } = 3;
    
    /// <summary>
    /// Minimum benzerlik skoru (0.0 - 1.0)
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Minimum skor 0.0-1.0 arasında olmalıdır.")]
    public float MinScore { get; set; } = 0.7f;
    
    /// <summary>
    /// Filtrelenecek doküman kategorileri
    /// </summary>
    public List<string>? Categories { get; set; }
    
    /// <summary>
    /// Filtrelenecek doküman ID'leri
    /// </summary>
    public List<Guid>? DocumentIds { get; set; }
    
    /// <summary>
    /// Filtrelenecek doküman adları
    /// </summary>
    public List<string>? DocumentNames { get; set; }

    /// <summary>
    /// Tek bir dokümanın adı - hangi collection'da arama yapılacağını belirler
    /// </summary>
    public string? DocumentName { get; set; }

    /// <summary>
    /// Tarih aralığı filtresi - başlangıç
    /// </summary>
    public DateTime? DateFrom { get; set; }
    
    /// <summary>
    /// Tarih aralığı filtresi - bitiş
    /// </summary>
    public DateTime? DateTo { get; set; }
}