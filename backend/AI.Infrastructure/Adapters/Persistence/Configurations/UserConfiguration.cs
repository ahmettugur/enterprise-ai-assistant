using AI.Domain.Identity;
using AI.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// User entity configuration
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(256)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500);

        builder.Property(u => u.PasswordSalt)
            .HasColumnName("password_salt")
            .HasMaxLength(100);

        builder.Property(u => u.AuthenticationSource)
            .HasColumnName("authentication_source")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.AdUsername)
            .HasColumnName("ad_username")
            .HasMaxLength(100);

        builder.Property(u => u.AdDomain)
            .HasColumnName("ad_domain")
            .HasMaxLength(100);

        builder.Property(u => u.ActiveDirectorySid)
            .HasColumnName("active_directory_sid")
            .HasMaxLength(200);

        builder.Property(u => u.ActiveDirectoryDn)
            .HasColumnName("active_directory_dn")
            .HasMaxLength(500);

        builder.Property(u => u.Department)
            .HasColumnName("department")
            .HasMaxLength(100);

        builder.Property(u => u.Title)
            .HasColumnName("title")
            .HasMaxLength(100);

        builder.Property(u => u.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(50);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(u => u.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts");

        builder.Property(u => u.LockoutEnd)
            .HasColumnName("lockout_end");

        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("ix_users_username");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");

        builder.HasIndex(u => u.ActiveDirectorySid)
            .HasDatabaseName("ix_users_ad_sid");

        builder.HasIndex(u => new { u.AdUsername, u.AdDomain })
            .HasDatabaseName("ix_users_ad_username_domain");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_users_is_active");

        // Relationships - use backing fields for encapsulated collections
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.RefreshTokens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Not: Admin kullanıcısı runtime'da SeedDefaultUsersAsync ile oluşturuluyor
        // Migration seed kullanılmıyor çünkü şifre hash'i dinamik olarak üretilmeli
    }
}
