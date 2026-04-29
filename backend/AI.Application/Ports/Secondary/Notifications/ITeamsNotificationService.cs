namespace AI.Application.Ports.Secondary.Notifications;

/// <summary>
/// Teams bildirim servisi interface'i
/// </summary>
public interface ITeamsNotificationService
{
    /// <summary>
    /// Rapor tamamlandı bildirimi gönderir
    /// </summary>
    Task<bool> SendReportCompletedAsync(
        string webhookUrl,
        string reportName,
        string reportUrl,
        int recordCount,
        TimeSpan duration,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rapor başarısız bildirimi gönderir
    /// </summary>
    Task<bool> SendReportFailedAsync(
        string webhookUrl,
        string reportName,
        string errorMessage,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Özel mesaj gönderir
    /// </summary>
    Task<bool> SendMessageAsync(
        string webhookUrl,
        string title,
        string message,
        string? color = null,
        CancellationToken cancellationToken = default);
}
