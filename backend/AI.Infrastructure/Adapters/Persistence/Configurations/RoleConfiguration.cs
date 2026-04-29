using AI.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Role entity configuration
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "identity");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(200);

        builder.Property(r => r.ActiveDirectoryGroup)
            .HasColumnName("active_directory_group")
            .HasMaxLength(200);

        builder.Property(r => r.IsSystem)
            .HasColumnName("is_system");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at");

        // Indexes
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("ix_roles_name");

        builder.HasIndex(r => r.ActiveDirectoryGroup)
            .HasDatabaseName("ix_roles_ad_group");

        // Relationship - use backing field for encapsulated collection
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Seed data - varsayılan roller
        builder.HasData(
            new { Id = "11111111-1111-1111-1111-111111111111", Name = Role.Names.Admin, Description = "Sistem yöneticisi", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Id = "22222222-2222-2222-2222-222222222222", Name = Role.Names.User, Description = "Standart kullanıcı", IsSystem = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
