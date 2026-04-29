namespace AI.Application.DTOs.ExcelAnalysis;

/// <summary>
/// Excel/CSV şema bilgisi
/// </summary>
public class ExcelSchemaResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> SampleRows { get; set; } = new();
}