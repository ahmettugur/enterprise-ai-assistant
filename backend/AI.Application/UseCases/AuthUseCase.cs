using AI.Application.DTOs.Auth;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Identity;
using AI.Application.Configuration;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Application.UseCases;

/// <summary>
/// Authentication servisi implementasyonu
/// </summary>
public sealed class AuthUseCase : IAuthUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITokenService _tokenService;
    private readonly ActiveDirectorySettings _adSettings;
    private readonly ILogger<AuthUseCase> _logger;

    public AuthUseCase(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITokenService tokenService,
        IOptions<ActiveDirectorySettings> adSettings,
        ILogger<AuthUseCase> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
        _adSettings = adSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login attempt failed: User not found for email {Email}", request.Email);
            throw new UnauthorizedAccessException("Geçersiz email veya şifre.");
        }

        if (user.AuthenticationSource != AuthenticationSource.Local)
        {
            _logger.LogWarning("Login attempt failed: User {Email} is not a local user", request.Email);
            throw new UnauthorizedAccessException("Bu hesap Active Directory ile giriş yapmalıdır.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt failed: User {Email} is inactive", request.Email);
            throw new UnauthorizedAccessException("Hesabınız devre dışı bırakılmış.");
        }

        if (!user.VerifyPassword(request.Password))
        {
            _logger.LogWarning("Login attempt failed: Invalid password for user {Email}", request.Email);
            throw new UnauthorizedAccessException("Geçersiz email veya şifre.");
        }

        return await CreateAuthResponseAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task<AuthResponse> WindowsLoginAsync(string adUsername, string adDomain, bool rememberMe = false, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        if (!_adSettings.Enabled)
        {
            throw new InvalidOperationException("Active Directory authentication is not enabled.");
        }

        var user = await _userRepository.GetByAdUsernameAsync(adUsername, adDomain, cancellationToken);

        if (user is null)
        {
            if (!_adSettings.AutoCreateUsers)
            {
                _logger.LogWarning("Windows login attempt failed: User {Domain}\\{Username} not found and auto-create is disabled", adDomain, adUsername);
                throw new UnauthorizedAccessException("Kullanıcı bulunamadı. Lütfen sistem yöneticisi ile iletişime geçin.");
            }

            // Kullanıcıyı otomatik oluştur
            user = await CreateUserFromActiveDirectoryAsync(adUsername, adDomain, cancellationToken);
            _logger.LogInformation("Auto-created user from Active Directory: {Domain}\\{Username}", adDomain, adUsername);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Windows login attempt failed: User {Domain}\\{Username} is inactive", adDomain, adUsername);
            throw new UnauthorizedAccessException("Hesabınız devre dışı bırakılmış.");
        }

        return await CreateAuthResponseAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            throw new UnauthorizedAccessException("Geçersiz access token.");
        }

        var jti = _tokenService.GetJtiFromToken(request.AccessToken);
        if (string.IsNullOrEmpty(jti))
        {
            throw new UnauthorizedAccessException("Geçersiz access token.");
        }

        var refreshToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (refreshToken is null)
        {
            throw new UnauthorizedAccessException("Geçersiz refresh token.");
        }

        if (!refreshToken.IsActive)
        {
            _logger.LogWarning("Refresh token attempt with inactive token for user {UserId}", refreshToken.UserId);
            throw new UnauthorizedAccessException("Refresh token süresi dolmuş veya iptal edilmiş.");
        }

        if (refreshToken.JwtId != jti)
        {
            _logger.LogWarning("Refresh token JTI mismatch for user {UserId}", refreshToken.UserId);
            throw new UnauthorizedAccessException("Token eşleşmesi başarısız.");
        }

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Kullanıcı bulunamadı veya devre dışı.");
        }

        // Eski refresh token'ı iptal et
        refreshToken.Revoke(ipAddress);
        await _userRepository.UpdateRefreshTokenAsync(refreshToken, cancellationToken);

        // Yeni token'lar oluştur
        return await CreateAuthResponseAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var token = await _userRepository.GetRefreshTokenAsync(refreshToken, cancellationToken);
        if (token is null)
        {
            return; // Token zaten yok veya iptal edilmiş
        }

        token.Revoke(ipAddress);
        await _userRepository.UpdateRefreshTokenAsync(token, cancellationToken);

        _logger.LogInformation("User {UserId} logged out", token.UserId);
    }

    public async Task LogoutAllAsync(string userId, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        await _userRepository.RevokeAllUserTokensAsync(userId, ipAddress, cancellationToken);
        _logger.LogInformation("All sessions revoked for user {UserId}", userId);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Şifreler eşleşmiyor.");
        }

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("Bu email adresi zaten kullanılıyor.");
        }

        var user = User.CreateLocalUser(
            email: request.Email,
            displayName: request.DisplayName ?? request.Email.Split('@')[0],
            password: request.Password
        );

        // Department ve Title varsa set et (property'ler private set olduğu için entity'de method gerekli)
        // TODO: User entity'sine UpdateProfile metodu eklenebilir

        await _userRepository.CreateAsync(user, cancellationToken);

        // Varsayılan rol ata
        var defaultRole = await _roleRepository.GetByNameAsync(Role.Names.User, cancellationToken);
        if (defaultRole is not null)
        {
            await _userRepository.AssignRoleAsync(user.Id, defaultRole.Id, null, cancellationToken);
        }

        _logger.LogInformation("New user registered: {Email}", request.Email);

        return await CreateAuthResponseAsync(user, ipAddress, userAgent, cancellationToken);
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await _userRepository.GetRolesAsync(userId, cancellationToken);

        return CreateUserInfo(user, roles);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new ArgumentException("Yeni şifreler eşleşmiyor.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("Kullanıcı bulunamadı.");
        }

        if (user.AuthenticationSource != AuthenticationSource.Local)
        {
            throw new InvalidOperationException("Active Directory kullanıcıları şifrelerini bu sistem üzerinden değiştiremez.");
        }

        if (!user.VerifyPassword(request.CurrentPassword))
        {
            throw new UnauthorizedAccessException("Mevcut şifre yanlış.");
        }

        user.SetPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Tüm refresh token'ları iptal et (güvenlik için)
        await _userRepository.RevokeAllUserTokensAsync(userId, cancellationToken: cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}", userId);
    }

    public Guid? GetUserIdFromToken(string accessToken)
    {
        return _tokenService.GetUserIdFromToken(accessToken);
    }

    public bool ValidateToken(string accessToken)
    {
        return _tokenService.ValidateAccessToken(accessToken);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roleNames);
        var jti = _tokenService.GetJtiFromToken(accessToken)!;
        var tokenString = _tokenService.GenerateRefreshTokenString();

        // Aggregate Root üzerinden refresh token oluştur (DDD pattern)
        var refreshToken = user.AddRefreshToken(tokenString, jti, 7, ipAddress, userAgent);

        await _userRepository.CreateRefreshTokenAsync(refreshToken, cancellationToken);
        await _userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            AccessTokenExpiration: DateTime.UtcNow.AddMinutes(60), // TODO: Settings'ten al
            RefreshTokenExpiration: refreshToken.ExpiresAt,
            User: CreateUserInfo(user, roles)
        );
    }

    private static UserInfo CreateUserInfo(User user, IEnumerable<Role> roles)
    {
        return new UserInfo(
            Id: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Department: user.Department,
            Title: user.Title,
            Roles: roles.Select(r => r.Name).ToList(),
            AuthenticationSource: user.AuthenticationSource.ToString()
        );
    }

    private async Task<User> CreateUserFromActiveDirectoryAsync(string adUsername, string adDomain, CancellationToken cancellationToken)
    {
        // AD'den kullanıcı bilgilerini al (gerçek implementasyonda LDAP sorgusu yapılır)
        // Şimdilik basit bir kullanıcı oluşturuyoruz
        var email = $"{adUsername}@{adDomain.ToLowerInvariant()}.local";
        var displayName = adUsername;

        var user = User.CreateFromActiveDirectory(
            email: email,
            displayName: displayName,
            adUsername: adUsername,
            adDomain: adDomain
        );

        await _userRepository.CreateAsync(user, cancellationToken);

        // Varsayılan AD rolünü ata
        var defaultRole = await _roleRepository.GetByNameAsync(_adSettings.DefaultRole, cancellationToken);
        if (defaultRole is not null)
        {
            await _userRepository.AssignRoleAsync(user.Id, defaultRole.Id, null, cancellationToken);
        }

        return user;
    }
}