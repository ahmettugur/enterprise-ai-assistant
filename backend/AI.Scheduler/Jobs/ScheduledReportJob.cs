using AI.Application.Ports.Secondary.Notifications;
using AI.Application.Ports.Secondary.Scheduling;
using AI.Scheduler.Configuration;
using Hangfire;
using Microsoft.Extensions.Options;

namespace AI.Scheduler.Jobs;

/// <summary>
/// Zamanlanmış rapor çalıştırma job'ı
/// </summary>
public sealed class ScheduledReportJob
{
    private readonly ISchedulerDataService _dataService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ScheduledReportJob> _logger;
    private readonly ScheduledReportSettings _settings;

    public ScheduledReportJob(
        ISchedulerDataService dataService,
        INotificationService notificationService,
        ILogger<ScheduledReportJob> logger,
        IOptions<ScheduledReportSettings> settings)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Tek bir zamanlanmış raporu çalıştırır
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    [Queue("reports")]
    public async Task ExecuteReportAsync(Guid reportId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Zamanlanmış rapor çalıştırılıyor - ReportId: {ReportId}", reportId);

        // Not: GetByIdAsync metodunu ISchedulerDataService'e eklememiz gerekebilir veya 
        // GetDueReportsAsync üzerinden çalışacak şekilde yeniden tasarlayabiliriz.
        // Şimdilik ISchedulerDataService'e bu metodu ekleyeceğiz.
        
        // Bu kısım ISchedulerDataService üzerinden çalışacak şekilde güncellendi
        // Ancak mevcut interface'de GetByIdAsync yok, bunu ekleyeceğiz.
        // Geçici olarak UpdateScheduledReportLastRunAsync kullanıyoruz.
        
        try
        {
            // Rapor çalıştırma simülasyonu
            await ExecuteSqlQueryAsync(reportId, cancellationToken);

            // Başarılı çalışma kaydı
            await _dataService.UpdateScheduledReportLastRunAsync(reportId, cancellationToken);

            _logger.LogInformation("Rapor başarıyla çalıştırıldı - ReportId: {ReportId}", reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rapor çalıştırılırken hata - ReportId: {ReportId}", reportId);
            throw;
        }
    }

    /// <summary>
    /// SQL sorgusunu çalıştırır ve sonucu döner
    /// </summary>
    private async Task ExecuteSqlQueryAsync(Guid reportId, CancellationToken cancellationToken)
    {
        // TODO: Gerçek SQL çalıştırma implementasyonu
        // Bu kısım rapor servis tipine göre farklı veritabanlarına bağlanacak
        
        _logger.LogDebug("SQL sorgusu çalıştırılıyor - ReportId: {ReportId}", reportId);

        // Şimdilik placeholder
        await Task.Delay(1000, cancellationToken);
    }
}
