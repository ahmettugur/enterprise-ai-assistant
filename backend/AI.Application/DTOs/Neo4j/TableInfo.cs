namespace AI.Application.DTOs.Neo4j;

/// <summary>
/// Neo4j'den dönen tablo bilgisi
/// </summary>
public class TableInfo
{
    /// <summary>
    /// Tablo adı (örn: Customer)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tam tablo adı (örn: Sales.Customer)
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Şema adı (örn: Sales)
    /// </summary>
    public string Schema { get; set; } = string.Empty;
    
    /// <summary>
    /// Tablo açıklaması (Türkçe)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Tablo tipi (Table veya View)
    /// </summary>
    public string Type { get; set; } = "Table";
    
    /// <summary>
    /// Semantic search relevance skoru (0-1 arası)
    /// </summary>
    public double RelevanceScore { get; set; }
}
