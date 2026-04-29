using System.Collections.Concurrent;
using AI.Domain.Identity;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IUserRepository for testing and development
/// Thread-safe using ConcurrentDictionary
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new();
    private readonly ConcurrentDictionary<string, Role> _roles = new();
    private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens = new();
    private readonly ConcurrentDictionary<string, List<UserRole>> _userRoles = new();

    #region User Operations

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u =>
            u.Email != null && u.Email.Value.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByActiveDirectorySidAsync(string sid, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u => u.ActiveDirectorySid == sid);
        return Task.FromResult(user);
    }

    public Task<User?> GetByAdUsernameAsync(string adUsername, string adDomain, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u =>
            u.AdUsername != null && u.AdUsername.Equals(adUsername, StringComparison.OrdinalIgnoreCase) &&
            u.AdDomain != null && u.AdDomain.Equals(adDomain, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetWithRolesAsync(string id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllAsync(int skip = 0, int take = 100, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _users.Values.AsEnumerable();

        if (!includeInactive)
            query = query.Where(u => u.IsActive);

        var users = query
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult(users);
    }

    public Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateLastLoginAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_users.TryGetValue(userId, out var user))
        {
            user.UpdateLastLogin();
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var exists = _users.Values.Any(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    #endregion

    #region User-Role Assignment Operations

    public Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_userRoles.TryGetValue(userId, out var userRoles))
        {
            var roleNames = userRoles
                .Select(ur => _roles.TryGetValue(ur.RoleId, out var role) ? role.Name : null)
                .Where(name => name != null)
                .Cast<string>()
                .ToList();
            return Task.FromResult(roleNames);
        }
        return Task.FromResult<List<string>>([]);
    }

    public Task<IReadOnlyList<Role>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_userRoles.TryGetValue(userId, out var userRoles))
        {
            var roles = userRoles
                .Select(ur => _roles.TryGetValue(ur.RoleId, out var role) ? role : null)
                .Where(r => r != null)
                .Cast<Role>()
                .ToList();
            return Task.FromResult<IReadOnlyList<Role>>(roles);
        }
        return Task.FromResult<IReadOnlyList<Role>>([]);
    }

    public Task AssignRoleAsync(string userId, string roleId, string? assignedBy = null, CancellationToken cancellationToken = default)
    {
        var userRole = UserRole.Create(userId, roleId, assignedBy);

        _userRoles.AddOrUpdate(
            userId,
            [userRole],
            (_, existing) =>
            {
                if (!existing.Any(ur => ur.RoleId == roleId))
                    existing.Add(userRole);
                return existing;
            });

        return Task.CompletedTask;
    }

    public Task RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        if (_userRoles.TryGetValue(userId, out var userRoles))
        {
            var roleToRemove = userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
            if (roleToRemove != null)
                userRoles.Remove(roleToRemove);
        }
        return Task.CompletedTask;
    }

    #endregion

    #region Refresh Token Operations

    public Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        _refreshTokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _refreshTokens[refreshToken.Token] = refreshToken;
        return Task.FromResult(refreshToken);
    }

    public Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _refreshTokens[refreshToken.Token] = refreshToken;
        return Task.CompletedTask;
    }

    public Task RevokeAllUserTokensAsync(string userId, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var userTokens = _refreshTokens.Values
            .Where(rt => rt.UserId.ToString() == userId && !rt.IsRevoked)
            .ToList();

        foreach (var token in userTokens)
        {
            token.Revoke(ipAddress);
        }
        return Task.CompletedTask;
    }

    public Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = _refreshTokens
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredTokens)
        {
            _refreshTokens.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Clear all data - useful for testing
    /// </summary>
    public void Clear()
    {
        _users.Clear();
        _roles.Clear();
        _refreshTokens.Clear();
        _userRoles.Clear();
    }

    /// <summary>
    /// Seed initial data - useful for testing
    /// </summary>
    public void SeedTestData()
    {
        // Create default roles
        var adminRole = Role.Create("Admin", "Administrator role", isSystem: true);
        var userRole = Role.Create("User", "Standard user role", isSystem: true);
        _roles[adminRole.Id] = adminRole;
        _roles[userRole.Id] = userRole;

        // Create test user
        var testUser = User.CreateLocalUser("test@example.com", "Test User", "Test123!");
        _users[testUser.Id] = testUser;

        // Assign role
        var assignment = UserRole.Create(testUser.Id, userRole.Id, "system");
        _userRoles[testUser.Id] = [assignment];
    }

    #endregion
}
