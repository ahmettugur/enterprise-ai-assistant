namespace AI.Application.DTOs;

/// <summary>
/// Kampanya bilgilerini temsil eden DTO
/// </summary>
public class PromotionDto
{
    /// <summary>
    /// Kampanya numarası
    /// </summary>
    public string? PromotionNumber { get; set; }
    
    /// <summary>
    /// Kampanya adı
    /// </summary>
    public string? PromotionName { get; set; }
}
