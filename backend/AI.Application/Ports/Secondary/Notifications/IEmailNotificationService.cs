namespace AI.Application.Ports.Secondary.Notifications;

/// <summary>
/// E-posta bildirim servisi interface'i
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Rapor tamamlandı e-postası gönderir
    /// </summary>
    Task<bool> SendReportCompletedAsync(
        string toEmail,
        string reportName,
        string reportUrl,
        int recordCount,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rapor başarısız e-postası gönderir
    /// </summary>
    Task<bool> SendReportFailedAsync(
        string toEmail,
        string reportName,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Çoklu alıcıya e-posta gönderir
    /// </summary>
    Task<bool> SendToMultipleAsync(
        IEnumerable<string> toEmails,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
