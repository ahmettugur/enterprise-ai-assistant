using AI.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity configuration for ScheduledReportLog
/// Schema: reports
/// </summary>
internal sealed class ScheduledReportLogConfiguration : IEntityTypeConfiguration<ScheduledReportLog>
{
    public void Configure(EntityTypeBuilder<ScheduledReportLog> builder)
    {
        builder.ToTable("scheduled_report_logs", "reports");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.ScheduledReportId)
            .HasColumnName("scheduled_report_id")
            .IsRequired();

        builder.Property(l => l.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(l => l.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(l => l.DurationMs)
            .HasColumnName("duration_ms");

        builder.Property(l => l.IsSuccess)
            .HasColumnName("is_success")
            .IsRequired();

        builder.Property(l => l.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.Property(l => l.ErrorDetails)
            .HasColumnName("error_details");

        builder.Property(l => l.OutputFilePath)
            .HasColumnName("output_file_path")
            .HasMaxLength(500);

        builder.Property(l => l.OutputUrl)
            .HasColumnName("output_url")
            .HasMaxLength(500);

        builder.Property(l => l.RecordCount)
            .HasColumnName("record_count");

        builder.Property(l => l.EmailSent)
            .HasColumnName("email_sent")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(l => l.TeamsSent)
            .HasColumnName("teams_sent")
            .HasDefaultValue(false)
            .IsRequired();

        // Indexes
        builder.HasIndex(l => l.ScheduledReportId)
            .HasDatabaseName("ix_scheduled_report_logs_scheduled_report_id");

        builder.HasIndex(l => l.StartedAt)
            .HasDatabaseName("ix_scheduled_report_logs_started_at");

        builder.HasIndex(l => new { l.ScheduledReportId, l.StartedAt })
            .HasDatabaseName("ix_scheduled_report_logs_report_id_started_at");

        builder.HasIndex(l => l.IsSuccess)
            .HasDatabaseName("ix_scheduled_report_logs_is_success");
    }
}
