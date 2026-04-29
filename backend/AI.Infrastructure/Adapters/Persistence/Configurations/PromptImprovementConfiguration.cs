using AI.Domain.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromptImprovement entity
/// </summary>
public class PromptImprovementConfiguration : IEntityTypeConfiguration<PromptImprovement>
{
    public void Configure(EntityTypeBuilder<PromptImprovement> builder)
    {
        builder.ToTable("prompt_improvements", "history");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.AnalysisReportId)
            .HasColumnName("analysis_report_id")
            .IsRequired();

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Issue)
            .HasColumnName("issue")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.Suggestion)
            .HasColumnName("suggestion")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.PromptModification)
            .HasColumnName("prompt_modification")
            .HasMaxLength(4000);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ReviewNotes)
            .HasColumnName("review_notes")
            .HasMaxLength(2000);

        builder.Property(e => e.ReviewedBy)
            .HasColumnName("reviewed_by")
            .HasMaxLength(100);

        builder.Property(e => e.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Foreign key relationship
        builder.HasOne(e => e.AnalysisReport)
            .WithMany()
            .HasForeignKey(e => e.AnalysisReportId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_prompt_improvements_status");

        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("ix_prompt_improvements_priority");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_prompt_improvements_created_at")
            .IsDescending();

        builder.HasIndex(e => e.AnalysisReportId)
            .HasDatabaseName("ix_prompt_improvements_analysis_report_id");
    }
}
