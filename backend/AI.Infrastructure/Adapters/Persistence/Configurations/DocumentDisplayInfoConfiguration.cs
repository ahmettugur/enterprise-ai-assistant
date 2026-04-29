using AI.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity configuration for DocumentDisplayInfo
/// </summary>
internal sealed class DocumentDisplayInfoConfiguration : IEntityTypeConfiguration<DocumentDisplayInfo>
{
    public void Configure(EntityTypeBuilder<DocumentDisplayInfo> builder)
    {
        builder.ToTable("document_display_info", "document");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(d => d.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.DocumentType)
            .HasColumnName("document_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(d => d.Keywords)
            .HasColumnName("keywords")
            .HasMaxLength(500);

        builder.Property(d => d.CategoryId)
            .HasColumnName("category_id")
            .HasMaxLength(50);

        builder.Property(d => d.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100);

        builder.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");

        // Index for faster lookups by filename
        builder.HasIndex(d => d.FileName)
            .IsUnique()
            .HasDatabaseName("ix_document_display_info_file_name");

        // Index for category filtering
        builder.HasIndex(d => d.CategoryId)
            .HasDatabaseName("ix_document_display_info_category_id");

        // Index for user filtering
        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("ix_document_display_info_user_id");

        // Relationship configured in DocumentCategoryConfiguration
    }
}
