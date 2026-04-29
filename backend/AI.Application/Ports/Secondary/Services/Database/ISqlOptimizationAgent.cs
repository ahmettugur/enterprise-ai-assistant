namespace AI.Application.Ports.Secondary.Services.Database;

/// <summary>
/// SQL sorgularını performans açısından optimize eden agent interface'i.
/// Index kullanımı, query plan optimizasyonu gibi iyileştirmeler yapar.
/// </summary>
public interface ISqlOptimizationAgent
{
    /// <summary>
    /// SQL sorgusunu optimize eder.
    /// </summary>
    /// <param name="sql">Optimize edilecek SQL sorgusu</param>
    /// <param name="databaseType">Veritabanı tipi (oracle, sqlserver)</param>
    /// <param name="schemaInfo">Opsiyonel schema bilgisi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Optimizasyon sonucu</returns>
    Task<SqlOptimizationResult> OptimizeAsync(
        string sql,
        string databaseType,
        string? schemaInfo = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// SQL optimizasyon sonucu
/// </summary>
public record SqlOptimizationResult
{
    /// <summary>
    /// Optimizasyon yapıldı mı?
    /// </summary>
    public bool IsOptimized { get; init; }
    
    /// <summary>
    /// Orijinal SQL sorgusu
    /// </summary>
    public string OriginalSql { get; init; } = string.Empty;
    
    /// <summary>
    /// Optimize edilmiş SQL sorgusu
    /// </summary>
    public string OptimizedSql { get; init; } = string.Empty;
    
    /// <summary>
    /// Uygulanan optimizasyonlar
    /// </summary>
    public List<SqlOptimization> Optimizations { get; init; } = [];
    
    /// <summary>
    /// Tahmini performans iyileştirmesi (%)
    /// </summary>
    public int? EstimatedImprovementPercent { get; init; }
    
    /// <summary>
    /// Optimizasyon açıklaması
    /// </summary>
    public string? Explanation { get; init; }
    
    /// <summary>
    /// Optimizasyon yapılmadı sonucu oluşturur
    /// </summary>
    public static SqlOptimizationResult NoOptimization(string sql, string? explanation = null) => new()
    {
        IsOptimized = false,
        OriginalSql = sql,
        OptimizedSql = sql,
        Explanation = explanation ?? "SQL sorgusu zaten optimize durumda."
    };
    
    /// <summary>
    /// Optimize edilmiş sonuç oluşturur
    /// </summary>
    public static SqlOptimizationResult Optimized(
        string originalSql, 
        string optimizedSql, 
        List<SqlOptimization> optimizations,
        int? improvementPercent = null,
        string? explanation = null) => new()
    {
        IsOptimized = true,
        OriginalSql = originalSql,
        OptimizedSql = optimizedSql,
        Optimizations = optimizations,
        EstimatedImprovementPercent = improvementPercent,
        Explanation = explanation
    };
}

/// <summary>
/// Uygulanan optimizasyon detayı
/// </summary>
public record SqlOptimization
{
    /// <summary>
    /// Optimizasyon tipi
    /// </summary>
    public SqlOptimizationType Type { get; init; }
    
    /// <summary>
    /// Optimizasyon açıklaması
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Önceki durum
    /// </summary>
    public string? Before { get; init; }
    
    /// <summary>
    /// Sonraki durum
    /// </summary>
    public string? After { get; init; }
    
    /// <summary>
    /// Tahmini etki
    /// </summary>
    public string? Impact { get; init; }
}

/// <summary>
/// Optimizasyon tipleri
/// </summary>
public enum SqlOptimizationType
{
    /// <summary>
    /// Index kullanımı eklendi
    /// </summary>
    IndexHint,
    
    /// <summary>
    /// WHERE koşulu iyileştirildi
    /// </summary>
    WhereClauseOptimization,
    
    /// <summary>
    /// JOIN sırası değiştirildi
    /// </summary>
    JoinReordering,
    
    /// <summary>
    /// Subquery'den JOIN'e dönüştürüldü
    /// </summary>
    SubqueryToJoin,
    
    /// <summary>
    /// SELECT * yerine belirli kolonlar seçildi
    /// </summary>
    SelectColumnSpecification,
    
    /// <summary>
    /// DISTINCT kaldırıldı veya optimize edildi
    /// </summary>
    DistinctOptimization,
    
    /// <summary>
    /// ORDER BY optimize edildi
    /// </summary>
    OrderByOptimization,
    
    /// <summary>
    /// UNION yerine UNION ALL kullanıldı
    /// </summary>
    UnionToUnionAll,
    
    /// <summary>
    /// Pagination eklendi veya iyileştirildi
    /// </summary>
    Pagination,
    
    /// <summary>
    /// Parallel hint eklendi
    /// </summary>
    ParallelHint,
    
    /// <summary>
    /// Format/boşluk düzenleme
    /// </summary>
    Formatting,
    
    /// <summary>
    /// Diğer optimizasyonlar
    /// </summary>
    Other
}
