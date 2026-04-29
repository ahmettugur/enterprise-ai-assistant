namespace AI.Application.DTOs.ChatMetadata;

/// <summary>
/// Rapor türü bilgisi
/// </summary>
public class ReportTypeInfo
{
    public required string Id { get; set; }
    public required string DatabaseId { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    /// <summary>
    /// onClick değeri (LLM'in anlayacağı değer)
    /// </summary>
    public required string OnClickValue { get; set; }
}