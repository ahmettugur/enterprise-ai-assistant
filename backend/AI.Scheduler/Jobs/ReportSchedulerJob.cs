using AI.Application.Ports.Secondary.Scheduling;
using Cronos;
using Hangfire;
using Microsoft.Extensions.Options;
using AI.Scheduler.Configuration;

namespace AI.Scheduler.Jobs;

/// <summary>
/// Zamanlanmış raporları Hangfire'a kaydeden recurring job
/// </summary>
public sealed class ReportSchedulerJob
{
    private readonly ISchedulerDataService _dataService;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<ReportSchedulerJob> _logger;
    private readonly ScheduledReportSettings _settings;

    private static readonly HashSet<Guid> _registeredJobs = [];
    private static readonly object _lock = new();

    public ReportSchedulerJob(
        ISchedulerDataService dataService,
        IRecurringJobManager recurringJobManager,
        ILogger<ReportSchedulerJob> logger,
        IOptions<ScheduledReportSettings> settings)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Aktif raporları kontrol eder ve Hangfire'a kaydeder
    /// Bu job her 5 dakikada bir çalışır
    /// </summary>
    [AutomaticRetry(Attempts = 1)]
    [Queue("critical")]
    public async Task SyncScheduledReportsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Zamanlanmış rapor senkronizasyonu başlatılıyor...");

        try
        {
            // Aktif raporları getir (Application port üzerinden)
            var activeReports = await _dataService.GetAllActiveAsync(cancellationToken);
            
            _logger.LogInformation("Toplam {Count} aktif rapor bulundu", activeReports.Count);

            var currentJobIds = new HashSet<Guid>();

            foreach (var report in activeReports)
            {
                currentJobIds.Add(report.Id);

                // Cron expression geçerli mi kontrol et
                if (!IsValidCronExpression(report.CronExpression))
                {
                    _logger.LogWarning("Geçersiz cron ifadesi, rapor atlanıyor - ReportId: {ReportId}, Cron: {Cron}",
                        report.Id, report.CronExpression);
                    continue;
                }

                var jobId = GetJobId(report.Id, report.Name);

                lock (_lock)
                {
                    // Job zaten kayıtlı mı kontrol et
                    if (!_registeredJobs.Contains(report.Id))
                    {
                        // Yeni recurring job ekle
                        _recurringJobManager.AddOrUpdate<ScheduledReportJob>(
                            jobId,
                            job => job.ExecuteReportAsync(report.Id, CancellationToken.None),
                            report.CronExpression,
                            new RecurringJobOptions
                            {
                                TimeZone = TimeZoneInfo.Local,
                                MisfireHandling = MisfireHandlingMode.Relaxed
                            });

                        _registeredJobs.Add(report.Id);
                        
                        _logger.LogInformation(
                            "Recurring job eklendi - ReportId: {ReportId}, Name: {Name}, Cron: {Cron}",
                            report.Id, report.Name, report.CronExpression);
                    }
                }
            }

            // Artık aktif olmayan job'ları kaldır
            lock (_lock)
            {
                var jobsToRemove = _registeredJobs.Where(id => !currentJobIds.Contains(id)).ToList();
                
                foreach (var reportId in jobsToRemove)
                {
                    // Eski job'ları kaldırırken isim bilinmiyor, eski format ile dene
                    var jobId = GetJobId(reportId);
                    _recurringJobManager.RemoveIfExists(jobId);
                    _registeredJobs.Remove(reportId);
                    
                    _logger.LogInformation("Recurring job kaldırıldı - ReportId: {ReportId}", reportId);
                }
            }

            _logger.LogInformation("Zamanlanmış rapor senkronizasyonu tamamlandı - Registered: {Count}",
                _registeredJobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor senkronizasyonu sırasında hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Tek bir raporu Hangfire'a kaydeder
    /// </summary>
    public void RegisterReport(Guid reportId, string cronExpression, string reportName)
    {
        if (!IsValidCronExpression(cronExpression))
        {
            _logger.LogWarning("Geçersiz cron ifadesi - ReportId: {ReportId}, Cron: {Cron}", reportId, cronExpression);
            return;
        }

        var jobId = GetJobId(reportId, reportName);

        lock (_lock)
        {
            _recurringJobManager.AddOrUpdate<ScheduledReportJob>(
                jobId,
                job => job.ExecuteReportAsync(reportId, CancellationToken.None),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local,
                    MisfireHandling = MisfireHandlingMode.Relaxed
                });

            _registeredJobs.Add(reportId);
        }

        _logger.LogInformation("Rapor kaydedildi - ReportId: {ReportId}, Name: {Name}, Cron: {Cron}, JobId: {JobId}",
            reportId, reportName, cronExpression, jobId);
    }

    /// <summary>
    /// Bir raporu Hangfire'dan kaldırır
    /// </summary>
    public void UnregisterReport(Guid reportId, string? reportName = null)
    {
        // Hem yeni format hem eski format ile dene
        var jobIdWithName = reportName != null ? GetJobId(reportId, reportName) : null;
        var jobIdWithoutName = GetJobId(reportId);

        lock (_lock)
        {
            if (jobIdWithName != null)
            {
                _recurringJobManager.RemoveIfExists(jobIdWithName);
            }
            _recurringJobManager.RemoveIfExists(jobIdWithoutName);
            _registeredJobs.Remove(reportId);
        }

        _logger.LogInformation("Rapor kaldırıldı - ReportId: {ReportId}", reportId);
    }

    /// <summary>
    /// Bir raporu hemen çalıştırır
    /// </summary>
    public string TriggerReport(Guid reportId)
    {
        var jobId = BackgroundJob.Enqueue<ScheduledReportJob>(
            job => job.ExecuteReportAsync(reportId, CancellationToken.None));

        _logger.LogInformation("Rapor tetiklendi - ReportId: {ReportId}, JobId: {JobId}", reportId, jobId);
        
        return jobId;
    }

    private static string GetJobId(Guid reportId) => $"scheduled-report-{reportId}";

    private static string GetJobId(Guid reportId, string reportName)
    {
        // Rapor adını URL-safe hale getir
        var safeName = string.IsNullOrWhiteSpace(reportName) 
            ? "unnamed" 
            : new string(reportName
                .ToLowerInvariant()
                .Replace(' ', '-')
                .Replace('ı', 'i')
                .Replace('ğ', 'g')
                .Replace('ü', 'u')
                .Replace('ş', 's')
                .Replace('ö', 'o')
                .Replace('ç', 'c')
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .Take(50) // Max 50 karakter
                .ToArray());
        
        return $"scheduled-report-{safeName}-{reportId}";
    }

    private static bool IsValidCronExpression(string cronExpression)
    {
        try
        {
            CronExpression.Parse(cronExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
