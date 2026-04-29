namespace AI.Domain.Identity;

/// <summary>
/// User-Role many-to-many ilişki entity'si
/// </summary>
public sealed class UserRole
{
    public string UserId { get; private set; } = null!;
    public User User { get; private set; } = null!;
    
    public string RoleId { get; private set; } = null!;
    public Role Role { get; private set; } = null!;
    
    public DateTime AssignedAt { get; private set; }
    
    /// <summary>
    /// Rolü atayan kullanıcı (audit için)
    /// </summary>
    public string? AssignedBy { get; private set; }

    // EF Core constructor
    private UserRole() { }

    public static UserRole Create(string userId, string roleId, string? assignedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);

        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };
    }
}
