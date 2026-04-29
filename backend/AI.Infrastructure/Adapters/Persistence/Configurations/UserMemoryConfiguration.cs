using AI.Domain.Memory;
using AI.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// UserMemory entity configuration
/// </summary>
internal sealed class UserMemoryConfiguration : IEntityTypeConfiguration<UserMemory>
{
    public void Configure(EntityTypeBuilder<UserMemory> builder)
    {
        builder.ToTable("user_memories", "memory");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Value)
            .HasColumnName("value")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(m => m.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Context)
            .HasColumnName("context")
            .HasMaxLength(2000);

        builder.Property(m => m.Confidence)
            .HasColumnName("confidence")
            .IsRequired()
            .HasConversion(
                c => c.Value,
                v => Confidence.Create(v))
            .HasDefaultValueSql("1.0");

        builder.Property(m => m.UsageCount)
            .HasColumnName("usage_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.LastAccessedAt)
            .HasColumnName("last_accessed_at")
            .IsRequired();

        builder.Property(m => m.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        // Indexes
        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("ix_user_memories_user_id");

        builder.HasIndex(m => new { m.UserId, m.Key })
            .HasDatabaseName("ix_user_memories_user_id_key")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(m => new { m.UserId, m.Category })
            .HasDatabaseName("ix_user_memories_user_id_category");

        builder.HasIndex(m => m.IsDeleted)
            .HasDatabaseName("ix_user_memories_is_deleted")
            .HasFilter("is_deleted = false");

        // Global query filter for soft delete
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
