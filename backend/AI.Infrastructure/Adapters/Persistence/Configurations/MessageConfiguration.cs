using AI.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity configuration for Message
/// </summary>
internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", "history");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.TokenCount)
            .HasColumnName("token_count");

        builder.Property(m => m.MessageTypeValue)
            .HasColumnName("message_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb");

        builder.Property(m => m.DeletedAt)
            .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("ix_messages_conversation_id");

        // CreatedAt index for ordering messages (replaces sequence_number)
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .HasDatabaseName("ix_messages_conversation_created_at");

        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("ix_messages_created_at")
            .IsDescending();

        // Full-text search index (PostgreSQL specific)
        // LEFT(content, 500000) ile sınırla — büyük HTML raporlarında tsvector 1MB limitini aşmasını önler
        builder.HasIndex(m => m.Content)
            .HasDatabaseName("ix_messages_content_fts")
            .HasMethod("gin")
            .IsTsVectorExpressionIndex("english");
        
        // Not: Eğer büyük içeriklerde tsvector hatası devam ederse,
        // doğrudan SQL migration ile index'i yeniden tanımlayabilirsiniz:
        // CREATE INDEX ix_messages_content_fts ON history.messages 
        //   USING gin (to_tsvector('english', LEFT(content, 500000)));

        // Query filter for soft delete
        builder.HasQueryFilter(m => m.DeletedAt == null);
    }
}