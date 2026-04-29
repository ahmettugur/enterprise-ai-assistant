namespace AI.Infrastructure.Configuration;

/// <summary>
/// Microsoft Teams entegrasyon ayarları
/// </summary>
public sealed class TeamsSettings
{
    public const string SectionName = "Teams";

    /// <summary>
    /// Teams entegrasyonu aktif mi
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Varsayılan webhook URL'si
    /// </summary>
    public string DefaultWebhookUrl { get; set; } = string.Empty;
}
