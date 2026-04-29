namespace AI.Application.DTOs.Neo4j;

/// <summary>
/// Markdown parse işleminin sonucu
/// </summary>
public class SchemaParseResult
{
    /// <summary>
    /// Parse edilen şemalar
    /// </summary>
    public List<SchemaInfo> Schemas { get; set; } = new();
    
    /// <summary>
    /// Parse edilen tablolar
    /// </summary>
    public List<TableSchema> Tables { get; set; } = new();
    
    /// <summary>
    /// Explicit olarak tanımlanan FK ilişkileri (markdown'daki FK bölümünden)
    /// </summary>
    public List<ForeignKeyRelation> ForeignKeyRelations { get; set; } = new();
    
    /// <summary>
    /// Toplam tablo sayısı
    /// </summary>
    public int TotalTableCount => Tables.Count;
    
    /// <summary>
    /// Toplam kolon sayısı
    /// </summary>
    public int TotalColumnCount => Tables.Sum(t => t.Columns.Count);
    
    /// <summary>
    /// Toplam FK sayısı (kolon tanımlarından)
    /// </summary>
    public int TotalForeignKeyCount => Tables
        .SelectMany(t => t.Columns)
        .Count(c => c.IsForeignKey);
    
    /// <summary>
    /// Explicit FK ilişki sayısı
    /// </summary>
    public int TotalExplicitForeignKeyCount => ForeignKeyRelations.Count;
    
    /// <summary>
    /// Parse hataları
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Parse başarılı mı?
    /// </summary>
    public bool IsSuccess => Errors.Count == 0;
}

/// <summary>
/// Foreign Key ilişkisi (Markdown'daki FK tablosundan parse edilir)
/// </summary>
public class ForeignKeyRelation
{
    /// <summary>
    /// FK'nın bulunduğu şema (örn: Sales)
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;
    
    /// <summary>
    /// FK'nın bulunduğu tablo (örn: SalesOrderHeader)
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;
    
    /// <summary>
    /// FK kolon adı (örn: CustomerID)
    /// </summary>
    public string SourceColumn { get; set; } = string.Empty;
    
    /// <summary>
    /// Referans verilen şema (örn: Sales)
    /// </summary>
    public string TargetSchema { get; set; } = string.Empty;
    
    /// <summary>
    /// Referans verilen tablo (örn: Customer)
    /// </summary>
    public string TargetTable { get; set; } = string.Empty;
    
    /// <summary>
    /// Referans verilen kolon (örn: CustomerID)
    /// </summary>
    public string TargetColumn { get; set; } = string.Empty;
    
    /// <summary>
    /// FK constraint adı (opsiyonel)
    /// </summary>
    public string FkName { get; set; } = string.Empty;
    
    /// <summary>
    /// Kaynak tablo tam adı (Schema.Table)
    /// </summary>
    public string SourceFullName => $"{SourceSchema}.{SourceTable}";
    
    /// <summary>
    /// Hedef tablo tam adı (Schema.Table)
    /// </summary>
    public string TargetFullName => $"{TargetSchema}.{TargetTable}";
}

/// <summary>
/// Şema bilgisi
/// </summary>
public class SchemaInfo
{
    /// <summary>
    /// Şema adı (örn: Sales)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Şema açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Şemadaki tablo sayısı
    /// </summary>
    public int TableCount { get; set; }
}
