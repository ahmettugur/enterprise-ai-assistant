using AI.Domain.Common;

namespace AI.Domain.Identity;

/// <summary>
/// Rol entity'si
/// Kullanıcı yetkilendirmesi için kullanılır
/// </summary>
public sealed class Role : AggregateRoot<string>
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>
    /// AD grup adı ile eşleştirme (opsiyonel)
    /// Örn: "AI-Admins" AD grubu "Admin" rolüne map edilebilir
    /// </summary>
    public string? ActiveDirectoryGroup { get; private set; }

    public bool IsSystem { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Navigation property - encapsulated collection
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    // EF Core constructor
    private Role() { }

    public static Role Create(string name, string? description = null, string? adGroup = null, bool isSystem = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Role
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            ActiveDirectoryGroup = adGroup,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Predefined roles
    public static class Names
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
