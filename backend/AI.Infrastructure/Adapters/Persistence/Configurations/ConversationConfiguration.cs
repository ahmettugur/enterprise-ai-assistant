using AI.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity configuration for Conversation
/// </summary>
internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", "history");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.ConnectionId)
            .HasColumnName("connection_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(255);

        builder.Property(c => c.Title)
            .HasColumnName("title")
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(c => c.LastMessageAt)
            .HasColumnName("last_message_at");

        builder.Property(c => c.MessageCount)
            .HasColumnName("message_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.IsArchived)
            .HasColumnName("is_archived")
            .HasDefaultValue(false)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.ConnectionId)
            .HasDatabaseName("ix_conversations_connection_id")
            .IsUnique();

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_conversations_user_id");

        builder.HasIndex(c => c.UpdatedAt)
            .HasDatabaseName("ix_conversations_updated_at")
            .IsDescending();

        builder.HasIndex(c => new { c.UserId, c.UpdatedAt })
            .HasDatabaseName("ix_conversations_user_updated");

        builder.HasIndex(c => c.IsArchived)
            .HasDatabaseName("ix_conversations_archived")
            .HasFilter("is_archived = false");

        // Relationships - backing field for DDD aggregate root pattern
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Use backing field for Messages navigation property
        builder.Navigation(c => c.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
