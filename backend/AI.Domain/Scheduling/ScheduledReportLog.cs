using AI.Domain.Common;

namespace AI.Domain.Scheduling;

/// <summary>
/// Zamanlanmış rapor çalışma geçmişi
/// Her rapor çalıştırıldığında bir log kaydı oluşturulur
/// </summary>
public sealed class ScheduledReportLog : Entity<Guid>
{

    /// <summary>
    /// İlişkili zamanlanmış rapor ID'si
    /// </summary>
    public Guid ScheduledReportId { get; private set; }

    /// <summary>
    /// Çalışma başlangıç zamanı
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// Çalışma bitiş zamanı
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Çalışma süresi (milisaniye)
    /// </summary>
    public long? DurationMs { get; private set; }

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Hata mesajı (başarısızsa)
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Hata detayları / stack trace (başarısızsa)
    /// </summary>
    public string? ErrorDetails { get; private set; }

    /// <summary>
    /// Üretilen rapor dosyasının yolu
    /// </summary>
    public string? OutputFilePath { get; private set; }

    /// <summary>
    /// Üretilen rapor URL'si (dashboard linki)
    /// </summary>
    public string? OutputUrl { get; private set; }

    /// <summary>
    /// Raporda dönen kayıt sayısı
    /// </summary>
    public int? RecordCount { get; private set; }

    /// <summary>
    /// E-posta gönderildi mi?
    /// </summary>
    public bool EmailSent { get; private set; }

    /// <summary>
    /// Teams bildirimi gönderildi mi?
    /// </summary>
    public bool TeamsSent { get; private set; }

    // Navigation property
    public ScheduledReport? ScheduledReport { get; private set; }

    // EF Core constructor
    private ScheduledReportLog()
    {
    }

    /// <summary>
    /// Factory method - yeni log kaydı oluşturur
    /// </summary>
    internal static ScheduledReportLog Create(Guid scheduledReportId)
    {
        if (scheduledReportId == Guid.Empty)
            throw new ArgumentException("ScheduledReportId cannot be empty", nameof(scheduledReportId));

        return new ScheduledReportLog
        {
            Id = Guid.NewGuid(),
            ScheduledReportId = scheduledReportId,
            StartedAt = DateTime.UtcNow,
            IsSuccess = false,
            EmailSent = false,
            TeamsSent = false
        };
    }

    /// <summary>
    /// Başarılı tamamlanmayı kaydeder
    /// </summary>
    public void MarkAsSuccess(string? outputFilePath = null, string? outputUrl = null, int? recordCount = null)
    {
        CompletedAt = DateTime.UtcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        IsSuccess = true;
        OutputFilePath = outputFilePath;
        OutputUrl = outputUrl;
        RecordCount = recordCount;
    }

    /// <summary>
    /// Başarısız tamamlanmayı kaydeder
    /// </summary>
    public void MarkAsFailed(string errorMessage, string? errorDetails = null)
    {
        CompletedAt = DateTime.UtcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        IsSuccess = false;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    /// <summary>
    /// E-posta gönderimini kaydeder
    /// </summary>
    public void MarkEmailSent()
    {
        EmailSent = true;
    }

    /// <summary>
    /// Teams gönderimini kaydeder
    /// </summary>
    public void MarkTeamsSent()
    {
        TeamsSent = true;
    }
}
