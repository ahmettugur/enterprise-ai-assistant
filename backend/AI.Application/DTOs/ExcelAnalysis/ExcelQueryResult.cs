namespace AI.Application.DTOs.ExcelAnalysis;

/// <summary>
/// SQL sorgu sonucu
/// </summary>
public class ExcelQueryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ExecutedSql { get; set; } = string.Empty;
    public List<Dictionary<string, object?>> Data { get; set; } = new();
    public string[] Columns { get; set; } = Array.Empty<string>();
    public int RowCount { get; set; }
    public long ExecutionTimeMs { get; set; }
}