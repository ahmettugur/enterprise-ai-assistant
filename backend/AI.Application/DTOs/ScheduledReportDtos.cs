namespace AI.Application.DTOs;

/// <summary>
/// Zamanlanmış rapor oluşturma isteği
/// </summary>
public sealed class CreateScheduledReportDto
{
    /// <summary>
    /// Rapor adı
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Orijinal kullanıcı promptu
    /// </summary>
    public required string OriginalPrompt { get; init; }

    /// <summary>
    /// LLM tarafından üretilen SQL sorgusu
    /// </summary>
    public required string SqlQuery { get; init; }

    /// <summary>
    /// Cron expression (örn: "0 9 * * 1" - Her pazartesi saat 9:00)
    /// </summary>
    public required string CronExpression { get; init; }

    /// <summary>
    /// Rapor servis tipi (sales vb.)
    /// </summary>
    public required string ReportServiceType { get; init; }

    /// <summary>
    /// Rapor veritabanı tipi (Oracle, SqlServer, vb.)
    /// </summary>
    public required string ReportDatabaseType { get; init; }

    /// <summary>
    /// Rapor veritabanı servis tipi (adventureworks, vb.)
    /// </summary>
    public required string ReportDatabaseServiceType { get; init; }

    /// <summary>
    /// Orijinal mesaj ID'si (opsiyonel)
    /// </summary>
    public Guid? OriginalMessageId { get; init; }

    /// <summary>
    /// Orijinal conversation ID'si (opsiyonel)
    /// </summary>
    public Guid? OriginalConversationId { get; init; }

    /// <summary>
    /// Bildirim e-posta adresi (opsiyonel)
    /// </summary>
    public string? NotificationEmail { get; init; }

    /// <summary>
    /// Teams webhook URL'si (opsiyonel)
    /// </summary>
    public string? TeamsWebhookUrl { get; init; }
}

/// <summary>
/// Mevcut bir mesajdan zamanlanmış rapor oluşturma isteği (Frontend'den kullanılacak)
/// </summary>
public sealed class CreateScheduledReportFromMessageDto
{
    /// <summary>
    /// Rapor adı
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Açıklama (opsiyonel)
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Orijinal mesaj ID'si - Bu mesajdaki SQL sorgusu kullanılacak
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Cron expression (örn: "0 9 * * 1-5" - Hafta içi her gün 09:00)
    /// </summary>
    public required string CronExpression { get; init; }

    /// <summary>
    /// Bildirim e-posta adresleri (virgülle ayrılmış)
    /// </summary>
    public List<string>? RecipientEmails { get; init; }

    /// <summary>
    /// Teams'e gönder
    /// </summary>
    public bool SendToTeams { get; init; }

    /// <summary>
    /// Aktif mi
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Zamanlanmış rapor güncelleme isteği
/// </summary>
public sealed class UpdateScheduledReportDto
{
    /// <summary>
    /// Rapor adı
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Cron expression
    /// </summary>
    public string? CronExpression { get; init; }

    /// <summary>
    /// Bildirim e-posta adresi
    /// </summary>
    public string? NotificationEmail { get; init; }

    /// <summary>
    /// Teams webhook URL'si
    /// </summary>
    public string? TeamsWebhookUrl { get; init; }

    /// <summary>
    /// SQL sorgusunu güncelle (dikkatli kullanın)
    /// </summary>
    public string? SqlQuery { get; init; }
}

/// <summary>
/// Zamanlanmış rapor yanıt DTO'su
/// </summary>
public sealed class ScheduledReportDto
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string OriginalPrompt { get; init; } = null!;
    public string SqlQuery { get; init; } = null!;
    public Guid? OriginalMessageId { get; init; }
    public Guid? OriginalConversationId { get; init; }
    public string CronExpression { get; init; } = null!;
    public string CronDescription { get; init; } = null!; // İnsan okunabilir açıklama
    public string ReportServiceType { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime? LastRunAt { get; init; }
    public DateTime? NextRunAt { get; init; }
    public int RunCount { get; init; }
    public bool? LastRunSuccess { get; init; }
    public string? LastErrorMessage { get; init; }
    public string? NotificationEmail { get; init; }
    public string? TeamsWebhookUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Zamanlanmış rapor detay DTO'su (loglarla birlikte)
/// </summary>
public sealed class ScheduledReportDetailDto
{
    public ScheduledReportDto Report { get; init; } = null!;
    public List<ScheduledReportLogDto> RecentLogs { get; init; } = [];
}

/// <summary>
/// Zamanlanmış rapor log DTO'su
/// </summary>
public sealed class ScheduledReportLogDto
{
    public Guid Id { get; init; }
    public Guid ScheduledReportId { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public long? DurationMs { get; init; }
    public string? DurationFormatted { get; init; } // "2.5 saniye" gibi
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputFilePath { get; init; }
    public string? OutputUrl { get; init; }
    public int? RecordCount { get; init; }
    public bool EmailSent { get; init; }
    public bool TeamsSent { get; init; }
}
