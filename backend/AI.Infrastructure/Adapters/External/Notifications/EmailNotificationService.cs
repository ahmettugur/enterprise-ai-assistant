using System.Net;
using System.Net.Mail;
using AI.Application.Ports.Secondary.Notifications;
using AI.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Adapters.External.Notifications;

/// <summary>
/// E-posta bildirim servisi implementasyonu
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IOptions<EmailSettings> settings,
        ILogger<EmailNotificationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendReportCompletedAsync(
        string toEmail,
        string reportName,
        string reportUrl,
        int recordCount,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var subject = $"✅ Rapor Hazır: {reportName}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0; }}
        .info-box {{ background: white; border-radius: 8px; padding: 20px; margin: 15px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.05); }}
        .info-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #f1f5f9; }}
        .info-row:last-child {{ border-bottom: none; }}
        .info-label {{ color: #64748b; font-weight: 500; }}
        .info-value {{ color: #1e293b; font-weight: 600; }}
        .btn {{ display: inline-block; background: #3b82f6; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: 600; margin-top: 20px; }}
        .btn:hover {{ background: #2563eb; }}
        .footer {{ text-align: center; padding: 20px; color: #64748b; font-size: 12px; }}
        .success-icon {{ font-size: 48px; margin-bottom: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='success-icon'>✅</div>
            <h1>Raporunuz Hazır!</h1>
        </div>
        <div class='content'>
            <div class='info-box'>
                <div class='info-row'>
                    <span class='info-label'>📊 Rapor Adı</span>
                    <span class='info-value'>{reportName}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>📈 Kayıt Sayısı</span>
                    <span class='info-value'>{recordCount:N0}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>⏱️ İşlem Süresi</span>
                    <span class='info-value'>{FormatDuration(duration)}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>📅 Oluşturulma</span>
                    <span class='info-value'>{DateTime.Now:dd.MM.yyyy HH:mm}</span>
                </div>
            </div>
            <div style='text-align: center;'>
                <a href='{reportUrl}' class='btn'>📥 Raporu İndir</a>
            </div>
        </div>
        <div class='footer'>
            <p>Bu e-posta otomatik olarak gönderilmiştir.</p>
            <p>AI Rapor Sistemi - Zamanlanmış Raporlar</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendReportFailedAsync(
        string toEmail,
        string reportName,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var subject = $"❌ Rapor Hatası: {reportName}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0; }}
        .error-box {{ background: #fef2f2; border: 1px solid #fecaca; border-radius: 8px; padding: 20px; margin: 15px 0; }}
        .error-title {{ color: #dc2626; font-weight: 600; margin-bottom: 10px; }}
        .error-message {{ color: #7f1d1d; font-family: monospace; font-size: 13px; background: white; padding: 15px; border-radius: 4px; overflow-x: auto; }}
        .info-box {{ background: white; border-radius: 8px; padding: 20px; margin: 15px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.05); }}
        .info-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #f1f5f9; }}
        .info-row:last-child {{ border-bottom: none; }}
        .info-label {{ color: #64748b; font-weight: 500; }}
        .info-value {{ color: #1e293b; font-weight: 600; }}
        .footer {{ text-align: center; padding: 20px; color: #64748b; font-size: 12px; }}
        .error-icon {{ font-size: 48px; margin-bottom: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='error-icon'>❌</div>
            <h1>Rapor Oluşturulamadı</h1>
        </div>
        <div class='content'>
            <div class='info-box'>
                <div class='info-row'>
                    <span class='info-label'>📊 Rapor Adı</span>
                    <span class='info-value'>{reportName}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>📅 Tarih</span>
                    <span class='info-value'>{DateTime.Now:dd.MM.yyyy HH:mm}</span>
                </div>
            </div>
            <div class='error-box'>
                <div class='error-title'>⚠️ Hata Detayı</div>
                <div class='error-message'>{System.Web.HttpUtility.HtmlEncode(errorMessage)}</div>
            </div>
            <p style='color: #64748b; font-size: 14px;'>
                Lütfen sistem yöneticinize başvurun veya rapor ayarlarını kontrol edin.
            </p>
        </div>
        <div class='footer'>
            <p>Bu e-posta otomatik olarak gönderilmiştir.</p>
            <p>AI Rapor Sistemi - Zamanlanmış Raporlar</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendToMultipleAsync(
        IEnumerable<string> toEmails,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var success = true;
        
        foreach (var email in toEmails)
        {
            var result = await SendEmailAsync(email, subject, htmlBody, cancellationToken);
            if (!result) success = false;
        }

        return success;
    }

    private async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("E-posta servisi devre dışı, e-posta gönderilmedi: {ToEmail}", toEmail);
            return false;
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30 saniye
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation(
                "E-posta başarıyla gönderildi: {ToEmail}, Konu: {Subject}",
                toEmail,
                subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "E-posta gönderilemedi: {ToEmail}, Konu: {Subject}",
                toEmail,
                subject);

            return false;
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
            return $"{duration.TotalSeconds:F1} saniye";
        
        if (duration.TotalMinutes < 60)
            return $"{duration.TotalMinutes:F1} dakika";
        
        return $"{duration.TotalHours:F1} saat";
    }
}
