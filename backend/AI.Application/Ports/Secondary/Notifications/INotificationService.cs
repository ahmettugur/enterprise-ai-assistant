namespace AI.Application.Ports.Secondary.Notifications;

/// <summary>
/// Unified notification service interface for email and Teams notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends an email notification
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a Teams notification
    /// </summary>
    Task SendTeamsAsync(string webhookUrl, object payload, CancellationToken cancellationToken = default);
}
