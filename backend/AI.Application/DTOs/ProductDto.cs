namespace AI.Application.DTOs;

/// <summary>
/// Ürün bilgilerini temsil eden DTO
/// </summary>
public class ProductDto
{
    /// <summary>
    /// Ürün adı
    /// </summary>
    public string? ProductName { get; set; }
    
    /// <summary>
    /// Barkod numarası
    /// </summary>
    public string? ProductNumber { get; set; }
}
