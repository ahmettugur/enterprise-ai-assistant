using AI.Application.DTOs.Auth;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Authentication Use Case interface'i
/// </summary>
public interface IAuthUseCase
{
    /// <summary>
    /// Email ve şifre ile giriş
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Windows Authentication ile giriş
    /// AD kullanıcı adı ve domain bilgisi ile kullanıcı doğrulaması
    /// </summary>
    Task<AuthResponse> WindowsLoginAsync(string adUsername, string adDomain, bool rememberMe = false, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh token ile yeni access token alma
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı çıkışı - refresh token'ı iptal eder
    /// </summary>
    Task LogoutAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının tüm refresh token'larını iptal eder
    /// </summary>
    Task LogoutAllAsync(string userId, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yeni kullanıcı kaydı (local authentication)
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı bilgilerini ID ile getir
    /// </summary>
    Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Şifre değiştirme
    /// </summary>
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Access token'dan kullanıcı ID'si çıkarır
    /// </summary>
    Guid? GetUserIdFromToken(string accessToken);

    /// <summary>
    /// Access token'ın geçerliliğini doğrular
    /// </summary>
    bool ValidateToken(string accessToken);
}
