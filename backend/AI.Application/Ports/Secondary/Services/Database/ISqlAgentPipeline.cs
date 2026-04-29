namespace AI.Application.Ports.Secondary.Services.Database;

/// <summary>
/// SQL Agent'ları koordine eden pipeline interface'i.
/// Validation ve Optimization agent'larını sırayla çalıştırır.
/// </summary>
public interface ISqlAgentPipeline
{
    /// <summary>
    /// SQL sorgusunu pipeline'dan geçirir (validate + optimize).
    /// </summary>
    /// <param name="sql">İşlenecek SQL sorgusu</param>
    /// <param name="databaseType">Veritabanı tipi (oracle, sqlserver)</param>
    /// <param name="options">Pipeline seçenekleri</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Pipeline sonucu</returns>
    Task<SqlPipelineResult> ProcessAsync(
        string sql,
        string databaseType,
        SqlPipelineOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Pipeline seçenekleri
/// </summary>
public record SqlPipelineOptions
{
    /// <summary>
    /// Validasyon aktif mi? (varsayılan: true)
    /// </summary>
    public bool EnableValidation { get; init; } = true;
    
    /// <summary>
    /// Optimizasyon aktif mi? (varsayılan: true)
    /// </summary>
    public bool EnableOptimization { get; init; } = true;
    
    /// <summary>
    /// Güvenlik kontrolü aktif mi? (varsayılan: true)
    /// </summary>
    public bool EnableSecurityCheck { get; init; } = true;
    
    /// <summary>
    /// Otomatik düzeltme yapılsın mı? (varsayılan: true)
    /// </summary>
    public bool EnableAutoCorrection { get; init; } = true;
    
    /// <summary>
    /// Schema bilgisi
    /// </summary>
    public string? SchemaInfo { get; init; }
    
    /// <summary>
    /// Maksimum retry sayısı
    /// </summary>
    public int MaxRetries { get; init; } = 2;
    
    /// <summary>
    /// Varsayılan seçenekler
    /// </summary>
    public static SqlPipelineOptions Default => new();
    
    /// <summary>
    /// Sadece validasyon
    /// </summary>
    public static SqlPipelineOptions ValidationOnly => new()
    {
        EnableValidation = true,
        EnableOptimization = false
    };
    
    /// <summary>
    /// Sadece optimizasyon
    /// </summary>
    public static SqlPipelineOptions OptimizationOnly => new()
    {
        EnableValidation = false,
        EnableOptimization = true
    };
}

/// <summary>
/// Pipeline sonucu
/// </summary>
public record SqlPipelineResult
{
    /// <summary>
    /// Pipeline başarılı mı?
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// Orijinal SQL sorgusu
    /// </summary>
    public string OriginalSql { get; init; } = string.Empty;
    
    /// <summary>
    /// Son SQL sorgusu (validate ve optimize edilmiş)
    /// </summary>
    public string FinalSql { get; init; } = string.Empty;
    
    /// <summary>
    /// Validasyon sonucu
    /// </summary>
    public SqlValidationResult? ValidationResult { get; init; }
    
    /// <summary>
    /// Optimizasyon sonucu
    /// </summary>
    public SqlOptimizationResult? OptimizationResult { get; init; }
    
    /// <summary>
    /// Pipeline aşamaları
    /// </summary>
    public List<SqlPipelineStage> Stages { get; init; } = [];
    
    /// <summary>
    /// Toplam işlem süresi (ms)
    /// </summary>
    public long ProcessingTimeMs { get; init; }
    
    /// <summary>
    /// Özet açıklama
    /// </summary>
    public string? Summary { get; init; }
    
    /// <summary>
    /// Hata mesajı (başarısız ise)
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Başarılı sonuç oluşturur
    /// </summary>
    public static SqlPipelineResult Success(
        string originalSql,
        string finalSql,
        SqlValidationResult? validationResult,
        SqlOptimizationResult? optimizationResult,
        List<SqlPipelineStage> stages,
        long processingTimeMs,
        string? summary = null) => new()
    {
        IsSuccess = true,
        OriginalSql = originalSql,
        FinalSql = finalSql,
        ValidationResult = validationResult,
        OptimizationResult = optimizationResult,
        Stages = stages,
        ProcessingTimeMs = processingTimeMs,
        Summary = summary
    };
    
    /// <summary>
    /// Başarısız sonuç oluşturur
    /// </summary>
    public static SqlPipelineResult Failure(
        string originalSql,
        string errorMessage,
        SqlValidationResult? validationResult = null,
        List<SqlPipelineStage>? stages = null,
        long processingTimeMs = 0) => new()
    {
        IsSuccess = false,
        OriginalSql = originalSql,
        FinalSql = originalSql,
        ValidationResult = validationResult,
        Stages = stages ?? [],
        ProcessingTimeMs = processingTimeMs,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Pipeline aşaması
/// </summary>
public record SqlPipelineStage
{
    /// <summary>
    /// Aşama adı
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Aşama başarılı mı?
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// İşlem süresi (ms)
    /// </summary>
    public long DurationMs { get; init; }
    
    /// <summary>
    /// Giriş SQL'i
    /// </summary>
    public string InputSql { get; init; } = string.Empty;
    
    /// <summary>
    /// Çıkış SQL'i
    /// </summary>
    public string OutputSql { get; init; } = string.Empty;
    
    /// <summary>
    /// Aşama mesajı
    /// </summary>
    public string? Message { get; init; }
}
