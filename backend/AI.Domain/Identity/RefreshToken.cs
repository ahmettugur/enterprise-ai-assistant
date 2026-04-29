using AI.Domain.Common;

namespace AI.Domain.Identity;

/// <summary>
/// Refresh token entity'si
/// JWT refresh token'ları saklamak için kullanılır
/// </summary>
public sealed class RefreshToken : Entity<string>
{

    public string UserId { get; private set; } = null!;
    public User User { get; private set; } = null!;

    public string Token { get; private set; } = null!;

    public string JwtId { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Bu token ile yeni token alındığında, yeni token'ın ID'si
    /// Token chain takibi için kullanılır
    /// </summary>
    public string? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? UserAgent { get; private set; }

    // EF Core constructor
    private RefreshToken() { }

    internal static RefreshToken Create(
        string userId,
        string token,
        string jwtId,
        int expirationDays = 7,
        string? ipAddress = null,
        string? userAgent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtId);

        return new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Token = token,
            JwtId = jwtId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            IsRevoked = false,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? ipAddress = null, string? replacedByTokenId = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        ReplacedByTokenId = replacedByTokenId;
    }
}
