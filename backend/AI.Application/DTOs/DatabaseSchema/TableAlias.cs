namespace AI.Application.DTOs.DatabaseSchema;

/// <summary>
/// Tablo alias bilgisi
/// </summary>
public class TableAlias
{
    /// <summary>
    /// Türkçe tablo adı
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Tablo açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;
}