using AI.Domain.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity configuration for MessageFeedback
/// </summary>
internal sealed class MessageFeedbackConfiguration : IEntityTypeConfiguration<MessageFeedback>
{
    public void Configure(EntityTypeBuilder<MessageFeedback> builder)
    {
        builder.ToTable("message_feedbacks", "history");

        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.Id)
            .HasColumnName("id");

        builder.Property(f => f.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(f => f.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000);

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(f => f.IsAnalyzed)
            .HasColumnName("is_analyzed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(f => f.AnalyzedAt)
            .HasColumnName("analyzed_at");

        // Relationships
        builder.HasOne(f => f.Message)
            .WithMany()
            .HasForeignKey(f => f.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Conversation)
            .WithMany()
            .HasForeignKey(f => f.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(f => f.MessageId)
            .HasDatabaseName("ix_message_feedbacks_message_id");

        builder.HasIndex(f => f.ConversationId)
            .HasDatabaseName("ix_message_feedbacks_conversation_id");

        builder.HasIndex(f => f.UserId)
            .HasDatabaseName("ix_message_feedbacks_user_id");

        builder.HasIndex(f => f.CreatedAt)
            .HasDatabaseName("ix_message_feedbacks_created_at");

        // Unique constraint: one feedback per message per user
        builder.HasIndex(f => new { f.MessageId, f.UserId })
            .IsUnique()
            .HasDatabaseName("ix_message_feedbacks_message_user_unique");

        // Index for pending analysis query
        builder.HasIndex(f => new { f.Type, f.IsAnalyzed })
            .HasDatabaseName("ix_message_feedbacks_type_analyzed");

        // Matching query filter: exclude feedbacks for soft-deleted messages
        // Matches Message entity's HasQueryFilter(m => m.DeletedAt == null)
        builder.HasQueryFilter(f => f.Message.DeletedAt == null);
    }
}
