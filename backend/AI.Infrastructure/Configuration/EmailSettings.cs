namespace AI.Infrastructure.Configuration;

/// <summary>
/// E-posta gönderim ayarları
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// E-posta gönderimi aktif mi
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SMTP sunucu adresi
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SSL kullan
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// SMTP kullanıcı adı
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP şifre
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gönderen e-posta adresi
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gönderen adı
    /// </summary>
    public string FromName { get; set; } = "AI Rapor Sistemi";
}
