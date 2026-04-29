namespace AI.Application.DTOs.Neo4j;

/// <summary>
/// Neo4j'den dönen kolon bilgisi
/// </summary>
public class ColumnInfo
{
    /// <summary>
    /// Kolon adı (örn: CustomerID)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Ait olduğu tablo adı (örn: Sales.Customer)
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// Veri tipi (örn: int, nvarchar(50), money)
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Türkçe alias (örn: Müşteri Numarası)
    /// </summary>
    public string Alias { get; set; } = string.Empty;
    
    /// <summary>
    /// Kolon açıklaması (Türkçe)
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary Key mi?
    /// </summary>
    public bool IsPrimaryKey { get; set; }
    
    /// <summary>
    /// Foreign Key mi?
    /// </summary>
    public bool IsForeignKey { get; set; }
    
    /// <summary>
    /// FK ise referans verdiği tablo (örn: Person.Person)
    /// </summary>
    public string? FkTable { get; set; }
    
    /// <summary>
    /// FK ise referans verdiği kolon (örn: BusinessEntityID)
    /// </summary>
    public string? FkColumn { get; set; }
}
