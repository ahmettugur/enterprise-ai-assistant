

namespace AI.Domain.Identity;

/// <summary>
/// Kullanıcı repository interface'i
/// </summary>
public interface IUserRepository
{
    // User operations
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByActiveDirectorySidAsync(string sid, CancellationToken cancellationToken = default);
    Task<User?> GetByAdUsernameAsync(string adUsername, string adDomain, CancellationToken cancellationToken = default);
    Task<User?> GetWithRolesAsync(string id, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(int skip = 0, int take = 100, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default);

    // User-Role assignment operations (User aggregate sınırı içinde)
    Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string userId, string roleId, string? assignedBy = null, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);

    // Refresh Token operations
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(string userId, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}
