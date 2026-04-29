namespace AI.Application.DTOs;

/// <summary>
/// Satış temsilcisi bilgilerini temsil eden DTO
/// </summary>
public class SalesPersonDto
{
    /// <summary>
    /// Satış temsilcisi ID'si (BusinessEntityID)
    /// </summary>
    public string? SalesPersonId { get; set; }
    
    /// <summary>
    /// Satış temsilcisi adı (FirstName + LastName)
    /// </summary>
    public string? SalesPersonName { get; set; }
}

