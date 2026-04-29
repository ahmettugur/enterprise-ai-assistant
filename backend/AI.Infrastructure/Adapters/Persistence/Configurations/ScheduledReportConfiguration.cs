using AI.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Adapters.Persistence.Configurations;

/// <summary>
/// Entity configuration for ScheduledReport
/// Schema: reports
/// </summary>
internal sealed class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.ToTable("scheduled_reports", "reports");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(s => s.OriginalPrompt)
            .HasColumnName("original_prompt")
            .IsRequired();

        builder.Property(s => s.SqlQuery)
            .HasColumnName("sql_query")
            .IsRequired();

        builder.Property(s => s.OriginalMessageId)
            .HasColumnName("original_message_id");

        builder.Property(s => s.OriginalConversationId)
            .HasColumnName("original_conversation_id");

        builder.Property(s => s.CronExpression)
            .HasColumnName("cron_expression")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.ReportServiceType)
            .HasColumnName("report_service_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.ReportDatabaseType)
            .HasColumnName("report_database_type")
            .HasMaxLength(50);

        builder.Property(s => s.ReportDatabaseServiceType)
            .HasColumnName("report_database_service_type")
            .HasMaxLength(100);

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(s => s.LastRunAt)
            .HasColumnName("last_run_at");

        builder.Property(s => s.NextRunAt)
            .HasColumnName("next_run_at");

        builder.Property(s => s.RunCount)
            .HasColumnName("run_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.LastRunSuccess)
            .HasColumnName("last_run_success");

        builder.Property(s => s.LastErrorMessage)
            .HasColumnName("last_error_message")
            .HasMaxLength(2000);

        builder.Property(s => s.NotificationEmail)
            .HasColumnName("notification_email")
            .HasMaxLength(255);

        builder.Property(s => s.TeamsWebhookUrl)
            .HasColumnName("teams_webhook_url")
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Navigation property - one-to-many relationship with backing field
        builder.HasMany(s => s.Logs)
            .WithOne(l => l.ScheduledReport)
            .HasForeignKey(l => l.ScheduledReportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Logs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_scheduled_reports_user_id");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("ix_scheduled_reports_is_active");

        builder.HasIndex(s => s.NextRunAt)
            .HasDatabaseName("ix_scheduled_reports_next_run_at");

        builder.HasIndex(s => new { s.UserId, s.IsActive })
            .HasDatabaseName("ix_scheduled_reports_user_id_is_active");
    }
}
