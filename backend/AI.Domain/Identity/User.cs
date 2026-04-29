using System.Security.Cryptography;
using AI.Domain.Common;
using AI.Domain.Enums;
using AI.Domain.Events;
using AI.Domain.Exceptions;
using AI.Domain.ValueObjects;

namespace AI.Domain.Identity;

/// <summary>
/// Kullanıcı entity'si
/// Hem AD kullanıcıları hem de lokal kullanıcılar için kullanılır
/// </summary>
public sealed class User : AggregateRoot<string>
{
    public string Username { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;

    /// <summary>
    /// Lokal kullanıcılar için şifre hash'i
    /// AD kullanıcıları için null olabilir
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// Şifre salt değeri
    /// </summary>
    public string? PasswordSalt { get; private set; }

    /// <summary>
    /// Kimlik doğrulama kaynağı
    /// </summary>
    public AuthenticationSource AuthenticationSource { get; private set; }

    /// <summary>
    /// AD kullanıcı adı (örn: jdoe)
    /// </summary>
    public string? AdUsername { get; private set; }

    /// <summary>
    /// AD Domain adı (örn: MYCOMPANY)
    /// </summary>
    public string? AdDomain { get; private set; }

    /// <summary>
    /// AD SID değeri (AD kullanıcıları için)
    /// </summary>
    public string? ActiveDirectorySid { get; private set; }

    /// <summary>
    /// AD'deki Distinguished Name
    /// </summary>
    public string? ActiveDirectoryDn { get; private set; }

    public string? Department { get; private set; }

    public string? Title { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockoutEnd { get; private set; }

    // Navigation properties - encapsulated collections
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // EF Core constructor
    private User() { }

    /// <summary>
    /// Aggregate Root üzerinden yeni refresh token oluşturur ve koleksiyona ekler.
    /// DDD invariant koruması: RefreshToken sadece bu metod ile oluşturulabilir.
    /// </summary>
    public RefreshToken AddRefreshToken(
        string token,
        string jwtId,
        int expirationDays = 7,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var refreshToken = RefreshToken.Create(Id, token, jwtId, expirationDays, ipAddress, userAgent);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    /// <summary>
    /// Lokal kullanıcı oluşturur
    /// </summary>
    public static User CreateLocalUser(string email, string displayName, string password)
    {
        var emailVO = ValueObjects.Email.Create(email);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = emailVO.Value,
            Email = emailVO,
            DisplayName = displayName,
            AuthenticationSource = AuthenticationSource.Local,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0
        };

        user.SetPassword(password);
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Email, "Local"));
        return user;
    }

    /// <summary>
    /// AD kullanıcısı oluşturur
    /// </summary>
    public static User CreateFromActiveDirectory(
        string email,
        string displayName,
        string adUsername,
        string adDomain,
        string? sid = null,
        string? dn = null,
        string? department = null,
        string? title = null)
    {
        var emailVO = ValueObjects.Email.Create(email);

        return new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = $"{adDomain.ToUpperInvariant()}\\{adUsername}".ToLowerInvariant(),
            Email = emailVO,
            DisplayName = displayName,
            AuthenticationSource = AuthenticationSource.ActiveDirectory,
            AdUsername = adUsername,
            AdDomain = adDomain.ToUpperInvariant(),
            ActiveDirectorySid = sid,
            ActiveDirectoryDn = dn,
            Department = department,
            Title = title,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0
        };
    }

    /// <summary>
    /// Şifre belirler (hash + salt ile)
    /// </summary>
    public void SetPassword(string password)
    {
        if (AuthenticationSource != AuthenticationSource.Local)
            throw new InvalidPasswordOperationException(Id);

        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        PasswordSalt = Convert.ToBase64String(salt);
        PasswordHash = HashPassword(password, salt);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Şifre doğrulaması
    /// </summary>
    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(PasswordHash) || string.IsNullOrEmpty(PasswordSalt))
            return false;

        var salt = Convert.FromBase64String(PasswordSalt);
        var hash = HashPassword(password, salt);
        return hash == PasswordHash;
    }

    private static string HashPassword(string password, byte[] salt)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(hash);
    }

    public void UpdateFromActiveDirectory(string? email, string? displayName, string? department, string? title)
    {
        if (!string.IsNullOrEmpty(email))
            Email = ValueObjects.Email.Create(email);
        if (!string.IsNullOrEmpty(displayName))
            DisplayName = displayName;
        Department = department;
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedLogin(int maxAttempts = 5, int lockoutMinutes = 15)
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            AddDomainEvent(new UserLockedOutEvent(Id, FailedLoginAttempts, LockoutEnd.Value));
        }
    }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
