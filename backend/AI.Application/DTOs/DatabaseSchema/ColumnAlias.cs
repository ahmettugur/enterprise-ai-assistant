namespace AI.Application.DTOs.DatabaseSchema;

/// <summary>
/// Kolon alias bilgisi
/// </summary>
public class ColumnAlias
{
    /// <summary>
    /// Türkçe kolon adı
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Kolon açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;
}