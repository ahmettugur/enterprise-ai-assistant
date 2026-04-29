namespace AI.Application.Ports.Secondary.Services.Database;

/// <summary>
/// SQL sorgularının doğruluğunu ve güvenliğini kontrol eden agent interface'i.
/// Schema bilgisine göre tablo ve kolon isimlerini doğrular.
/// </summary>
public interface ISqlValidationAgent
{
    /// <summary>
    /// SQL sorgusunu doğrular.
    /// </summary>
    /// <param name="sql">Doğrulanacak SQL sorgusu</param>
    /// <param name="databaseType">Veritabanı tipi (oracle, sqlserver)</param>
    /// <param name="schemaInfo">Opsiyonel schema bilgisi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Doğrulama sonucu</returns>
    Task<SqlValidationResult> ValidateAsync(
        string sql, 
        string databaseType,
        string? schemaInfo = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// SQL doğrulama sonucu
/// </summary>
public record SqlValidationResult
{
    /// <summary>
    /// Doğrulama başarılı mı?
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Orijinal SQL sorgusu
    /// </summary>
    public string OriginalSql { get; init; } = string.Empty;
    
    /// <summary>
    /// Düzeltilmiş SQL sorgusu (eğer düzeltme yapıldıysa)
    /// </summary>
    public string? CorrectedSql { get; init; }
    
    /// <summary>
    /// Bulunan hatalar
    /// </summary>
    public List<SqlValidationError> Errors { get; init; } = [];
    
    /// <summary>
    /// Uyarılar (performans, best practices, vb.)
    /// </summary>
    public List<SqlValidationWarning> Warnings { get; init; } = [];
    
    /// <summary>
    /// Güvenlik sorunları
    /// </summary>
    public List<SqlSecurityIssue> SecurityIssues { get; init; } = [];
    
    /// <summary>
    /// Doğrulama açıklaması
    /// </summary>
    public string? Explanation { get; init; }
    
    /// <summary>
    /// Başarılı sonuç oluşturur
    /// </summary>
    public static SqlValidationResult Valid(string sql, string? explanation = null) => new()
    {
        IsValid = true,
        OriginalSql = sql,
        CorrectedSql = null,
        Explanation = explanation
    };
    
    /// <summary>
    /// Düzeltilmiş başarılı sonuç oluşturur
    /// </summary>
    public static SqlValidationResult ValidWithCorrection(string originalSql, string correctedSql, string explanation) => new()
    {
        IsValid = true,
        OriginalSql = originalSql,
        CorrectedSql = correctedSql,
        Explanation = explanation
    };
    
    /// <summary>
    /// Hatalı sonuç oluşturur
    /// </summary>
    public static SqlValidationResult Invalid(string sql, List<SqlValidationError> errors, string? explanation = null) => new()
    {
        IsValid = false,
        OriginalSql = sql,
        Errors = errors,
        Explanation = explanation
    };
}

/// <summary>
/// SQL doğrulama hatası
/// </summary>
public record SqlValidationError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public SqlErrorSeverity Severity { get; init; } = SqlErrorSeverity.Error;
    public int? LineNumber { get; init; }
    public int? ColumnNumber { get; init; }
    public string? Suggestion { get; init; }
}

/// <summary>
/// SQL doğrulama uyarısı
/// </summary>
public record SqlValidationWarning
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Suggestion { get; init; }
}

/// <summary>
/// SQL güvenlik sorunu
/// </summary>
public record SqlSecurityIssue
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public SqlSecuritySeverity Severity { get; init; } = SqlSecuritySeverity.Warning;
    public string? Pattern { get; init; }
}

/// <summary>
/// Hata ciddiyeti
/// </summary>
public enum SqlErrorSeverity
{
    Warning,
    Error,
    Critical
}

/// <summary>
/// Güvenlik sorunu ciddiyeti
/// </summary>
public enum SqlSecuritySeverity
{
    Info,
    Warning,
    Critical
}
