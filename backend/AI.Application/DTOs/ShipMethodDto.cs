namespace AI.Application.DTOs;

/// <summary>
/// Teslimat yöntemi bilgilerini temsil eden DTO
/// </summary>
public class ShipMethodDto
{
    /// <summary>
    /// Teslimat yöntemi ID'si
    /// </summary>
    public string? ShipMethodId { get; set; }
    
    /// <summary>
    /// Teslimat yöntemi adı
    /// </summary>
    public string? ShipMethodName { get; set; }
}

