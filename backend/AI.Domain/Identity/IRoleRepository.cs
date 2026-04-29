

namespace AI.Domain.Identity;

/// <summary>
/// Role repository interface'i
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByActiveDirectoryGroupAsync(string adGroup, CancellationToken cancellationToken = default);
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> IsNameUniqueAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default);
}
