namespace AI.Application.Ports.Secondary.Services.Auth;

/// <summary>
/// Mevcut oturum açmış kullanıcı bilgilerini sağlayan servis
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Mevcut kullanıcının ID'si (JWT sub claim)
    /// </summary>
    string? UserId { get; }
    
    /// <summary>
    /// Mevcut kullanıcının email adresi
    /// </summary>
    string? Email { get; }
    
    /// <summary>
    /// Mevcut kullanıcının görünen adı
    /// </summary>
    string? DisplayName { get; }
    
    /// <summary>
    /// Kullanıcının rolleri
    /// </summary>
    IEnumerable<string> Roles { get; }
    
    /// <summary>
    /// Kullanıcı oturum açmış mı?
    /// </summary>
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Kullanıcı belirtilen role sahip mi?
    /// </summary>
    bool IsInRole(string role);
    
    /// <summary>
    /// Kullanıcı Admin mi?
    /// </summary>
    bool IsAdmin { get; }
}
