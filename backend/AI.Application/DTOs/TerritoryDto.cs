namespace AI.Application.DTOs;

/// <summary>
/// Satış bölgesi bilgilerini temsil eden DTO
/// </summary>
public class TerritoryDto
{
    /// <summary>
    /// Bölge ID'si
    /// </summary>
    public string? TerritoryId { get; set; }
    
    /// <summary>
    /// Bölge adı
    /// </summary>
    public string? TerritoryName { get; set; }
    
    /// <summary>
    /// Bölge grubu (North America, Europe, Pacific)
    /// </summary>
    public string? TerritoryGroup { get; set; }
}

