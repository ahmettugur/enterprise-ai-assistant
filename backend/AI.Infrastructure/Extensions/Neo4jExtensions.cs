using AI.Application.Configuration;
using AI.Application.DTOs.DatabaseSchema;
using AI.Application.DTOs.Neo4j;
using AI.Application.Ports.Secondary.Services.AIChat;
using AI.Application.Ports.Secondary.Services.Database;
using AI.Infrastructure.Adapters.AI.Neo4j;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace AI.Infrastructure.Extensions;

/// <summary>
/// Neo4j Schema Catalog için DI extension metodları
/// </summary>
public static class Neo4jExtensions
{
    /// <summary>
    /// Neo4j Schema Catalog servislerini DI container'a ekler
    /// </summary>
    public static IServiceCollection AddNeo4jSchemaCatalog(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Settings
        services.Configure<Neo4jSettings>(
            configuration.GetSection(Neo4jSettings.SectionName));

        // Services
        services.AddSingleton<ISchemaParserService, SchemaParserService>();
        services.AddSingleton<ISchemaGraphService, SchemaGraphService>();
        services.AddSingleton<IDynamicPromptBuilder, DynamicPromptBuilder>();
        
        // Hibrit Yaklaşım - Veritabanından Otomatik Şema Çekme
        services.AddSingleton<IDatabaseSchemaReader, DatabaseSchemaReader>();

        // Health Check
        var settings = configuration.GetSection(Neo4jSettings.SectionName).Get<Neo4jSettings>();
        if (settings?.Enabled == true)
        {
            services.AddHealthChecks()
                .AddCheck<Neo4jHealthCheck>(
                    "neo4j",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "db", "neo4j", "graph" });
        }

        return services;
    }
    
    /// <summary>
    /// Neo4j Schema Catalog'u başlatır - şema yoksa oluşturur
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="forceRefresh">true ise mevcut şemayı silip yeniden oluşturur</param>
    public static async Task InitializeNeo4jSchemaCatalogAsync(
        this IServiceProvider serviceProvider, 
        bool forceRefresh = false)
    {
        var settings = serviceProvider.GetRequiredService<IOptions<Neo4jSettings>>().Value;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        if (!settings.Enabled)
        {
            return;
        }

        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Neo4jSchemaCatalog");
        
        var schemaGraphService = serviceProvider.GetRequiredService<ISchemaGraphService>();
        var parserService = serviceProvider.GetRequiredService<ISchemaParserService>();

        try
        {
            // 1. Neo4j bağlantısını test et
            var isConnected = await schemaGraphService.TestConnectionAsync();
            if (!isConnected)
            {
                logger.LogWarning("Neo4j bağlantısı kurulamadı. Schema Catalog başlatılamadı.");
                return;
            }

            // 2. Mevcut şema verilerini kontrol et
            var stats = await schemaGraphService.GetStatsAsync();
            
            if (stats.TotalTables > 0 && !forceRefresh)
            {
                logger.LogInformation(
                    "Neo4j Schema Catalog zaten mevcut. Şema: {Schemas}, Tablo: {Tables}, Kolon: {Columns}, FK: {FKs}",
                    stats.TotalSchemas,
                    stats.TotalTables,
                    stats.TotalColumns,
                    stats.TotalForeignKeys);
                return;
            }

            // 3. Force refresh ise önce mevcut verileri sil
            if (forceRefresh && stats.TotalTables > 0)
            {
                logger.LogInformation("Force refresh: Mevcut Neo4j Schema Catalog siliniyor...");
                await ClearAllSchemaDataAsync(settings, logger);
                logger.LogInformation("Mevcut veriler silindi.");
            }

            // 4. Şemayı kaynağa göre import et
            logger.LogInformation(
                "Neo4j Schema Catalog import ediliyor. Kaynak: {SchemaSource}",
                settings.SchemaSource);

            SchemaParseResult parseResult;

            if (settings.SchemaSource == SchemaSourceType.Database)
            {
                // Veritabanından şema çek
                parseResult = await ImportFromDatabaseAsync(
                    serviceProvider, settings, configuration, logger);
            }
            else
            {
                // Markdown dosyasından şema çek (varsayılan)
                parseResult = await ImportFromMarkdownAsync(
                    parserService, settings, logger);
            }
            
            if (parseResult == null || !parseResult.IsSuccess)
            {
                logger.LogError("Schema parse hatası: {Errors}", 
                    parseResult != null ? string.Join(", ", parseResult.Errors) : "Parse sonucu null");
                return;
            }

            logger.LogInformation(
                "Schema parse edildi. Tablo: {Tables}, Kolon: {Columns}, FK: {FKs}, ExplicitFK: {ExplicitFKs}",
                parseResult.TotalTableCount,
                parseResult.TotalColumnCount,
                parseResult.TotalForeignKeyCount,
                parseResult.ForeignKeyRelations.Count);

            // 5. Cypher script oluştur ve çalıştır
            var cypherScript = parserService.GenerateCypherImportScript(parseResult);
            
            await ExecuteCypherImportAsync(settings, cypherScript, logger);

            // 6. Sonucu doğrula
            var newStats = await schemaGraphService.GetStatsAsync();
            logger.LogInformation(
                "Neo4j Schema Catalog başarıyla oluşturuldu. Kaynak: {SchemaSource}, Şema: {Schemas}, Tablo: {Tables}, Kolon: {Columns}, FK: {FKs}",
                settings.SchemaSource,
                newStats.TotalSchemas,
                newStats.TotalTables,
                newStats.TotalColumns,
                newStats.TotalForeignKeys);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Neo4j Schema Catalog başlatma hatası");
        }
    }
    
    /// <summary>
    /// Markdown dosyasından şema import eder
    /// </summary>
    private static async Task<SchemaParseResult> ImportFromMarkdownAsync(
        ISchemaParserService parserService,
        Neo4jSettings settings,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        logger.LogInformation("Markdown dosyasından şema okunuyor: {FileName}", settings.MarkdownSchemaFile);
        
        var parseResult = await parserService.ParseFromResourceAsync(settings.MarkdownSchemaFile);
        return parseResult;
    }
    
    /// <summary>
    /// SQL Server veritabanından şema import eder
    /// </summary>
    private static async Task<SchemaParseResult> ImportFromDatabaseAsync(
        IServiceProvider serviceProvider,
        Neo4jSettings settings,
        IConfiguration configuration,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var databaseSchemaReader = serviceProvider.GetRequiredService<IDatabaseSchemaReader>();
        
        // Connection string al
        var connectionString = configuration.GetConnectionString(settings.DatabaseConnectionName);
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogError("Connection string bulunamadı: {ConnectionName}", settings.DatabaseConnectionName);
            return new SchemaParseResult
            {
                Errors = new List<string> { $"Connection string bulunamadı: {settings.DatabaseConnectionName}" }
            };
        }
        
        logger.LogInformation(
            "Veritabanından şema okunuyor. Connection: {ConnectionName}", 
            settings.DatabaseConnectionName);
        
        // Bağlantıyı test et
        if (!await databaseSchemaReader.TestConnectionAsync(connectionString))
        {
            logger.LogError("Veritabanı bağlantısı başarısız: {ConnectionName}", settings.DatabaseConnectionName);
            return new SchemaParseResult
            {
                Errors = new List<string> { $"Veritabanı bağlantısı başarısız: {settings.DatabaseConnectionName}" }
            };
        }
        
        // Alias konfigürasyonu yükle (varsa)
        AliasConfiguration? aliasConfig = null;
        if (!string.IsNullOrEmpty(settings.AliasConfigFile))
        {
            try
            {
                var aliasJson = await LoadResourceFileAsync(settings.AliasConfigFile);
                if (!string.IsNullOrEmpty(aliasJson))
                {
                    aliasConfig = System.Text.Json.JsonSerializer.Deserialize<AliasConfiguration>(aliasJson);
                    logger.LogInformation("Alias konfigürasyonu yüklendi: {FileName}", settings.AliasConfigFile);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Alias konfigürasyonu yüklenemedi: {FileName}", settings.AliasConfigFile);
            }
        }
        
        // Şemayı veritabanından oku
        var parseResult = await databaseSchemaReader.ReadSchemaFromDatabaseAsync(
            connectionString, settings.DefaultSource, aliasConfig);
        
        return parseResult;
    }
    
    /// <summary>
    /// Resources klasöründen dosya içeriğini okur
    /// </summary>
    private static async Task<string?> LoadResourceFileAsync(string fileName)
    {
        var assembly = typeof(Neo4jExtensions).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        
        if (resourceName == null)
        {
            // Dosya sisteminden okumayı dene
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(basePath, "Common", "Resources", fileName);
            
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            
            return null;
        }
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;
        
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Neo4j'deki tüm şema verilerini siler
    /// </summary>
    private static async Task ClearAllSchemaDataAsync(
        Neo4jSettings settings, 
        Microsoft.Extensions.Logging.ILogger logger)
    {
        using var driver = GraphDatabase.Driver(
            settings.Uri,
            AuthTokens.Basic(settings.Username, settings.Password));

        await using var session = driver.AsyncSession(o => o.WithDatabase(settings.Database));

        var clearStatements = new[]
        {
            // İlişkileri sil
            "MATCH ()-[r:REFERENCES]->() DELETE r",
            "MATCH ()-[r:JOINS_WITH]->() DELETE r",
            "MATCH ()-[r:HAS_COLUMN]->() DELETE r",
            "MATCH ()-[r:CONTAINS]->() DELETE r",
            
            // Node'ları sil
            "MATCH (c:Column) DELETE c",
            "MATCH (t:Table) DELETE t",
            "MATCH (s:Schema) DELETE s",
            
            // Index'leri sil (hata verirse atla)
            "DROP INDEX table_search IF EXISTS",
            "DROP INDEX column_search IF EXISTS"
        };

        foreach (var statement in clearStatements)
        {
            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(statement);
                });
                logger.LogDebug("Executed: {Statement}", statement);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Clear statement hatası (devam ediliyor): {Error}", ex.Message);
            }
        }
    }

    private static async Task ExecuteCypherImportAsync(
        Neo4jSettings settings, 
        string cypherScript, 
        Microsoft.Extensions.Logging.ILogger logger)
    {
        using var driver = GraphDatabase.Driver(
            settings.Uri,
            AuthTokens.Basic(settings.Username, settings.Password));

        await using var session = driver.AsyncSession(o => o.WithDatabase(settings.Database));

        var statements = SplitCypherStatements(cypherScript);
        var executedCount = 0;
        var errorCount = 0;

        foreach (var statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement))
                continue;

            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(statement);
                });
                executedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                var shortStatement = statement.Length > 100 
                    ? statement.Substring(0, 100) + "..." 
                    : statement;
                logger.LogWarning("Cypher statement hatası: {Error} - Statement: {Statement}", 
                    ex.Message, shortStatement);
            }
        }

        logger.LogInformation(
            "Cypher import tamamlandı. Başarılı: {Success}, Hata: {Errors}, Toplam: {Total}",
            executedCount, errorCount, statements.Count);
    }

    private static List<string> SplitCypherStatements(string script)
    {
        var statements = new List<string>();
        var currentStatement = new System.Text.StringBuilder();
        var lines = script.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Yorum satırlarını atla
            if (trimmedLine.StartsWith("//"))
                continue;

            // Boş satırları atla
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                if (currentStatement.Length > 0)
                {
                    var stmt = currentStatement.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stmt))
                    {
                        statements.Add(stmt);
                    }
                    currentStatement.Clear();
                }
                continue;
            }

            currentStatement.AppendLine(trimmedLine);

            // Satır ; ile bitiyorsa statement tamamlandı
            if (trimmedLine.EndsWith(";"))
            {
                var stmt = currentStatement.ToString().Trim().TrimEnd(';');
                if (!string.IsNullOrWhiteSpace(stmt))
                {
                    statements.Add(stmt);
                }
                currentStatement.Clear();
            }
        }

        // Son kalan statement
        if (currentStatement.Length > 0)
        {
            var stmt = currentStatement.ToString().Trim().TrimEnd(';');
            if (!string.IsNullOrWhiteSpace(stmt))
            {
                statements.Add(stmt);
            }
        }

        return statements;
    }
}

/// <summary>
/// Neo4j bağlantı sağlık kontrolü
/// </summary>
public class Neo4jHealthCheck : IHealthCheck
{
    private readonly ISchemaGraphService _schemaGraphService;

    public Neo4jHealthCheck(ISchemaGraphService schemaGraphService)
    {
        _schemaGraphService = schemaGraphService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _schemaGraphService.TestConnectionAsync();
            
            if (isHealthy)
            {
                var stats = await _schemaGraphService.GetStatsAsync();
                return HealthCheckResult.Healthy(
                    "Neo4j Schema Catalog is healthy",
                    new Dictionary<string, object>
                    {
                        ["schemas"] = stats.TotalSchemas,
                        ["tables"] = stats.TotalTables,
                        ["views"] = stats.TotalViews,
                        ["columns"] = stats.TotalColumns,
                        ["foreignKeys"] = stats.TotalForeignKeys
                    });
            }

            return HealthCheckResult.Degraded("Neo4j connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Neo4j health check failed",
                ex,
                new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}
