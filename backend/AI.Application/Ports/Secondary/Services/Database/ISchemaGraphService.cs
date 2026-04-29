using AI.Application.DTOs.Neo4j;
using AI.Application.DTOs.SchemaCatalog;

namespace AI.Application.Ports.Secondary.Services.Database;

/// <summary>
/// Neo4j Schema Catalog servisi için interface
/// Veritabanı şemasını Neo4j'de saklar ve dinamik SQL üretimi için şema bilgisi sağlar
/// </summary>
public interface ISchemaGraphService
{
    /// <summary>
    /// Kullanıcı sorgusuna göre ilgili tabloları bulur (Full-text search)
    /// </summary>
    /// <param name="userQuery">Kullanıcının sorusu</param>
    /// <param name="maxResults">Maksimum sonuç sayısı</param>
    /// <returns>İlgili tablolar (relevance score'a göre sıralı)</returns>
    Task<IEnumerable<TableInfo>> FindRelevantTablesAsync(string userQuery, int maxResults = 10);
    
    /// <summary>
    /// Anahtar kelimelerle tablo arar
    /// </summary>
    /// <param name="keywords">Anahtar kelimeler</param>
    /// <returns>Eşleşen tablolar</returns>
    Task<IEnumerable<TableInfo>> SearchTablesByKeywordsAsync(IEnumerable<string> keywords);
    
    /// <summary>
    /// Kolon alias'ına göre arar (Türkçe alias desteği)
    /// </summary>
    /// <param name="alias">Türkçe alias (örn: "Müşteri Numarası")</param>
    /// <returns>Eşleşen kolonlar</returns>
    Task<IEnumerable<ColumnInfo>> SearchColumnsByAliasAsync(string alias);
    
    /// <summary>
    /// İki tablo arasındaki en kısa JOIN path'i bulur
    /// </summary>
    /// <param name="table1">Birinci tablo (fullName)</param>
    /// <param name="table2">İkinci tablo (fullName)</param>
    /// <returns>JOIN path bilgisi veya null</returns>
    Task<JoinPath?> FindJoinPathAsync(string table1, string table2);
    
    /// <summary>
    /// Birden fazla tablo arasındaki JOIN path'leri bulur
    /// </summary>
    /// <param name="tableNames">Tablo adları listesi</param>
    /// <returns>JOIN path'leri</returns>
    Task<IEnumerable<JoinPath>> FindJoinPathsAsync(IEnumerable<string> tableNames);
    
    /// <summary>
    /// Belirtilen tablonun şema bilgisini getirir (kolonlar dahil)
    /// </summary>
    /// <param name="tableName">Tablo adı (fullName)</param>
    /// <returns>Tablo şeması veya null</returns>
    Task<TableSchema?> GetTableSchemaAsync(string tableName);
    
    /// <summary>
    /// Birden fazla tablonun şema bilgisini getirir
    /// </summary>
    /// <param name="tableNames">Tablo adları</param>
    /// <returns>Tablo şemaları</returns>
    Task<IEnumerable<TableSchema>> GetTableSchemasAsync(IEnumerable<string> tableNames);
    
    /// <summary>
    /// Dinamik prompt için şema bilgisi oluşturur (Markdown formatında)
    /// </summary>
    /// <param name="tableNames">Dahil edilecek tablo adları</param>
    /// <returns>Markdown formatında şema bilgisi</returns>
    Task<string> GenerateSchemaPromptAsync(IEnumerable<string> tableNames);
    
    /// <summary>
    /// Kullanıcı sorgusuna göre dinamik şema prompt'u oluşturur
    /// (İlgili tabloları bulur + JOIN path'leri ekler)
    /// </summary>
    /// <param name="userQuery">Kullanıcının sorusu</param>
    /// <returns>Dinamik şema prompt'u</returns>
    Task<string> GenerateDynamicSchemaPromptAsync(string userQuery);
    
    /// <summary>
    /// Belirtilen şemadaki tüm tabloları getirir
    /// </summary>
    /// <param name="schemaName">Şema adı (örn: Sales)</param>
    /// <returns>Tablolar</returns>
    Task<IEnumerable<TableInfo>> GetTablesBySchemaAsync(string schemaName);
    
    /// <summary>
    /// Tüm şemaları getirir
    /// </summary>
    /// <returns>Şema bilgileri</returns>
    Task<IEnumerable<SchemaInfo>> GetAllSchemasAsync();
    
    /// <summary>
    /// Neo4j bağlantısını test eder
    /// </summary>
    /// <returns>Bağlantı başarılı mı?</returns>
    Task<bool> TestConnectionAsync();
    
    /// <summary>
    /// Schema Catalog istatistiklerini getirir
    /// </summary>
    /// <returns>İstatistikler</returns>
    Task<SchemaCatalogStats> GetStatsAsync();
    
    #region Source-aware Methods (Çoklu Veritabanı Desteği)
    
    /// <summary>
    /// Belirli bir kaynağa ait tabloları sorgular
    /// </summary>
    /// <param name="userQuery">Kullanıcının sorusu</param>
    /// <param name="source">Kaynak adı (örn: AdventureWorks, Northwind)</param>
    /// <param name="maxResults">Maksimum sonuç sayısı</param>
    /// <returns>İlgili tablolar</returns>
    Task<IEnumerable<TableInfo>> FindRelevantTablesBySourceAsync(string userQuery, string source, int maxResults = 10);
    
    /// <summary>
    /// Belirli bir kaynağa ait tüm tabloları getirir
    /// </summary>
    /// <param name="source">Kaynak adı</param>
    /// <returns>Tablolar</returns>
    Task<IEnumerable<TableInfo>> GetTablesBySourceAsync(string source);
    
    /// <summary>
    /// Belirli bir kaynağa ait şemaları getirir
    /// </summary>
    /// <param name="source">Kaynak adı</param>
    /// <returns>Şemalar</returns>
    Task<IEnumerable<SchemaInfo>> GetSchemasBySourceAsync(string source);
    
    /// <summary>
    /// Belirli bir kaynağın istatistiklerini getirir
    /// </summary>
    /// <param name="source">Kaynak adı</param>
    /// <returns>İstatistikler</returns>
    Task<SchemaCatalogStats> GetStatsBySourceAsync(string source);
    
    /// <summary>
    /// Mevcut tüm kaynakları listeler
    /// </summary>
    /// <returns>Kaynak adları</returns>
    Task<IEnumerable<string>> GetAvailableSourcesAsync();
    
    /// <summary>
    /// Belirli bir kaynağa ait tüm verileri siler
    /// </summary>
    /// <param name="source">Kaynak adı</param>
    /// <returns>Başarılı mı?</returns>
    Task<bool> DeleteSourceDataAsync(string source);
    
    #endregion
}