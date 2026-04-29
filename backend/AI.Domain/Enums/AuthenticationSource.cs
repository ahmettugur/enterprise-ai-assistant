namespace AI.Domain.Enums;

/// <summary>
/// Kullanıcı kimlik doğrulama kaynağı
/// </summary>
public enum AuthenticationSource
{
    /// <summary>
    /// Lokal kullanıcı (email/şifre ile giriş)
    /// </summary>
    Local = 0,
    
    /// <summary>
    /// Active Directory kullanıcısı (Windows Authentication)
    /// </summary>
    ActiveDirectory = 1
}
