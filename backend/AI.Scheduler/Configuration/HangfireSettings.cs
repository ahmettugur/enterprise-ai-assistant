namespace AI.Scheduler.Configuration;

/// <summary>
/// Hangfire yapılandırma ayarları
/// </summary>
public sealed class HangfireSettings
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// PostgreSQL şema adı
    /// </summary>
    public string SchemaName { get; set; } = "hangfire";

    /// <summary>
    /// Dashboard yolu
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// Dashboard başlığı
    /// </summary>
    public string DashboardTitle { get; set; } = "AI Scheduler";

    /// <summary>
    /// Worker sayısı
    /// </summary>
    public int WorkerCount { get; set; } = 5;

    /// <summary>
    /// Kuyruk isimleri
    /// </summary>
    public string[] Queues { get; set; } = ["critical", "default", "reports"];

    /// <summary>
    /// Server adı
    /// </summary>
    public string ServerName { get; set; } = "AI-Scheduler-01";

    /// <summary>
    /// Heartbeat aralığı
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Server kontrol aralığı
    /// </summary>
    public TimeSpan ServerCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Schedule polling aralığı
    /// </summary>
    public TimeSpan SchedulePollingInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// İstatistik polling aralığı
    /// </summary>
    public TimeSpan StatsPollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Job süre dolumu kontrol aralığı
    /// </summary>
    public TimeSpan JobExpirationCheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Silinen job listesi boyutu
    /// </summary>
    public int DeletedListSize { get; set; } = 10000;

    /// <summary>
    /// Başarılı job listesi boyutu
    /// </summary>
    public int SucceededListSize { get; set; } = 10000;
}
