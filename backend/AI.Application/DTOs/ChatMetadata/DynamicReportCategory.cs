namespace AI.Application.DTOs.ChatMetadata;

/// <summary>
/// Dinamik rapor kategorisi
/// </summary>
public class DynamicReportCategory
{
    public required string Id { get; set; }
    /// <summary>
    /// Hangi veritabanına ait (adventureworks)
    /// </summary>
    public required string DatabaseId { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    /// <summary>
    /// onClick değeri
    /// </summary>
    public required string OnClickValue { get; set; }
    /// <summary>
    /// Router'dan dönecek reportName değeri (adventureworks)
    /// </summary>
    public required string ReportName { get; set; }
}