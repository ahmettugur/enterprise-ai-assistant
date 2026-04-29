using AI.Application.DTOs.DatabaseSchema;
using AI.Application.DTOs.Neo4j;

namespace AI.Application.Ports.Secondary.Services.Database;

/// <summary>
/// Veritabanından şema bilgisini otomatik olarak çeken servis interface'i
/// </summary>
public interface IDatabaseSchemaReader
{
    /// <summary>
    /// SQL Server veritabanından şema bilgisini otomatik çeker
    /// </summary>
    /// <param name="connectionString">Veritabanı connection string</param>
    /// <param name="sourceName">Kaynak adı (Neo4j'de source property olarak kullanılacak)</param>
    /// <param name="aliasConfig">Opsiyonel alias konfigürasyonu (Türkçe isimler için)</param>
    /// <returns>Parse edilmiş şema bilgisi</returns>
    Task<SchemaParseResult> ReadSchemaFromDatabaseAsync(
        string connectionString, 
        string sourceName,
        AliasConfiguration? aliasConfig = null);

    /// <summary>
    /// Belirtilen veritabanındaki tüm tabloları listeler
    /// </summary>
    Task<IEnumerable<TableInfo>> GetTablesAsync(string connectionString, string sourceName);

    /// <summary>
    /// Belirtilen tablonun kolon bilgilerini getirir
    /// </summary>
    Task<IEnumerable<ColumnInfo>> GetColumnsAsync(string connectionString, string tableName);

    /// <summary>
    /// Belirtilen veritabanındaki tüm foreign key ilişkilerini getirir
    /// </summary>
    Task<IEnumerable<ForeignKeyRelation>> GetForeignKeysAsync(string connectionString);

    /// <summary>
    /// Veritabanı bağlantısını test eder
    /// </summary>
    Task<bool> TestConnectionAsync(string connectionString);
}