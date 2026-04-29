namespace AI.Application.DTOs.SchemaCatalog;

/// <summary>
/// Schema Catalog istatistikleri
/// </summary>
public class SchemaCatalogStats
{
    public int TotalSchemas { get; set; }
    public int TotalTables { get; set; }
    public int TotalViews { get; set; }
    public int TotalColumns { get; set; }
    public int TotalForeignKeys { get; set; }
    public DateTime? LastUpdated { get; set; }
}