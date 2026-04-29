using System.Text;
using System.Text.Json;
using AI.Application.Ports.Secondary.Notifications;
using AI.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Adapters.External.Notifications;

/// <summary>
/// Microsoft Teams Incoming Webhook kullanarak bildirim gönderen servis
/// </summary>
public sealed class TeamsNotificationService : ITeamsNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly TeamsSettings _settings;
    private readonly ILogger<TeamsNotificationService> _logger;

    public TeamsNotificationService(
        HttpClient httpClient,
        IOptions<TeamsSettings> settings,
        ILogger<TeamsNotificationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> SendReportCompletedAsync(
        string webhookUrl,
        string reportName,
        string reportUrl,
        int recordCount,
        TimeSpan duration,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Teams bildirimleri devre dışı, bildirim gönderilmedi");
            return false;
        }

        var card = CreateAdaptiveCard(
            title: "✅ Rapor Hazır",
            subtitle: reportName,
            color: "Good",
            facts: new Dictionary<string, string>
            {
                { "Kayıt Sayısı", recordCount.ToString("N0") },
                { "Süre", FormatDuration(duration) },
                { "Kullanıcı", userId ?? "Sistem" },
                { "Zaman", DateTime.Now.ToString("dd.MM.yyyy HH:mm") }
            },
            buttons: new List<AdaptiveCardButton>
            {
                new("Raporu Aç", reportUrl)
            });

        return await SendCardAsync(webhookUrl, card, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SendReportFailedAsync(
        string webhookUrl,
        string reportName,
        string errorMessage,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Teams bildirimleri devre dışı, bildirim gönderilmedi");
            return false;
        }

        var card = CreateAdaptiveCard(
            title: "❌ Rapor Başarısız",
            subtitle: reportName,
            color: "Attention",
            facts: new Dictionary<string, string>
            {
                { "Hata", TruncateMessage(errorMessage, 200) },
                { "Kullanıcı", userId ?? "Sistem" },
                { "Zaman", DateTime.Now.ToString("dd.MM.yyyy HH:mm") }
            });

        return await SendCardAsync(webhookUrl, card, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SendMessageAsync(
        string webhookUrl,
        string title,
        string message,
        string? color = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Teams bildirimleri devre dışı, bildirim gönderilmedi");
            return false;
        }

        var card = CreateAdaptiveCard(
            title: title,
            subtitle: message,
            color: color ?? "Default");

        return await SendCardAsync(webhookUrl, card, cancellationToken);
    }

    /// <summary>
    /// Adaptive Card oluşturur
    /// </summary>
    private static object CreateAdaptiveCard(
        string title,
        string subtitle,
        string color,
        Dictionary<string, string>? facts = null,
        List<AdaptiveCardButton>? buttons = null)
    {
        var body = new List<object>
        {
            new
            {
                type = "TextBlock",
                size = "Large",
                weight = "Bolder",
                text = title,
                wrap = true,
                style = "heading"
            },
            new
            {
                type = "TextBlock",
                text = subtitle,
                wrap = true,
                spacing = "Small"
            }
        };

        // Facts (key-value pairs)
        if (facts != null && facts.Count > 0)
        {
            body.Add(new
            {
                type = "FactSet",
                facts = facts.Select(f => new { title = f.Key, value = f.Value }).ToList(),
                spacing = "Medium"
            });
        }

        // Actions (buttons)
        var actions = new List<object>();
        if (buttons != null && buttons.Count > 0)
        {
            foreach (var button in buttons)
            {
                actions.Add(new
                {
                    type = "Action.OpenUrl",
                    title = button.Title,
                    url = button.Url
                });
            }
        }

        // Full Adaptive Card message for Teams
        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    contentUrl = (string?)null,
                    content = new
                    {
                        type = "AdaptiveCard",
                        body = body,
                        actions = actions.Count > 0 ? actions : null,
                        msteams = new
                        {
                            width = "Full"
                        },
                        version = "1.4",
                        schema = "http://adaptivecards.io/schemas/adaptive-card.json"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Card'ı Teams webhook'a gönderir
    /// </summary>
    private async Task<bool> SendCardAsync(string webhookUrl, object card, CancellationToken cancellationToken)
    {
        try
        {
            var url = string.IsNullOrEmpty(webhookUrl) ? _settings.DefaultWebhookUrl : webhookUrl;
            
            if (string.IsNullOrEmpty(url))
            {
                _logger.LogWarning("Teams webhook URL'si bulunamadı, bildirim gönderilemedi");
                return false;
            }

            var json = JsonSerializer.Serialize(card, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogDebug("Teams'e gönderilen payload: {Payload}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Teams bildirimi başarıyla gönderildi");
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Teams bildirimi gönderilemedi - Status: {Status}, Response: {Response}",
                response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teams bildirimi gönderilirken hata oluştu");
            return false;
        }
    }

    /// <summary>
    /// Süreyi okunabilir formata çevirir
    /// </summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:N0} ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:N1} saniye";
        if (duration.TotalHours < 1)
            return $"{duration.TotalMinutes:N1} dakika";
        return $"{duration.TotalHours:N1} saat";
    }

    /// <summary>
    /// Mesajı belirtilen uzunlukta keser
    /// </summary>
    private static string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;
        return message.Length <= maxLength ? message : message[..maxLength] + "...";
    }
}

/// <summary>
/// Adaptive Card button
/// </summary>
internal record AdaptiveCardButton(string Title, string Url);
