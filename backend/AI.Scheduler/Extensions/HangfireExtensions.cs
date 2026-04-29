using AI.Scheduler.Configuration;
using AI.Scheduler.Jobs;
using Hangfire;
using Hangfire.PostgreSql;

namespace AI.Scheduler.Extensions;

/// <summary>
/// Hangfire servisleri için dependency injection extension'ları
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Hangfire servislerini ekler
    /// </summary>
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Ayarları yükle
        var hangfireSettings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>() 
            ?? new HangfireSettings();
        
        services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));
        services.Configure<ScheduledReportSettings>(configuration.GetSection(ScheduledReportSettings.SectionName));

        // Note: EmailSettings and TeamsSettings are now configured in InfrastructureExtensions
        // Note: Notification services (IEmailNotificationService, ITeamsNotificationService, INotificationService) 
        //       are now registered in InfrastructureExtensions

        // PostgreSQL connection string
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("PostgreSQL connection string not found");

        // Hangfire'ı PostgreSQL ile yapılandır
        services.AddHangfire((sp, config) =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                }, new PostgreSqlStorageOptions
                {
                    SchemaName = hangfireSettings.SchemaName,
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = hangfireSettings.SchedulePollingInterval,
                    JobExpirationCheckInterval = hangfireSettings.JobExpirationCheckInterval,
                    CountersAggregateInterval = TimeSpan.FromMinutes(5)
                });
        });

        // Hangfire Server'ı ekle
        services.AddHangfireServer(options =>
        {
            options.ServerName = hangfireSettings.ServerName;
            options.WorkerCount = hangfireSettings.WorkerCount;
            options.Queues = hangfireSettings.Queues;
            options.HeartbeatInterval = hangfireSettings.HeartbeatInterval;
            options.ServerCheckInterval = hangfireSettings.ServerCheckInterval;
            options.SchedulePollingInterval = hangfireSettings.SchedulePollingInterval;
            options.StopTimeout = TimeSpan.FromSeconds(30);
            options.ShutdownTimeout = TimeSpan.FromSeconds(15);
        });

        // Job sınıflarını kaydet
        services.AddScoped<ScheduledReportJob>();
        services.AddScoped<ReportSchedulerJob>();
        services.AddScoped<FeedbackAnalysisJob>();

        return services;
    }

    /// <summary>
    /// Hangfire Dashboard'u ve recurring job'ları yapılandırır
    /// </summary>
    public static IApplicationBuilder UseHangfireServices(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var hangfireSettings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>() 
            ?? new HangfireSettings();

        // Hangfire Dashboard'u ekle
        app.UseHangfireDashboard(hangfireSettings.DashboardPath, new DashboardOptions
        {
            DashboardTitle = hangfireSettings.DashboardTitle,
            DisplayStorageConnectionString = false,
            StatsPollingInterval = (int)hangfireSettings.StatsPollingInterval.TotalMilliseconds,
            // Geliştirme ortamında yetkilendirme olmadan erişim
            // Prodüksiyonda IDashboardAuthorizationFilter implement edilmeli
            Authorization = []
        });

        // Recurring job'ları kaydet
        RegisterRecurringJobs();

        return app;
    }

    /// <summary>
    /// Sistem recurring job'larını kaydeder
    /// </summary>
    private static void RegisterRecurringJobs()
    {
        // Her 5 dakikada bir zamanlanmış raporları senkronize et
        RecurringJob.AddOrUpdate<ReportSchedulerJob>(
            "sync-scheduled-reports",
            job => job.SyncScheduledReportsAsync(CancellationToken.None),
            "*/5 * * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local,
                MisfireHandling = MisfireHandlingMode.Relaxed
            });

        // Her gün gece 2:00'de negatif feedback analizi yap
        RecurringJob.AddOrUpdate<FeedbackAnalysisJob>(
            "feedback-analysis",
            job => job.AnalyzeFeedbacksAsync(CancellationToken.None),
            "0 2 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"),
                MisfireHandling = MisfireHandlingMode.Relaxed
            });
    }
}
