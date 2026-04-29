namespace AI.Application.DTOs.AdvancedRag;

/// <summary>
/// Metadata alan bilgisi
/// </summary>
public class MetadataFieldInfo
{
    /// <summary>
    /// Alan adı (Qdrant payload key)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Alan açıklaması (LLM için)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Alan tipi
    /// </summary>
    public MetadataFieldType FieldType { get; set; } = MetadataFieldType.String;

    /// <summary>
    /// Olası değerler (enum-like alanlar için)
    /// </summary>
    public List<string>? PossibleValues { get; set; }
}