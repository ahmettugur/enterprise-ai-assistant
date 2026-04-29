using AI.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// Kullanıcı repository implementasyonu
/// </summary>
public sealed class UserRepository(ChatDbContext dbContext, ILogger<UserRepository> logger) : IUserRepository
{
    private readonly ChatDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ILogger<UserRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #region User Operations

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.ToLowerInvariant();
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByActiveDirectorySidAsync(string sid, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ActiveDirectorySid == sid, cancellationToken);
    }

    public async Task<User?> GetByAdUsernameAsync(string adUsername, string adDomain, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = adUsername.ToLowerInvariant();
        var normalizedDomain = adDomain.ToUpperInvariant();
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.AdUsername != null && string.Equals(u.AdUsername, normalizedUsername, StringComparison.OrdinalIgnoreCase)
                                   && u.AdDomain != null && string.Equals(u.AdDomain, normalizedDomain, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<User?> GetWithRolesAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(int skip = 0, int take = 100, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsQueryable();

        if (!includeInactive)
            query = query.Where(u => u.IsActive);

        return await query
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created - Id: {UserId}, Username: {Username}", user.Id, user.Username);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User updated - Id: {UserId}, Username: {Username}", user.Id, user.Username);
    }

    public async Task UpdateLastLoginAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is not null)
        {
            user.UpdateLastLogin();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.ToLowerInvariant();
        return await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername, cancellationToken);
    }

    #endregion

    #region User-Role Assignment Operations

    public async Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync(cancellationToken);
    }

    public async Task AssignRoleAsync(string userId, string roleId, string? assignedBy = null, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Role already assigned - UserId: {UserId}, RoleId: {RoleId}", userId, roleId);
            return;
        }

        var userRole = UserRole.Create(userId, roleId, assignedBy);
        await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role assigned - UserId: {UserId}, RoleId: {RoleId}", userId, roleId);
    }

    public async Task RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole == null)
        {
            _logger.LogWarning("UserRole not found - UserId: {UserId}, RoleId: {RoleId}", userId, roleId);
            return;
        }

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role removed - UserId: {UserId}, RoleId: {RoleId}", userId, roleId);
    }

    #endregion

    #region Refresh Token Operations

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Refresh token created - UserId: {UserId}", refreshToken.UserId);
        return refreshToken;
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllUserTokensAsync(string userId, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(ipAddress);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("All refresh tokens revoked - UserId: {UserId}, Count: {Count}", userId, activeTokens.Count);
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30); // 30 günden eski token'ları sil

        var expiredTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < cutoffDate || (rt.IsRevoked && rt.RevokedAt < cutoffDate))
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            _dbContext.RefreshTokens.RemoveRange(expiredTokens);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Expired refresh tokens cleaned up - Count: {Count}", expiredTokens.Count);
        }
    }

    #endregion
}
