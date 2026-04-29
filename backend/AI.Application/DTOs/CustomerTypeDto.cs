namespace AI.Application.DTOs;

/// <summary>
/// Müşteri tipi bilgilerini temsil eden DTO
/// </summary>
public class CustomerTypeDto
{
    /// <summary>
    /// Müşteri tipi ID'si
    /// </summary>
    public string? CustomerTypeId { get; set; }
    
    /// <summary>
    /// Müşteri tipi adı (Individual, Store)
    /// </summary>
    public string? CustomerTypeName { get; set; }
}

