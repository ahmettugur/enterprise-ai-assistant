using AI.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// Role repository implementasyonu
/// </summary>
public sealed class RoleRepository : IRoleRepository
{
    private readonly ChatDbContext _context;

    public RoleRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToUpperInvariant();
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToUpper() == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => r.IsSystem)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByActiveDirectoryGroupAsync(string adGroup, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.ActiveDirectoryGroup == adGroup, cancellationToken);
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
        if (role is not null)
        {
            if (role.IsSystem)
            {
                throw new InvalidOperationException("Sistem rolleri silinemez.");
            }
            
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsNameUniqueAsync(string name, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToUpperInvariant();
        var query = _context.Roles.Where(r => r.Name.ToUpper() == normalizedName);
        
        if (!string.IsNullOrWhiteSpace(excludeId))
        {
            query = query.Where(r => r.Id != excludeId);
        }
        
        return !await query.AnyAsync(cancellationToken);
    }
}
