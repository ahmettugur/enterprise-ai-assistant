namespace AI.Application.DTOs;

/// <summary>
/// Mağaza bilgilerini temsil eden DTO
/// </summary>
public class StoreDto
{
    /// <summary>
    /// Mağaza numarası
    /// </summary>
    public string? StoreNumber { get; set; }
    
    /// <summary>
    /// Mağaza adı
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Bölge numarası
    /// </summary>
    public string? RegionNumber { get; set; }

    /// <summary>
    /// Aktif durumu
    /// </summary>
    public string? IsActive { get; set; }
}
