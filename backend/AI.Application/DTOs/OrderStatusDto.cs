namespace AI.Application.DTOs;

/// <summary>
/// Sipariş durumu bilgilerini temsil eden DTO
/// </summary>
public class OrderStatusDto
{
    /// <summary>
    /// Sipariş durumu ID'si
    /// </summary>
    public string? OrderStatusId { get; set; }
    
    /// <summary>
    /// Sipariş durumu adı
    /// </summary>
    public string? OrderStatusName { get; set; }
}

