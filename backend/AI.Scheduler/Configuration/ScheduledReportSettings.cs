namespace AI.Scheduler.Configuration;

/// <summary>
/// Zamanlanmış rapor çalıştırma ayarları
/// </summary>
public sealed class ScheduledReportSettings
{
    public const string SectionName = "ScheduledReports";

    /// <summary>
    /// Aynı anda çalışabilecek maksimum rapor sayısı
    /// </summary>
    public int MaxConcurrentReports { get; set; } = 3;

    /// <summary>
    /// Varsayılan timeout süresi (dakika)
    /// </summary>
    public int DefaultTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Hata durumunda tekrar deneme sayısı
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Tekrar denemeler arası bekleme süresi (dakika)
    /// </summary>
    public int RetryDelayMinutes { get; set; } = 5;

    /// <summary>
    /// Rapor çıktı dizini
    /// </summary>
    public string OutputDirectory { get; set; } = "wwwroot/reports";

    /// <summary>
    /// Base URL (rapor linklerinde kullanılacak)
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:7041";
}
