namespace AI.Application.DTOs.ExcelAnalysis;

/// <summary>
/// Tam analiz sonucu (şema + sorgu + açıklama)
/// </summary>
public class ExcelAnalysisResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ExcelSchemaResult? Schema { get; set; }
    public string GeneratedSql { get; set; } = string.Empty;
    public ExcelQueryResult? QueryResult { get; set; }
    public string? Explanation { get; set; }
    public string? HtmlTable { get; set; }
}