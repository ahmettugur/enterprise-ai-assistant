namespace AI.Application.DTOs.DatabaseSchema;

/// <summary>
/// Alias konfigürasyonu - Türkçe tablo ve kolon isimleri için
/// </summary>
public class AliasConfiguration
{
    /// <summary>
    /// Kaynak veritabanı adı
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Tablo alias'ları (key: FullName, value: Alias bilgileri)
    /// </summary>
    public Dictionary<string, TableAlias> Tables { get; set; } = new();

    /// <summary>
    /// Kolon alias'ları (key: TableName.ColumnName, value: Alias bilgileri)
    /// </summary>
    public Dictionary<string, ColumnAlias> Columns { get; set; } = new();
}