namespace AI.Application.DTOs.ChatMetadata;

/// <summary>
/// Hazır rapor bilgisi
/// </summary>
public class ReadyReportInfo
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    /// <summary>
    /// Rapor URL'i
    /// </summary>
    public required string Url { get; set; }
}