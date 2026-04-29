namespace AI.Application.Configuration;

/// <summary>
/// JWT yapılandırma ayarları
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    
    /// <summary>
    /// JWT Secret Key - minimum 256 bit (32 karakter)
    /// </summary>
    public string SecretKey { get; set; } = null!;
    
    /// <summary>
    /// Token oluşturucu (issuer)
    /// </summary>
    public string Issuer { get; set; } = null!;
    
    /// <summary>
    /// Token geçerli olacak audience'lar
    /// </summary>
    public string[] Audiences { get; set; } = [];
    
    /// <summary>
    /// Access token geçerlilik süresi (dakika)
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Refresh token geçerlilik süresi (gün)
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
    
    /// <summary>
    /// Clock skew tolerance (saniye)
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 60;
}
