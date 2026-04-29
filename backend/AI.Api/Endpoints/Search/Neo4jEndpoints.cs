using AI.Application.Configuration;
using AI.Application.DTOs.Neo4j;
using AI.Application.DTOs.SchemaCatalog;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using AliasConfiguration = AI.Application.DTOs.DatabaseSchema.AliasConfiguration;

namespace AI.Api.Endpoints.Search;

/// <summary>
/// Neo4j Schema Catalog API Endpoints
/// </summary>
public static class Neo4JEndpoints
{
    public static IEndpointRouteBuilder MapNeo4JEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/neo4j")
            .WithTags("Neo4j Schema Catalog");

        // Health & Stats
        group.MapGet("/health", GetHealth)
            .WithName("GetNeo4jHealth")
            .WithSummary("Neo4j bağlantı durumunu kontrol eder");

        group.MapGet("/stats", GetStats)
            .WithName("GetNeo4jStats")
            .WithSummary("Schema Catalog istatistiklerini getirir");

        // Schema Operations
        group.MapGet("/schemas", GetAllSchemas)
            .WithName("GetAllSchemas")
            .WithSummary("Tüm şemaları listeler");

        group.MapGet("/schemas/{schemaName}/tables", GetTablesBySchema)
            .WithName("GetTablesBySchema")
            .WithSummary("Belirtilen şemadaki tabloları listeler");

        group.MapGet("/tables/{tableName}", GetTableSchema)
            .WithName("GetTableSchema")
            .WithSummary("Tablo şema bilgisini getirir");

        // Search Operations
        group.MapGet("/search/tables", SearchTables)
            .WithName("SearchTables")
            .WithSummary("Kullanıcı sorgusuna göre ilgili tabloları arar");

        group.MapGet("/search/columns", SearchColumns)
            .WithName("SearchColumns")
            .WithSummary("Alias ile kolon arar");

        // JOIN Path
        group.MapGet("/join-path", FindJoinPath)
            .WithName("FindJoinPath")
            .WithSummary("İki tablo arasındaki JOIN path'i bulur");

        // Dynamic Prompt Generation
        group.MapGet("/dynamic-prompt", GenerateDynamicPrompt)
            .WithName("GenerateDynamicPrompt")
            .WithSummary("Kullanıcı sorgusuna göre dinamik şema prompt'u oluşturur");

        // Import Operations
        group.MapPost("/import/parse", ParseSchema)
            .WithName("ParseSchema")
            .WithSummary("adventurerworks_schema.md dosyasını parse eder");

        group.MapPost("/import/execute", ExecuteImport)
            .WithName("ExecuteImport")
            .WithSummary("Parse edilen şemayı Neo4j'ye import eder");

        group.MapGet("/import/cypher-script", GetCypherScript)
            .WithName("GetCypherScript")
            .WithSummary("Cypher import script'ini döndürür");

        group.MapPost("/import/refresh", RefreshSchema)
            .WithName("RefreshSchema")
            .WithSummary("Mevcut şemayı silip yeniden import eder (Force Refresh)");

        group.MapDelete("/import/clear", ClearAllData)
            .WithName("ClearAllData")
            .WithSummary("Neo4j'deki tüm şema verilerini siler");

        // Hibrit Yaklaşım - Veritabanından Otomatik Şema Çekme
        group.MapPost("/import/from-database", ImportFromDatabase)
            .WithName("ImportFromDatabase")
            .WithSummary("SQL Server veritabanından şema çeker ve Neo4j'ye import eder");

        // Çoklu Kaynak (Multi-Source) Operations
        group.MapGet("/sources", GetAvailableSources)
            .WithName("GetAvailableSources")
            .WithSummary("Mevcut tüm veri kaynaklarını listeler");

        group.MapGet("/sources/{source}/tables", GetTablesBySource)
            .WithName("GetTablesBySource")
            .WithSummary("Belirli bir kaynağa ait tabloları listeler");

        group.MapGet("/sources/{source}/schemas", GetSchemasBySource)
            .WithName("GetSchemasBySource")
            .WithSummary("Belirli bir kaynağa ait şemaları listeler");

        group.MapGet("/sources/{source}/stats", GetStatsBySource)
            .WithName("GetStatsBySource")
            .WithSummary("Belirli bir kaynağın istatistiklerini getirir");

        group.MapDelete("/sources/{source}", DeleteSource)
            .WithName("DeleteSource")
            .WithSummary("Belirli bir kaynağa ait tüm verileri siler");

        group.MapGet("/search/tables-by-source", SearchTablesBySource)
            .WithName("SearchTablesBySource")
            .WithSummary("Belirli bir kaynakta tablo arar");

        return app;
    }

    #region Health & Stats

    private static async Task<Results<Ok<object>, StatusCodeHttpResult>> GetHealth(
        ISchemaGraphService schemaGraphService)
    {
        var isHealthy = await schemaGraphService.TestConnectionAsync();
        
        if (isHealthy)
        {
            return TypedResults.Ok<object>(new { status = "healthy", message = "Neo4j bağlantısı başarılı" });
        }

        return TypedResults.StatusCode(503);
    }

    private static async Task<Ok<SchemaCatalogStats>> GetStats(
        ISchemaGraphService schemaGraphService)
    {
        var stats = await schemaGraphService.GetStatsAsync();
        return TypedResults.Ok(stats);
    }

    #endregion

    #region Schema Operations

    private static async Task<Ok<IEnumerable<SchemaInfo>>> GetAllSchemas(
        ISchemaGraphService schemaGraphService)
    {
        var schemas = await schemaGraphService.GetAllSchemasAsync();
        return TypedResults.Ok(schemas);
    }

    private static async Task<Results<Ok<IEnumerable<TableInfo>>, NotFound>> GetTablesBySchema(
        string schemaName,
        ISchemaGraphService schemaGraphService)
    {
        var tables = await schemaGraphService.GetTablesBySchemaAsync(schemaName);
        var tableList = tables.ToList();

        if (!tableList.Any())
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok<IEnumerable<TableInfo>>(tableList);
    }

    private static async Task<Results<Ok<TableSchema>, NotFound>> GetTableSchema(
        string tableName,
        ISchemaGraphService schemaGraphService)
    {
        var schema = await schemaGraphService.GetTableSchemaAsync(tableName);

        if (schema == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(schema);
    }

    #endregion

    #region Search Operations

    private static async Task<Ok<IEnumerable<TableInfo>>> SearchTables(
        string query,
        int maxResults,
        ISchemaGraphService schemaGraphService)
    {
        var tables = await schemaGraphService.FindRelevantTablesAsync(query, maxResults > 0 ? maxResults : 10);
        return TypedResults.Ok(tables);
    }

    private static async Task<Ok<IEnumerable<ColumnInfo>>> SearchColumns(
        string alias,
        ISchemaGraphService schemaGraphService)
    {
        var columns = await schemaGraphService.SearchColumnsByAliasAsync(alias);
        return TypedResults.Ok(columns);
    }

    #endregion

    #region JOIN Path

    private static async Task<Results<Ok<JoinPath>, NotFound>> FindJoinPath(
        string table1,
        string table2,
        ISchemaGraphService schemaGraphService)
    {
        var path = await schemaGraphService.FindJoinPathAsync(table1, table2);

        if (path == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(path);
    }

    #endregion

    #region Dynamic Prompt

    private static async Task<Ok<object>> GenerateDynamicPrompt(
        string query,
        ISchemaGraphService schemaGraphService)
    {
        var prompt = await schemaGraphService.GenerateDynamicSchemaPromptAsync(query);
        return TypedResults.Ok<object>(new { query, prompt, promptLength = prompt.Length });
    }

    #endregion

    #region Import Operations

    private static async Task<Ok<SchemaParseResult>> ParseSchema(
        ISchemaParserService parserService)
    {
        var result = await parserService.ParseFromResourceAsync("adventurerworks_schema.md");
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<object>, BadRequest<object>>> ExecuteImport(
        ISchemaParserService parserService,
        IOptions<Neo4jSettings> settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Neo4jEndpoints");
        
        try
        {
            // 1. Schema dosyasını parse et
            var parseResult = await parserService.ParseFromResourceAsync("adventurerworks_schema.md");

            if (!parseResult.IsSuccess)
            {
                return TypedResults.BadRequest<object>(new
                {
                    success = false,
                    message = "Schema parse hatası",
                    errors = parseResult.Errors
                });
            }

            // 2. Cypher script oluştur
            var cypherScript = parserService.GenerateCypherImportScript(parseResult);

            // 3. Neo4j'ye bağlan ve çalıştır
            var neo4JSettings = settings.Value;
            using var driver = GraphDatabase.Driver(
                neo4JSettings.Uri,
                AuthTokens.Basic(neo4JSettings.Username, neo4JSettings.Password));

            await using var session = driver.AsyncSession(o => o.WithDatabase(neo4JSettings.Database));

            // Script'i statement'lara böl ve çalıştır
            var statements = SplitCypherStatements(cypherScript);
            var executedCount = 0;
            var errors = new List<string>();

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
                    var shortStatement = statement.Length > 100 
                        ? statement.Substring(0, 100) + "..." 
                        : statement;
                    errors.Add($"Statement hatası: {shortStatement} - {ex.Message}");
                    logger.LogWarning(ex, "Cypher statement hatası: {Statement}", shortStatement);
                }
            }

            return TypedResults.Ok<object>(new
            {
                success = true,
                message = "Import tamamlandı",
                stats = new
                {
                    schemas = parseResult.Schemas.Count,
                    tables = parseResult.TotalTableCount,
                    columns = parseResult.TotalColumnCount,
                    foreignKeys = parseResult.TotalForeignKeyCount,
                    statementsExecuted = executedCount,
                    totalStatements = statements.Count
                },
                errors = errors.Count > 0 ? errors : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Neo4j import hatası");
            return TypedResults.BadRequest<object>(new
            {
                success = false,
                message = "Import hatası",
                error = ex.Message
            });
        }
    }

    private static async Task<Ok<object>> GetCypherScript(
        ISchemaParserService parserService)
    {
        var parseResult = await parserService.ParseFromResourceAsync("adventurerworks_schema.md");
        var cypherScript = parserService.GenerateCypherImportScript(parseResult);

        return TypedResults.Ok<object>(new
        {
            parseResult = new
            {
                schemas = parseResult.Schemas.Count,
                tables = parseResult.TotalTableCount,
                columns = parseResult.TotalColumnCount,
                foreignKeys = parseResult.TotalForeignKeyCount,
                isSuccess = parseResult.IsSuccess,
                errors = parseResult.Errors
            },
            cypherScript,
            scriptLength = cypherScript.Length
        });
    }

    private static async Task<Results<Ok<object>, BadRequest<object>>> RefreshSchema(
        ISchemaParserService parserService,
        ISchemaGraphService schemaGraphService,
        IOptions<Neo4jSettings> settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Neo4jEndpoints");
        
        try
        {
            logger.LogInformation("Schema refresh başlatılıyor...");
            
            // 1. Mevcut verileri sil
            await ClearAllDataInternal(settings.Value, logger);
            logger.LogInformation("Mevcut veriler silindi");
            
            // 2. Schema dosyasını parse et
            var parseResult = await parserService.ParseFromResourceAsync("adventurerworks_schema.md");

            if (!parseResult.IsSuccess)
            {
                return TypedResults.BadRequest<object>(new
                {
                    success = false,
                    message = "Schema parse hatası",
                    errors = parseResult.Errors
                });
            }

            logger.LogInformation(
                "Schema parse edildi. Tablo: {Tables}, Kolon: {Columns}, FK: {FKs}, ExplicitFK: {ExplicitFKs}",
                parseResult.TotalTableCount,
                parseResult.TotalColumnCount,
                parseResult.TotalForeignKeyCount,
                parseResult.ForeignKeyRelations.Count);

            // 3. Cypher script oluştur
            var cypherScript = parserService.GenerateCypherImportScript(parseResult);

            // 4. Neo4j'ye bağlan ve çalıştır
            var neo4JSettings = settings.Value;
            using var driver = GraphDatabase.Driver(
                neo4JSettings.Uri,
                AuthTokens.Basic(neo4JSettings.Username, neo4JSettings.Password));

            await using var session = driver.AsyncSession(o => o.WithDatabase(neo4JSettings.Database));

            var statements = SplitCypherStatements(cypherScript);
            var executedCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

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
                    errors.Add($"{shortStatement} - {ex.Message}");
                    logger.LogWarning(ex, "Cypher statement hatası: {Statement}", shortStatement);
                }
            }

            // 5. Sonucu doğrula
            var newStats = await schemaGraphService.GetStatsAsync();

            logger.LogInformation(
                "Schema refresh tamamlandı. Şema: {Schemas}, Tablo: {Tables}, Kolon: {Columns}, FK: {FKs}",
                newStats.TotalSchemas,
                newStats.TotalTables,
                newStats.TotalColumns,
                newStats.TotalForeignKeys);

            return TypedResults.Ok<object>(new
            {
                success = true,
                message = "Schema refresh tamamlandı",
                parseStats = new
                {
                    schemas = parseResult.Schemas.Count,
                    tables = parseResult.TotalTableCount,
                    columns = parseResult.TotalColumnCount,
                    columnForeignKeys = parseResult.TotalForeignKeyCount,
                    explicitForeignKeys = parseResult.ForeignKeyRelations.Count
                },
                importStats = new
                {
                    statementsExecuted = executedCount,
                    statementsWithErrors = errorCount,
                    totalStatements = statements.Count
                },
                neo4jStats = new
                {
                    schemas = newStats.TotalSchemas,
                    tables = newStats.TotalTables,
                    views = newStats.TotalViews,
                    columns = newStats.TotalColumns,
                    foreignKeys = newStats.TotalForeignKeys
                },
                errors = errors.Count > 0 ? errors.Take(10).ToList() : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema refresh hatası");
            return TypedResults.BadRequest<object>(new
            {
                success = false,
                message = "Refresh hatası",
                error = ex.Message
            });
        }
    }

    private static async Task<Results<Ok<object>, BadRequest<object>>> ClearAllData(
        IOptions<Neo4jSettings> settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Neo4jEndpoints");
        
        try
        {
            await ClearAllDataInternal(settings.Value, logger);
            
            return TypedResults.Ok<object>(new
            {
                success = true,
                message = "Tüm şema verileri silindi"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Veri silme hatası");
            return TypedResults.BadRequest<object>(new
            {
                success = false,
                message = "Silme hatası",
                error = ex.Message
            });
        }
    }

    private static async Task ClearAllDataInternal(Neo4jSettings settings, Microsoft.Extensions.Logging.ILogger logger)
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
            
            // Full-text index'leri sil (hata verirse atla)
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
                logger.LogDebug("Clear statement executed: {Statement}", statement);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Clear statement hatası (devam ediliyor): {Statement} - {Error}", 
                    statement, ex.Message);
            }
        }

        logger.LogInformation("Tüm şema verileri silindi");
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
                // Eğer biriken statement varsa kaydet
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

    #endregion

    #region Hybrid Approach - Database Schema Import

    private static async Task<Results<Ok<object>, BadRequest<object>>> ImportFromDatabase(
        ImportFromDatabaseRequest request,
        IDatabaseSchemaReader databaseSchemaReader,
        ISchemaParserService parserService,
        IOptions<Neo4jSettings> settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Neo4jEndpoints");

        try
        {
            logger.LogInformation(
                "Veritabanından şema import başlatılıyor: {Source}, ClearExisting: {ClearExisting}",
                request.SourceName, request.ClearExistingSource);

            // 1. Bağlantıyı test et
            var connectionString = request.ConnectionString;
            if (!await databaseSchemaReader.TestConnectionAsync(connectionString))
            {
                return TypedResults.BadRequest<object>(new
                {
                    success = false,
                    message = "Veritabanı bağlantısı başarısız"
                });
            }

            // 2. Alias konfigürasyonunu yükle (varsa)
            AliasConfiguration? aliasConfig = null;
            if (!string.IsNullOrEmpty(request.AliasConfigJson))
            {
                aliasConfig = System.Text.Json.JsonSerializer.Deserialize<AliasConfiguration>(
                    request.AliasConfigJson);
            }

            // 3. Şemayı veritabanından oku
            var schemaResult = await databaseSchemaReader.ReadSchemaFromDatabaseAsync(
                connectionString, request.SourceName, aliasConfig);

            logger.LogInformation(
                "Şema okundu: {Tables} tablo, {Columns} kolon, {FKs} FK",
                schemaResult.TotalTableCount,
                schemaResult.TotalColumnCount,
                schemaResult.TotalForeignKeyCount);

            // 4. Mevcut source verilerini sil (isteğe bağlı)
            var neo4JSettings = settings.Value;
            using var driver = GraphDatabase.Driver(
                neo4JSettings.Uri,
                AuthTokens.Basic(neo4JSettings.Username, neo4JSettings.Password));

            await using var session = driver.AsyncSession(o => o.WithDatabase(neo4JSettings.Database));

            if (request.ClearExistingSource)
            {
                await ClearSourceDataInternal(session, request.SourceName, logger);
                logger.LogInformation("Mevcut kaynak verileri silindi: {Source}", request.SourceName);
            }

            // 5. Cypher script oluştur ve çalıştır
            var cypherScript = GenerateCypherFromDatabaseSchema(schemaResult, request.SourceName);
            var statements = SplitCypherStatements(cypherScript);

            var executedCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

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
                    errors.Add($"{shortStatement} - {ex.Message}");
                    logger.LogWarning(ex, "Cypher statement hatası: {Statement}", shortStatement);
                }
            }

            logger.LogInformation(
                "Import tamamlandı: {Executed}/{Total} statement, {Errors} hata",
                executedCount, statements.Count, errorCount);

            return TypedResults.Ok<object>(new
            {
                success = true,
                message = "Veritabanından import tamamlandı",
                source = request.SourceName,
                stats = new
                {
                    schemas = schemaResult.Schemas.Count,
                    tables = schemaResult.TotalTableCount,
                    columns = schemaResult.TotalColumnCount,
                    foreignKeys = schemaResult.TotalForeignKeyCount,
                    statementsExecuted = executedCount,
                    statementsWithErrors = errorCount
                },
                errors = errors.Count > 0 ? errors.Take(10).ToList() : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Veritabanından import hatası");
            return TypedResults.BadRequest<object>(new
            {
                success = false,
                message = "Import hatası",
                error = ex.Message
            });
        }
    }

    #endregion

    #region Multi-Source Operations

    private static async Task<Ok<IEnumerable<string>>> GetAvailableSources(
        ISchemaGraphService schemaGraphService)
    {
        var sources = await schemaGraphService.GetAvailableSourcesAsync();
        return TypedResults.Ok(sources);
    }

    private static async Task<Ok<IEnumerable<TableInfo>>> GetTablesBySource(
        string source,
        ISchemaGraphService schemaGraphService)
    {
        var tables = await schemaGraphService.GetTablesBySourceAsync(source);
        return TypedResults.Ok(tables);
    }

    private static async Task<Ok<IEnumerable<SchemaInfo>>> GetSchemasBySource(
        string source,
        ISchemaGraphService schemaGraphService)
    {
        var schemas = await schemaGraphService.GetSchemasBySourceAsync(source);
        return TypedResults.Ok(schemas);
    }

    private static async Task<Ok<SchemaCatalogStats>> GetStatsBySource(
        string source,
        ISchemaGraphService schemaGraphService)
    {
        var stats = await schemaGraphService.GetStatsBySourceAsync(source);
        return TypedResults.Ok(stats);
    }

    private static async Task<Results<Ok<object>, BadRequest<object>>> DeleteSource(
        string source,
        ISchemaGraphService schemaGraphService)
    {
        var result = await schemaGraphService.DeleteSourceDataAsync(source);

        if (result)
        {
            return TypedResults.Ok<object>(new
            {
                success = true,
                message = $"'{source}' kaynağına ait veriler silindi"
            });
        }

        return TypedResults.BadRequest<object>(new
        {
            success = false,
            message = "Silme işlemi başarısız"
        });
    }

    private static async Task<Ok<IEnumerable<TableInfo>>> SearchTablesBySource(
        string query,
        string source,
        int maxResults,
        ISchemaGraphService schemaGraphService)
    {
        var tables = await schemaGraphService.FindRelevantTablesBySourceAsync(
            query, source, maxResults > 0 ? maxResults : 10);
        return TypedResults.Ok(tables);
    }

    private static async Task ClearSourceDataInternal(
        IAsyncSession session,
        string source,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var clearStatements = new[]
        {
            $"MATCH (c:Column)-[:BELONGS_TO]->(t:Table {{source: '{source}'}}) DETACH DELETE c",
            $"MATCH (t:Table {{source: '{source}'}}) DETACH DELETE t",
            $"MATCH (s:Schema {{source: '{source}'}}) DETACH DELETE s"
        };

        foreach (var statement in clearStatements)
        {
            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(statement);
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning("Source clear hatası (devam ediliyor): {Error}", ex.Message);
            }
        }
    }

    private static string GenerateCypherFromDatabaseSchema(SchemaParseResult schemaResult, string source)
    {
        var sb = new System.Text.StringBuilder();

        // Full-text index oluştur (yoksa)
        sb.AppendLine("// Full-text search index'leri");
        sb.AppendLine($"CREATE FULLTEXT INDEX table_search IF NOT EXISTS FOR (t:Table) ON EACH [t.name, t.fullName, t.description, t.schema];");
        sb.AppendLine($"CREATE FULLTEXT INDEX column_search IF NOT EXISTS FOR (c:Column) ON EACH [c.name, c.alias, c.description];");
        sb.AppendLine();

        // Şemaları oluştur
        sb.AppendLine("// Şemalar");
        foreach (var schema in schemaResult.Schemas)
        {
            sb.AppendLine($"MERGE (s:Schema {{name: '{schema.Name}', source: '{source}'}})");
            sb.AppendLine($"SET s.description = '{EscapeCypher(schema.Description ?? "")}';");
        }
        sb.AppendLine();

        // Tabloları oluştur
        sb.AppendLine("// Tablolar");
        foreach (var table in schemaResult.Tables)
        {
            sb.AppendLine($"MERGE (t:Table {{fullName: '{table.FullName}', source: '{source}'}})");
            sb.AppendLine($"SET t.name = '{table.Name}',");
            sb.AppendLine($"    t.schema = '{table.Schema}',");
            sb.AppendLine($"    t.description = '{EscapeCypher(table.Description ?? "")}',");
            sb.AppendLine($"    t.type = '{table.Type ?? "Table"}';");
        }
        sb.AppendLine();

        // Schema-Table ilişkileri
        sb.AppendLine("// Schema-Table ilişkileri");
        foreach (var table in schemaResult.Tables)
        {
            sb.AppendLine($"MATCH (s:Schema {{name: '{table.Schema}', source: '{source}'}}), (t:Table {{fullName: '{table.FullName}', source: '{source}'}})");
            sb.AppendLine("MERGE (s)-[:CONTAINS]->(t);");
        }
        sb.AppendLine();

        // Kolonları oluştur
        sb.AppendLine("// Kolonlar");
        foreach (var table in schemaResult.Tables)
        {
            foreach (var col in table.Columns)
            {
                sb.AppendLine($"MATCH (t:Table {{fullName: '{table.FullName}', source: '{source}'}})");
                sb.AppendLine($"MERGE (c:Column {{name: '{col.Name}', tableName: '{table.FullName}', source: '{source}'}})");
                sb.AppendLine($"SET c.dataType = '{col.DataType ?? ""}',");
                sb.AppendLine($"    c.alias = '{EscapeCypher(col.Alias ?? col.Name)}',");
                sb.AppendLine($"    c.description = '{EscapeCypher(col.Description ?? "")}',");
                sb.AppendLine($"    c.isPrimaryKey = {col.IsPrimaryKey.ToString().ToLower()},");
                sb.AppendLine($"    c.isForeignKey = {col.IsForeignKey.ToString().ToLower()},");
                sb.AppendLine($"    c.fkTable = {(string.IsNullOrEmpty(col.FkTable) ? "null" : $"'{col.FkTable}'")}");
                sb.AppendLine("MERGE (t)-[:HAS_COLUMN]->(c);");
            }
        }
        sb.AppendLine();

        // Foreign Key ilişkileri
        sb.AppendLine("// FK JOINS_WITH ilişkileri");
        foreach (var fk in schemaResult.ForeignKeyRelations)
        {
            var sourceTable = $"{fk.SourceSchema}.{fk.SourceTable}";
            var targetTable = $"{fk.TargetSchema}.{fk.TargetTable}";

            sb.AppendLine($"MATCH (t1:Table {{fullName: '{sourceTable}', source: '{source}'}}), (t2:Table {{fullName: '{targetTable}', source: '{source}'}})");
            sb.AppendLine($"MERGE (t1)-[r:JOINS_WITH {{via: '{fk.SourceColumn}', fkColumn: '{fk.TargetColumn}'}}]->(t2);");
        }

        return sb.ToString();
    }

    private static string EscapeCypher(string value)
    {
        return value?.Replace("'", "\\'").Replace("\n", " ").Replace("\r", "") ?? "";
    }

    #endregion
}

/// <summary>
/// Veritabanından şema import isteği
/// </summary>
public class ImportFromDatabaseRequest
{
    /// <summary>
    /// Kaynak adı (Neo4j'de source property olarak kullanılır)
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// SQL Server connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Alias konfigürasyonu (JSON formatında, opsiyonel)
    /// </summary>
    public string? AliasConfigJson { get; set; }

    /// <summary>
    /// Mevcut source verilerini silip yeniden oluştur
    /// </summary>
    public bool ClearExistingSource { get; set; } = true;
}
