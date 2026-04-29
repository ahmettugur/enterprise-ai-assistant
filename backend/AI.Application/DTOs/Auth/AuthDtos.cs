namespace AI.Application.DTOs.Auth;

/// <summary>
/// Login isteği DTO'su
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

/// <summary>
/// Windows Authentication ile login isteği
/// </summary>
public sealed record WindowsLoginRequest(
    bool RememberMe = false
);

/// <summary>
/// Token yenileme isteği
/// </summary>
public sealed record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);

/// <summary>
/// Başarılı authentication sonucu
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiration,
    DateTime RefreshTokenExpiration,
    UserInfo User
);

/// <summary>
/// Kullanıcı bilgileri
/// </summary>
public sealed record UserInfo(
    string Id,
    string Email,
    string DisplayName,
    string? Department,
    string? Title,
    IReadOnlyList<string> Roles,
    string AuthenticationSource
);

/// <summary>
/// Kullanıcı kayıt isteği
/// </summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string? DisplayName,
    string? Department,
    string? Title
);

/// <summary>
/// Şifre değiştirme isteği
/// </summary>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);

/// <summary>
/// Şifre sıfırlama isteği (forgot password)
/// </summary>
public sealed record ForgotPasswordRequest(
    string Email
);

/// <summary>
/// Şifre sıfırlama (reset password)
/// </summary>
public sealed record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmNewPassword
);
