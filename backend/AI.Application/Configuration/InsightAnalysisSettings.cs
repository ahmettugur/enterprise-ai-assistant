namespace AI.Application.Configuration;

/// <summary>
/// Insight analizi için konfigürasyon ayarları.
/// Token-aware chunking ve paralel işlem ayarlarını içerir.
/// </summary>
public class InsightAnalysisSettings
{
    /// <summary>
    /// Token limit ayarları
    /// </summary>
    public TokenLimitSettings TokenLimits { get; set; } = new();
    
    /// <summary>
    /// Chunking ayarları
    /// </summary>
    public ChunkingSettings Chunking { get; set; } = new();
    
    /// <summary>
    /// İşlem ayarları
    /// </summary>
    public ProcessingSettings Processing { get; set; } = new();
}

/// <summary>
/// Token limit konfigürasyonu - GPT-4.1 için optimize edilmiş
/// </summary>
public class TokenLimitSettings
{
    /// <summary>
    /// Model context limiti (GPT-4.1: 1M token)
    /// </summary>
    public int ModelContextLimit { get; set; } = 1_000_000;
    
    /// <summary>
    /// System prompt için ayrılan token
    /// </summary>
    public int SystemPromptTokens { get; set; } = 3_000;
    
    /// <summary>
    /// LLM çıktısı için ayrılan token
    /// </summary>
    public int OutputReserveTokens { get; set; } = 32_000;
    
    /// <summary>
    /// Güvenlik payı yüzdesi
    /// </summary>
    public int SafetyMarginPercent { get; set; } = 10;
    
    /// <summary>
    /// Chunk başına hedef token sayısı
    /// </summary>
    public int TargetTokensPerChunk { get; set; } = 80_000;
    
    /// <summary>
    /// Tek seferde gönderilebilecek maksimum token (chunk'lama yapılmaz)
    /// </summary>
    public int SinglePassThreshold { get; set; } = 700_000;
    
    /// <summary>
    /// Karakter başına ortalama token oranı (JSON için ~0.3)
    /// </summary>
    public double TokensPerCharacter { get; set; } = 0.3;
    
    /// <summary>
    /// Kullanılabilir token hesapla
    /// </summary>
    public int CalculateAvailableTokens()
    {
        var safetyMargin = (int)(ModelContextLimit * SafetyMarginPercent / 100.0);
        return ModelContextLimit - SystemPromptTokens - OutputReserveTokens - safetyMargin;
    }
}

/// <summary>
/// Chunking konfigürasyonu
/// </summary>
public class ChunkingSettings
{
    /// <summary>
    /// Maksimum chunk sayısı (maliyet kontrolü)
    /// </summary>
    public int MaxChunks { get; set; } = 10;
    
    /// <summary>
    /// Chunk başına minimum satır sayısı
    /// </summary>
    public int MinRowsPerChunk { get; set; } = 200;
    
    /// <summary>
    /// Chunk başına maksimum satır sayısı
    /// </summary>
    public int MaxRowsPerChunk { get; set; } = 10_000;
    
    /// <summary>
    /// Sampling etkinleştirme
    /// </summary>
    public bool EnableSampling { get; set; } = true;
    
    /// <summary>
    /// Sampling stratejisi: "Random", "Stratified", "Systematic"
    /// </summary>
    public string SamplingStrategy { get; set; } = "Stratified";
    
    /// <summary>
    /// Minimum sampling oranı (örn: 0.1 = en az %10 veri)
    /// </summary>
    public double MinSamplingRate { get; set; } = 0.1;

    /// <summary>
    /// Token bazlı chunking kullanılsın mı?
    /// </summary>
    public bool UseTokenBasedChunking { get; set; } = false;
}

/// <summary>
/// İşlem konfigürasyonu
/// </summary>
public class ProcessingSettings
{
    /// <summary>
    /// Paralel chunk analizi etkinleştirme
    /// </summary>
    public bool EnableParallelChunkAnalysis { get; set; } = true;
    
    /// <summary>
    /// Maksimum paralel işlem sayısı
    /// </summary>
    public int MaxParallelism { get; set; } = 5;
    
    /// <summary>
    /// LLM çağrıları arasında minimum bekleme (ms)
    /// </summary>
    public int MinDelayBetweenCallsMs { get; set; } = 100;
    
    /// <summary>
    /// Chunk analizi timeout süresi (saniye)
    /// </summary>
    public int ChunkAnalysisTimeoutSeconds { get; set; } = 120;
    
    /// <summary>
    /// Hata durumunda retry sayısı
    /// </summary>
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// Retry bekleme süresi (ms)
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Progress bildirimlerini SignalR ile gönder
    /// </summary>
    public bool EnableProgressNotifications { get; set; } = true;
}
