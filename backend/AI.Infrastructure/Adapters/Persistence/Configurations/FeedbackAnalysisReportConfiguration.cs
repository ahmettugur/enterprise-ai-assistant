using AI.Domain.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// EF Core configuration for FeedbackAnalysisReport entity
/// </summary>
public class FeedbackAnalysisReportConfiguration : IEntityTypeConfiguration<FeedbackAnalysisReport>
{
    public void Configure(EntityTypeBuilder<FeedbackAnalysisReport> builder)
    {
        builder.ToTable("feedback_analysis_reports", "history");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.AnalyzedAt)
            .HasColumnName("analyzed_at")
            .IsRequired();

        builder.Property(e => e.TotalFeedbacksAnalyzed)
            .HasColumnName("total_feedbacks_analyzed")
            .IsRequired();

        builder.Property(e => e.OverallSummary)
            .HasColumnName("overall_summary")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.CategoriesJson)
            .HasColumnName("categories_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.SuggestionsJson)
            .HasColumnName("suggestions_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.HighPriorityCount)
            .HasColumnName("high_priority_count")
            .IsRequired();

        builder.Property(e => e.MediumPriorityCount)
            .HasColumnName("medium_priority_count")
            .IsRequired();

        builder.Property(e => e.LowPriorityCount)
            .HasColumnName("low_priority_count")
            .IsRequired();

        builder.Property(e => e.PeriodStart)
            .HasColumnName("period_start");

        builder.Property(e => e.PeriodEnd)
            .HasColumnName("period_end");

        // Index for querying by date
        builder.HasIndex(e => e.AnalyzedAt)
            .HasDatabaseName("ix_feedback_analysis_reports_analyzed_at")
            .IsDescending();
    }
}
