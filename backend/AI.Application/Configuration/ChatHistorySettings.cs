namespace AI.Application.Configuration;

/// <summary>
/// Chat history konfigürasyon ayarları
/// </summary>
public class ChatHistorySettings
{
    /// <summary>
    /// Depolama modu: InMemory, PostgreSQL, PostgreSQLWithRedis
    /// </summary>
    public string StorageMode { get; set; } = "";
    
    /// <summary>
    /// Cache süresi
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Metriklerin etkinleştirilip etkinleştirilmeyeceği
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
    
    /// <summary>
    /// PostgreSQL kullanılıp kullanılmayacağını kontrol eder (PostgreSQL veya PostgreSQLWithRedis)
    /// </summary>
    public bool UsePostgreSQL => StorageMode.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                                StorageMode.Equals("PostgreSQLWithRedis", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// InMemory kullanılıp kullanılmayacağını kontrol eder
    /// </summary>
    public bool UseInMemory => StorageMode.Equals("InMemory", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// PostgreSQL + Redis kombinasyonu kullanılıp kullanılmayacağını kontrol eder
    /// </summary>
    public bool UsePostgreSQLWithRedis => StorageMode.Equals("PostgreSQLWithRedis", StringComparison.OrdinalIgnoreCase);
    
    /// <summary>
    /// Redis cache kullanılıp kullanılmayacağını kontrol eder
    /// </summary>
    public bool UseRedisCache => UsePostgreSQLWithRedis;
}