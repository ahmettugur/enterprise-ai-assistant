namespace AI.Application.DTOs;

/// <summary>
/// Para birimi bilgilerini temsil eden DTO
/// </summary>
public class CurrencyDto
{
    /// <summary>
    /// Para birimi kodu (USD, EUR, vb.)
    /// </summary>
    public string? CurrencyCode { get; set; }
    
    /// <summary>
    /// Para birimi adı
    /// </summary>
    public string? CurrencyName { get; set; }
}

