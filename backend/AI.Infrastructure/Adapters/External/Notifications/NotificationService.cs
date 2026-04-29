using AI.Application.Ports.Secondary.Notifications;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.External.Notifications;

/// <summary>
/// Unified notification service that delegates to email and Teams services
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IEmailNotificationService _emailService;
    private readonly ITeamsNotificationService _teamsService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailNotificationService emailService,
        ITeamsNotificationService teamsService,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email to: {To}, Subject: {Subject}", to, subject);
            
            // IEmailNotificationService sadece spesifik rapor metodlarını içeriyor.
            // Genel bir SendEmail metodu yok. Bu yüzden SendToMultipleAsync kullanıyoruz.
            await _emailService.SendToMultipleAsync(new[] { to }, subject, body, cancellationToken);
            
            _logger.LogInformation("Successfully sent email to: {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to: {To}", to);
            throw;
        }
    }

    public async Task SendTeamsAsync(
        string webhookUrl,
        object payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                _logger.LogWarning("Teams webhook URL is empty, skipping notification");
                return;
            }

            _logger.LogInformation("Sending Teams notification to: {WebhookUrl}", webhookUrl);
            
            // ITeamsNotificationService'de genel bir SendNotificationAsync yok.
            // SendMessageAsync kullanıyoruz. Payload string'e çevrilmeli.
            var message = payload is string str ? str : System.Text.Json.JsonSerializer.Serialize(payload);
            await _teamsService.SendMessageAsync(webhookUrl, "Notification", message, null, cancellationToken);
            
            _logger.LogInformation("Successfully sent Teams notification to: {WebhookUrl}", webhookUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams notification to: {WebhookUrl}", webhookUrl);
            throw;
        }
    }
}
