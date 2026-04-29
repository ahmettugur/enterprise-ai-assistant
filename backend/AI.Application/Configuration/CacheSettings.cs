using System.Text.Json.Serialization;

namespace AI.Application.Configuration;

/// <summary>
/// Cache yapılandırma ayarları
/// appsettings.json'dan okunur, hardcoded değerler yerine kullanılır
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Chat history cache süresi (dakika)
    /// </summary>
    public int ChatHistoryTtlMinutes { get; set; } = 15;
    
    /// <summary>
    /// Chat history cache süresi
    /// </summary>
    [JsonIgnore]
    public TimeSpan ChatHistoryTtl => TimeSpan.FromMinutes(ChatHistoryTtlMinutes);

    /// <summary>
    /// Conversation metadata cache süresi (dakika)
    /// </summary>
    public int ConversationMetadataTtlMinutes { get; set; } = 30;
    
    /// <summary>
    /// Conversation metadata cache süresi
    /// </summary>
    [JsonIgnore]
    public TimeSpan ConversationMetadataTtl => TimeSpan.FromMinutes(ConversationMetadataTtlMinutes);

    /// <summary>
    /// Conversation DTO cache süresi (dakika)
    /// </summary>
    public int ConversationDtoTtlMinutes { get; set; } = 30;
    
    /// <summary>
    /// Conversation DTO cache süresi
    /// </summary>
    [JsonIgnore]
    public TimeSpan ConversationDtoTtl => TimeSpan.FromMinutes(ConversationDtoTtlMinutes);

    /// <summary>
    /// L1 (Memory) cache süresi (dakika)
    /// </summary>
    public int MemoryCacheTtlMinutes { get; set; } = 5;
    
    /// <summary>
    /// L1 (Memory) cache süresi
    /// </summary>
    [JsonIgnore]
    public TimeSpan MemoryCacheTtl => TimeSpan.FromMinutes(MemoryCacheTtlMinutes);

    /// <summary>
    /// L2 (Redis) varsayılan cache süresi (dakika)
    /// </summary>
    public int DefaultRedisTtlMinutes { get; set; } = 60;
    
    /// <summary>
    /// L2 (Redis) varsayılan cache süresi
    /// </summary>
    [JsonIgnore]
    public TimeSpan DefaultRedisTtl => TimeSpan.FromMinutes(DefaultRedisTtlMinutes);

    /// <summary>
    /// Sliding expiration etkin mi?
    /// Aktifse, sık erişilen veriler cache'de daha uzun kalır
    /// </summary>
    public bool EnableSlidingExpiration { get; set; } = true;

    /// <summary>
    /// Sliding expiration süresi (dakika)
    /// </summary>
    public int SlidingExpirationTtlMinutes { get; set; } = 30;
    
    /// <summary>
    /// Sliding expiration süresi
    /// </summary>
    [JsonIgnore]
    public TimeSpan SlidingExpirationTtl => TimeSpan.FromMinutes(SlidingExpirationTtlMinutes);

    /// <summary>
    /// Compression eşiği (byte)
    /// Bu değerden büyük veriler sıkıştırılır
    /// Varsayılan: 1KB
    /// </summary>
    public int CompressionThresholdBytes { get; set; } = 1024;

    /// <summary>
    /// Compression etkin mi?
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Cache stampede koruması etkin mi?
    /// </summary>
    public bool EnableStampedeProtection { get; set; } = true;

    /// <summary>
    /// Stampede lock timeout süresi (saniye)
    /// </summary>
    public int StampedeLockTimeoutSeconds { get; set; } = 5;
    
    /// <summary>
    /// Stampede lock timeout süresi
    /// </summary>
    [JsonIgnore]
    public TimeSpan StampedeLockTimeout => TimeSpan.FromSeconds(StampedeLockTimeoutSeconds);

    /// <summary>
    /// Expired key temizleme aralığı (dakika)
    /// </summary>
    public int ExpiredKeyCleanupIntervalMinutes { get; set; } = 10;
    
    /// <summary>
    /// Expired key temizleme aralığı
    /// </summary>
    [JsonIgnore]
    public TimeSpan ExpiredKeyCleanupInterval => TimeSpan.FromMinutes(ExpiredKeyCleanupIntervalMinutes);

    /// <summary>
    /// Maximum tracked key sayısı
    /// Memory leak önleme için üst limit
    /// </summary>
    public int MaxTrackedKeys { get; set; } = 10000;
}
