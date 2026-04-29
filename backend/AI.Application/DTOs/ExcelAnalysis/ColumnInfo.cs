namespace AI.Application.DTOs.ExcelAnalysis;

/// <summary>
/// Sütun bilgisi
/// </summary>
public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public List<string> SampleValues { get; set; } = new();
}