namespace AI.Application.Configuration;

/// <summary>
/// Şema kaynağı türü
/// </summary>
public enum SchemaSourceType
{
    /// <summary>
    /// Markdown dosyasından şema yükle (adventurerworks_schema.md)
    /// </summary>
    Markdown,
    
    /// <summary>
    /// SQL Server veritabanından otomatik şema çek
    /// </summary>
    Database
}

/// <summary>
/// Neo4j Graph Database bağlantı ayarları
/// </summary>
public class Neo4jSettings
{
    public const string SectionName = "Neo4j";
    
    /// <summary>
    /// Neo4j Bolt protokol URI (örn: bolt://localhost:7687)
    /// </summary>
    public string Uri { get; set; } = "bolt://localhost:7687";
    
    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = "neo4j";
    
    /// <summary>
    /// Şifre
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Veritabanı adı (varsayılan: neo4j)
    /// </summary>
    public string Database { get; set; } = "neo4j";
    
    /// <summary>
    /// Maksimum bağlantı havuzu boyutu
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;
    
    /// <summary>
    /// Bağlantı edinme timeout süresi
    /// </summary>
    public TimeSpan ConnectionAcquisitionTimeout { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Schema Catalog özelliği aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Şema kaynağı türü (Markdown veya Database)
    /// </summary>
    public SchemaSourceType SchemaSource { get; set; } = SchemaSourceType.Markdown;
    
    /// <summary>
    /// Markdown şema dosyası adı (SchemaSource=Markdown için)
    /// Resources/Prompts klasöründeki dosya adı
    /// </summary>
    public string MarkdownSchemaFile { get; set; } = "adventurerworks_schema.md";
    
    /// <summary>
    /// Veritabanı connection string adı (SchemaSource=Database için)
    /// appsettings.json'daki ConnectionStrings bölümündeki key
    /// </summary>
    public string DatabaseConnectionName { get; set; } = "AdventureWorks2022";
    
    /// <summary>
    /// Alias konfigürasyon dosyası (SchemaSource=Database için, opsiyonel)
    /// Resources klasöründeki JSON dosya adı
    /// </summary>
    public string? AliasConfigFile { get; set; } = "alias_config_adventureworks.json";
    
    /// <summary>
    /// Maksimum ilgili tablo sayısı (dinamik prompt için)
    /// </summary>
    public int MaxRelevantTables { get; set; } = 6;
    
    /// <summary>
    /// Full-text search minimum relevance skoru
    /// </summary>
    public double MinRelevanceScore { get; set; } = 0.3;
    
    /// <summary>
    /// Maksimum JOIN hop sayısı
    /// </summary>
    public int MaxJoinHops { get; set; } = 4;
    
    /// <summary>
    /// Varsayılan kaynak adı (tek veritabanı senaryosu için)
    /// </summary>
    public string DefaultSource { get; set; } = "AdventureWorks";
    
    /// <summary>
    /// Çoklu veritabanı kaynakları konfigürasyonu
    /// </summary>
    public List<DatabaseSourceConfig> DatabaseSources { get; set; } = new();
}

/// <summary>
/// Veritabanı kaynak konfigürasyonu (Hibrit yaklaşım için)
/// </summary>
public class DatabaseSourceConfig
{
    /// <summary>
    /// Kaynak adı (Neo4j'de source property olarak kullanılır)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// SQL Server connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Alias konfigürasyon dosyası yolu (opsiyonel)
    /// </summary>
    public string? AliasConfigPath { get; set; }
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Otomatik senkronizasyon aktif mi?
    /// </summary>
    public bool AutoSync { get; set; } = false;
    
    /// <summary>
    /// Senkronizasyon cron expression (AutoSync=true ise)
    /// </summary>
    public string? SyncCronExpression { get; set; }
}
