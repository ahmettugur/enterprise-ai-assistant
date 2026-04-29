using AI.Domain.Common;

namespace AI.Domain.Scheduling;

/// <summary>
/// Zamanlanmış rapor entity'si
/// Kullanıcının belirli aralıklarla otomatik çalıştırılmasını istediği raporları temsil eder
/// </summary>
public sealed class ScheduledReport : AggregateRoot<Guid>
{

    /// <summary>
    /// Raporu oluşturan kullanıcının ID'si
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// Rapor adı (kullanıcı tarafından belirlenir)
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Kullanıcının orijinal promptu
    /// </summary>
    public string OriginalPrompt { get; private set; } = null!;

    /// <summary>
    /// LLM tarafından üretilen SQL sorgusu
    /// </summary>
    public string SqlQuery { get; private set; } = null!;

    /// <summary>
    /// Orijinal mesajın ID'si (referans için)
    /// </summary>
    public Guid? OriginalMessageId { get; private set; }

    /// <summary>
    /// Orijinal conversation ID'si
    /// </summary>
    public Guid? OriginalConversationId { get; private set; }

    /// <summary>
    /// Cron expression (örn: "0 9 * * 1" - Her pazartesi saat 9:00)
    /// </summary>
    public string CronExpression { get; private set; } = null!;

    /// <summary>
    /// Kullanılan rapor servis tipi (Oracle, SqlServer, vb.)
    /// </summary>
    public string ReportServiceType { get; private set; } = null!;

    /// <summary>
    /// Veritabanı tipi (oracle, sqlserver, postgresql)
    /// </summary>
    public string ReportDatabaseType { get; private set; } = null!;

    /// <summary>
    /// Veritabanı servis tipi (northwind, adventure_works, vb.)
    /// </summary>
    public string ReportDatabaseServiceType { get; private set; } = null!;

    /// <summary>
    /// Raporun aktif olup olmadığı
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Son çalışma zamanı
    /// </summary>
    public DateTime? LastRunAt { get; private set; }

    /// <summary>
    /// Bir sonraki planlanan çalışma zamanı
    /// </summary>
    public DateTime? NextRunAt { get; private set; }

    /// <summary>
    /// Toplam çalışma sayısı
    /// </summary>
    public int RunCount { get; private set; }

    /// <summary>
    /// Son çalışmanın başarılı olup olmadığı
    /// </summary>
    public bool? LastRunSuccess { get; private set; }

    /// <summary>
    /// Son hata mesajı (varsa)
    /// </summary>
    public string? LastErrorMessage { get; private set; }

    /// <summary>
    /// Bildirim e-posta adresi (opsiyonel)
    /// </summary>
    public string? NotificationEmail { get; private set; }

    /// <summary>
    /// Teams webhook URL'si (opsiyonel)
    /// </summary>
    public string? TeamsWebhookUrl { get; private set; }

    /// <summary>
    /// Oluşturulma zamanı
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Son güncelleme zamanı
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties - encapsulated collection
    private readonly List<ScheduledReportLog> _logs = [];
    public IReadOnlyCollection<ScheduledReportLog> Logs => _logs.AsReadOnly();

    // EF Core constructor
    private ScheduledReport()
    {
    }

    /// <summary>
    /// Factory method - yeni zamanlanmış rapor oluşturur
    /// </summary>
    public static ScheduledReport Create(
        string userId,
        string name,
        string originalPrompt,
        string sqlQuery,
        string cronExpression,
        string reportServiceType,
        string reportDatabaseType,
        string reportDatabaseServiceType,
        Guid? originalMessageId = null,
        Guid? originalConversationId = null,
        string? notificationEmail = null,
        string? teamsWebhookUrl = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(originalPrompt))
            throw new ArgumentException("OriginalPrompt cannot be empty", nameof(originalPrompt));
        if (string.IsNullOrWhiteSpace(sqlQuery))
            throw new ArgumentException("SqlQuery cannot be empty", nameof(sqlQuery));
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new ArgumentException("CronExpression cannot be empty", nameof(cronExpression));
        if (string.IsNullOrWhiteSpace(reportServiceType))
            throw new ArgumentException("ReportServiceType cannot be empty", nameof(reportServiceType));

        return new ScheduledReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            OriginalPrompt = originalPrompt,
            SqlQuery = sqlQuery,
            CronExpression = cronExpression,
            ReportServiceType = reportServiceType,
            ReportDatabaseType = reportDatabaseType,
            ReportDatabaseServiceType = reportDatabaseServiceType,
            OriginalMessageId = originalMessageId,
            OriginalConversationId = originalConversationId,
            NotificationEmail = notificationEmail,
            TeamsWebhookUrl = teamsWebhookUrl,
            IsActive = true,
            RunCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Yeni log kaydı oluşturur ve koleksiyona ekler (Aggregate Root pattern)
    /// </summary>
    public ScheduledReportLog AddLog()
    {
        var log = ScheduledReportLog.Create(Id);
        _logs.Add(log);
        return log;
    }

    /// <summary>
    /// Raporu aktif/pasif yapar
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cron expression'ı günceller
    /// </summary>
    public void UpdateSchedule(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new ArgumentException("CronExpression cannot be empty", nameof(cronExpression));

        CronExpression = cronExpression;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rapor adını günceller
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Bildirim ayarlarını günceller
    /// </summary>
    public void UpdateNotificationSettings(string? email, string? teamsWebhookUrl)
    {
        NotificationEmail = email;
        TeamsWebhookUrl = teamsWebhookUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Başarılı çalışmayı kaydeder
    /// </summary>
    public void RecordSuccessfulRun(DateTime? nextRunAt = null)
    {
        LastRunAt = DateTime.UtcNow;
        LastRunSuccess = true;
        LastErrorMessage = null;
        NextRunAt = nextRunAt;
        RunCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Başarısız çalışmayı kaydeder
    /// </summary>
    public void RecordFailedRun(string errorMessage, DateTime? nextRunAt = null)
    {
        LastRunAt = DateTime.UtcNow;
        LastRunSuccess = false;
        LastErrorMessage = errorMessage;
        NextRunAt = nextRunAt;
        RunCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// İlk veya sonraki çalışma zamanını ayarlar (çalışma kaydı olmadan)
    /// </summary>
    public void SetNextRunAt(DateTime? nextRunAt)
    {
        NextRunAt = nextRunAt;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// SQL sorgusunu günceller
    /// </summary>
    public void UpdateSqlQuery(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
            throw new ArgumentException("SqlQuery cannot be empty", nameof(sqlQuery));

        SqlQuery = sqlQuery;
        UpdatedAt = DateTime.UtcNow;
    }
}
