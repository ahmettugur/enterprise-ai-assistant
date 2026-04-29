namespace AI.Application.DTOs;

/// <summary>
/// Satış nedeni bilgilerini temsil eden DTO
/// </summary>
public class SalesReasonDto
{
    /// <summary>
    /// Satış nedeni ID'si
    /// </summary>
    public string? SalesReasonId { get; set; }
    
    /// <summary>
    /// Satış nedeni adı
    /// </summary>
    public string? SalesReasonName { get; set; }
    
    /// <summary>
    /// Satış nedeni türü
    /// </summary>
    public string? ReasonType { get; set; }
}

