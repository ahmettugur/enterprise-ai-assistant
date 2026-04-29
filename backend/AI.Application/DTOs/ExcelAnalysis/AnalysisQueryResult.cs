namespace AI.Application.DTOs.ExcelAnalysis;

/// <summary>
/// Tek bir analiz sorgusunun çalıştırma sonucu (başlık + DuckDB sonucu)
/// </summary>
public class AnalysisQueryResult
{
    /// <summary>
    /// Sorgu başlığı (ör: "Genel Bakış")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Sorgu açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Çalıştırılan SQL
    /// </summary>
    public string ExecutedSql { get; set; } = string.Empty;

    /// <summary>
    /// DuckDB sorgu sonucu
    /// </summary>
    public ExcelQueryResult QueryResult { get; set; } = new();

    /// <summary>
    /// Sorgu başarılı mı?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Hata mesajı (başarısız ise)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
