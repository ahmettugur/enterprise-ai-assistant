using AI.Domain.Identity;

namespace AI.Application.Ports.Secondary.Services.Auth;


/// <summary>
/// JWT Token servisi interface'i
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Access token oluşturur
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> roles);

    /// <summary>
    /// Rastgele refresh token string'i üretir
    /// </summary>
    string GenerateRefreshTokenString();

    /// <summary>
    /// Access token'dan claim'leri çıkarır
    /// Token süresi dolmuş olsa bile
    /// </summary>
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);

    /// <summary>
    /// Access token'ın geçerliliğini doğrular
    /// </summary>
    bool ValidateAccessToken(string accessToken);

    /// <summary>
    /// Token'dan JTI (JWT ID) claim'ini çıkarır
    /// </summary>
    string? GetJtiFromToken(string accessToken);

    /// <summary>
    /// Token'dan User ID'yi çıkarır
    /// </summary>
    Guid? GetUserIdFromToken(string accessToken);
}
