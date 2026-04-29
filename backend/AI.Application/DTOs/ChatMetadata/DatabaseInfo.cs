namespace AI.Application.DTOs.ChatMetadata;

/// <summary>
/// Veritabanı bilgisi
/// </summary>
public class DatabaseInfo
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
}