using AI.Application.DTOs.Neo4j;
using Neo4jColumnInfo = AI.Application.DTOs.Neo4j.ColumnInfo;

using System.Text;
using AI.Application.Configuration;
using AI.Application.DTOs.SchemaCatalog;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace AI.Infrastructure.Adapters.AI.Neo4j;

/// <summary>
/// Neo4j Schema Catalog servisi
/// Veritabanı şemasını Neo4j'de saklar ve dinamik SQL üretimi için şema bilgisi sağlar
/// </summary>
public class SchemaGraphService : ISchemaGraphService, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly Neo4jSettings _settings;
    private readonly ILogger<SchemaGraphService> _logger;

    public SchemaGraphService(
        IOptions<Neo4jSettings> settings,
        ILogger<SchemaGraphService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _driver = GraphDatabase.Driver(
            _settings.Uri,
            AuthTokens.Basic(_settings.Username, _settings.Password),
            config => config
                .WithMaxConnectionPoolSize(_settings.MaxConnectionPoolSize)
                .WithConnectionAcquisitionTimeout(_settings.ConnectionAcquisitionTimeout));

        _logger.LogInformation("Neo4j driver initialized: {Uri}", _settings.Uri);
    }

    #region Table Search

    public async Task<IEnumerable<TableInfo>> FindRelevantTablesAsync(string userQuery, int maxResults = 10)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Schema Catalog devre dışı");
            return Enumerable.Empty<TableInfo>();
        }

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                // Full-text search ile ilgili tabloları bul
                var query = @"
                    CALL db.index.fulltext.queryNodes('table_search', $query) 
                    YIELD node, score
                    WHERE score > $minScore
                    RETURN node.name AS name, 
                           node.fullName AS fullName,
                           node.schema AS schema,
                           node.description AS description,
                           node.type AS type,
                           score
                    ORDER BY score DESC
                    LIMIT $limit";

                var cursor = await tx.RunAsync(query, new
                {
                    query = PrepareSearchQuery(userQuery),
                    minScore = _settings.MinRelevanceScore,
                    limit = maxResults
                });

                return await cursor.ToListAsync();
            });

            var tables = new List<TableInfo>();
            foreach (var r in result)
            {
                try
                {
                    tables.Add(new TableInfo
                    {
                        Name = r["name"]?.As<string>() ?? string.Empty,
                        FullName = r["fullName"]?.As<string>() ?? string.Empty,
                        Schema = r["schema"]?.As<string>() ?? string.Empty,
                        Description = r["description"]?.As<string>() ?? string.Empty,
                        Type = r["type"]?.As<string>() ?? "Table",
                        RelevanceScore = r["score"]?.As<double>() ?? 0.0
                    });
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Tablo parse hatası, atlanıyor. Keys: {Keys}", 
                        string.Join(", ", r.Keys));
                }
            }
            
            _logger.LogDebug("FindRelevantTablesAsync: {Count} tablo bulundu", tables.Count);
            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tablo arama hatası: {Query}", userQuery);
            return Enumerable.Empty<TableInfo>();
        }
    }

    public async Task<IEnumerable<TableInfo>> SearchTablesByKeywordsAsync(IEnumerable<string> keywords)
    {
        var keywordList = keywords.ToList();
        if (!keywordList.Any())
            return Enumerable.Empty<TableInfo>();

        var searchQuery = string.Join(" OR ", keywordList);
        return await FindRelevantTablesAsync(searchQuery);
    }

    public async Task<IEnumerable<Neo4jColumnInfo>> SearchColumnsByAliasAsync(string alias)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(alias))
            return Enumerable.Empty<Neo4jColumnInfo>();

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    CALL db.index.fulltext.queryNodes('column_search', $alias) 
                    YIELD node, score
                    WHERE score > $minScore
                    RETURN node.name AS name,
                           node.tableName AS tableName,
                           node.dataType AS dataType,
                           node.alias AS alias,
                           node.description AS description,
                           node.isPrimaryKey AS isPrimaryKey,
                           node.isForeignKey AS isForeignKey,
                           node.fkTable AS fkTable,
                           node.fkColumn AS fkColumn,
                           score
                    ORDER BY score DESC
                    LIMIT 20";

                var cursor = await tx.RunAsync(query, new
                {
                    alias = PrepareSearchQuery(alias),
                    minScore = _settings.MinRelevanceScore
                });

                return await cursor.ToListAsync();
            });

            var columns = new List<Neo4jColumnInfo>();
            foreach (var r in result)
            {
                try
                {
                    columns.Add(new Neo4jColumnInfo
                    {
                        Name = r["name"]?.As<string>() ?? string.Empty,
                        TableName = r["tableName"]?.As<string>() ?? string.Empty,
                        DataType = r["dataType"]?.As<string>() ?? string.Empty,
                        Alias = r["alias"]?.As<string>() ?? string.Empty,
                        Description = r["description"]?.As<string>() ?? string.Empty,
                        IsPrimaryKey = r["isPrimaryKey"]?.As<bool>() ?? false,
                        IsForeignKey = r["isForeignKey"]?.As<bool>() ?? false,
                        FkTable = r["fkTable"]?.As<string?>(),
                        FkColumn = r["fkColumn"]?.As<string?>()
                    });
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Kolon parse hatası, atlanıyor");
                }
            }
            return columns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kolon arama hatası: {Alias}", alias);
            return Enumerable.Empty<Neo4jColumnInfo>();
        }
    }

    #endregion

    #region JOIN Path Finding

    public async Task<JoinPath?> FindJoinPathAsync(string table1, string table2)
    {
        if (!_settings.Enabled)
            return null;

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                // En kısa JOIN path'i bul
                var query = @"
                    MATCH path = shortestPath(
                        (t1:Table {fullName: $table1})-[:JOINS_WITH*1.." + _settings.MaxJoinHops + @"]-(t2:Table {fullName: $table2})
                    )
                    RETURN [n IN nodes(path) | n.fullName] AS tables,
                           [r IN relationships(path) | {
                               fromTable: startNode(r).fullName,
                               toTable: endNode(r).fullName,
                               via: r.via,
                               fkColumn: r.fkColumn
                           }] AS joins";

                var cursor = await tx.RunAsync(query, new { table1, table2 });
                return await cursor.SingleOrDefaultAsync();
            });

            if (result == null)
            {
                _logger.LogDebug("JOIN path bulunamadı: {Table1} -> {Table2}", table1, table2);
                return null;
            }

            var joinPath = new JoinPath
            {
                Tables = result["tables"].As<List<string>>()
            };

            var joins = result["joins"].As<List<IDictionary<string, object>>>();
            foreach (var join in joins)
            {
                joinPath.Joins.Add(new JoinInfo
                {
                    FromTable = join["fromTable"]?.ToString() ?? string.Empty,
                    ToTable = join["toTable"]?.ToString() ?? string.Empty,
                    Via = join["via"]?.ToString(),
                    FkColumn = join["fkColumn"]?.ToString()
                });
            }

            return joinPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JOIN path arama hatası: {Table1} -> {Table2}", table1, table2);
            return null;
        }
    }

    public async Task<IEnumerable<JoinPath>> FindJoinPathsAsync(IEnumerable<string> tableNames)
    {
        var tables = tableNames.ToList();
        if (tables.Count < 2)
            return Enumerable.Empty<JoinPath>();

        var paths = new List<JoinPath>();

        // Her ardışık tablo çifti için path bul
        for (int i = 0; i < tables.Count - 1; i++)
        {
            var path = await FindJoinPathAsync(tables[i], tables[i + 1]);
            if (path != null)
            {
                paths.Add(path);
            }
        }

        return paths;
    }

    #endregion

    #region Schema Retrieval

    public async Task<TableSchema?> GetTableSchemaAsync(string tableName)
    {
        if (!_settings.Enabled)
            return null;

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (t:Table {fullName: $tableName})-[:HAS_COLUMN]->(c:Column)
                    RETURN t.name AS tableName,
                           t.fullName AS fullName,
                           t.schema AS schema,
                           t.description AS tableDescription,
                           t.type AS tableType,
                           collect({
                               name: c.name,
                               dataType: c.dataType,
                               alias: c.alias,
                               description: c.description,
                               isPrimaryKey: c.isPrimaryKey,
                               isForeignKey: c.isForeignKey,
                               fkTable: c.fkTable,
                               fkColumn: c.fkColumn
                           }) AS columns";

                var cursor = await tx.RunAsync(query, new { tableName });
                return await cursor.SingleOrDefaultAsync();
            });

            if (result == null)
            {
                _logger.LogDebug("Tablo bulunamadı: {TableName}", tableName);
                return null;
            }

            var schema = new TableSchema
            {
                Name = result["tableName"].As<string>(),
                FullName = result["fullName"].As<string>(),
                Schema = result["schema"].As<string>(),
                Description = result["tableDescription"].As<string>(),
                Type = result["tableType"].As<string>()
            };

            var columns = result["columns"].As<List<IDictionary<string, object>>>();
            var pkCount = 0;
            var fkCount = 0;
            
            foreach (var col in columns)
            {
                // Boolean değerleri güvenli parse et (Neo4j farklı formatlar döndürebilir)
                col.TryGetValue("isPrimaryKey", out var pkValue);
                col.TryGetValue("isForeignKey", out var fkValue);
                var isPk = ParseBooleanValue(pkValue);
                var isFk = ParseBooleanValue(fkValue);
                
                if (isPk) pkCount++;
                if (isFk) fkCount++;
                
                schema.Columns.Add(new Neo4jColumnInfo
                {
                    Name = col["name"]?.ToString() ?? string.Empty,
                    TableName = schema.FullName,
                    DataType = col["dataType"]?.ToString() ?? string.Empty,
                    Alias = col["alias"]?.ToString() ?? string.Empty,
                    Description = col["description"]?.ToString() ?? string.Empty,
                    IsPrimaryKey = isPk,
                    IsForeignKey = isFk,
                    FkTable = col["fkTable"]?.ToString(),
                    FkColumn = col["fkColumn"]?.ToString()
                });
            }
            
            _logger.LogDebug(
                "Tablo şeması alındı: {TableName} - {ColCount} kolon, {PkCount} PK, {FkCount} FK", 
                tableName, columns.Count, pkCount, fkCount);

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tablo şeması getirme hatası: {TableName}", tableName);
            return null;
        }
    }

    public async Task<IEnumerable<TableSchema>> GetTableSchemasAsync(IEnumerable<string> tableNames)
    {
        var schemas = new List<TableSchema>();

        foreach (var tableName in tableNames)
        {
            var schema = await GetTableSchemaAsync(tableName);
            if (schema != null)
            {
                schemas.Add(schema);
            }
        }

        return schemas;
    }

    public async Task<IEnumerable<TableInfo>> GetTablesBySchemaAsync(string schemaName)
    {
        if (!_settings.Enabled)
            return Enumerable.Empty<TableInfo>();

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (s:Schema {name: $schemaName})-[:CONTAINS]->(t:Table)
                    RETURN t.name AS name,
                           t.fullName AS fullName,
                           t.schema AS schema,
                           t.description AS description,
                           t.type AS type
                    ORDER BY t.name";

                var cursor = await tx.RunAsync(query, new { schemaName });
                return await cursor.ToListAsync();
            });

            var tables = new List<TableInfo>();
            foreach (var r in result)
            {
                try
                {
                    tables.Add(new TableInfo
                    {
                        Name = r["name"]?.As<string>() ?? string.Empty,
                        FullName = r["fullName"]?.As<string>() ?? string.Empty,
                        Schema = r["schema"]?.As<string>() ?? string.Empty,
                        Description = r["description"]?.As<string>() ?? string.Empty,
                        Type = r["type"]?.As<string>() ?? "Table"
                    });
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Tablo parse hatası, atlanıyor");
                }
            }
            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şema tabloları getirme hatası: {SchemaName}", schemaName);
            return Enumerable.Empty<TableInfo>();
        }
    }

    public async Task<IEnumerable<SchemaInfo>> GetAllSchemasAsync()
    {
        if (!_settings.Enabled)
            return Enumerable.Empty<SchemaInfo>();

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (s:Schema)
                    OPTIONAL MATCH (s)-[:CONTAINS]->(t:Table)
                    RETURN s.name AS name,
                           s.description AS description,
                           count(t) AS tableCount
                    ORDER BY s.name";

                var cursor = await tx.RunAsync(query);
                return await cursor.ToListAsync();
            });

            var schemas = new List<SchemaInfo>();
            foreach (var r in result)
            {
                try
                {
                    schemas.Add(new SchemaInfo
                    {
                        Name = r["name"]?.As<string>() ?? string.Empty,
                        Description = r["description"]?.As<string>() ?? string.Empty,
                        TableCount = r["tableCount"]?.As<int>() ?? 0
                    });
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Schema parse hatası, atlanıyor");
                }
            }
            return schemas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şema listesi getirme hatası");
            return Enumerable.Empty<SchemaInfo>();
        }
    }

    #endregion

    #region Prompt Generation

    public async Task<string> GenerateSchemaPromptAsync(IEnumerable<string> tableNames)
    {
        var schemas = await GetTableSchemasAsync(tableNames);
        var schemaList = schemas.ToList();

        if (!schemaList.Any())
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("## Veritabanı Şeması\n");

        foreach (var schema in schemaList)
        {
            sb.AppendLine($"### {schema.FullName}");
            sb.AppendLine($"**Açıklama:** {schema.Description}\n");
            sb.AppendLine("| Kolon | Tip | Alias | Açıklama |");
            sb.AppendLine("|-------|-----|-------|----------|");

            foreach (var col in schema.Columns)
            {
                var notes = new List<string>();
                if (col.IsPrimaryKey) notes.Add("PK");
                if (col.IsForeignKey && !string.IsNullOrEmpty(col.FkTable))
                    notes.Add($"FK → {col.FkTable}");

                var noteStr = notes.Any() ? $" ({string.Join(", ", notes)})" : "";
                sb.AppendLine($"| {col.Name} | {col.DataType} | {col.Alias} | {col.Description}{noteStr} |");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public async Task<string> GenerateDynamicSchemaPromptAsync(string userQuery)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Schema Catalog devre dışı, dinamik prompt oluşturulamadı");
            return string.Empty;
        }

        // 1. İlgili tabloları bul
        var relevantTables = await FindRelevantTablesAsync(userQuery, _settings.MaxRelevantTables);
        var tableList = relevantTables.ToList();

        if (!tableList.Any())
        {
            _logger.LogWarning("İlgili tablo bulunamadı: {Query}", userQuery);
            return string.Empty;
        }

        _logger.LogInformation(
            "Dinamik prompt için {Count} tablo bulundu: {Tables}",
            tableList.Count,
            string.Join(", ", tableList.Select(t => t.FullName)));

        var tableNames = tableList.Select(t => t.FullName).ToList();

        // 2. Şema bilgisini oluştur
        var schemaPrompt = await GenerateSchemaPromptAsync(tableNames);

        // 3. JOIN path'leri bul ve ekle
        var joinPaths = await FindJoinPathsAsync(tableNames);
        var joinPathList = joinPaths.ToList();

        if (joinPathList.Any())
        {
            var sb = new StringBuilder(schemaPrompt);
            sb.AppendLine("## JOIN Yolları\n");

            foreach (var path in joinPathList)
            {
                sb.AppendLine($"**{path.Tables.First()} → {path.Tables.Last()}**");
                foreach (var join in path.Joins)
                {
                    sb.AppendLine($"- `{join.FromTable}`.`{join.Via}` → `{join.ToTable}`.`{join.FkColumn}`");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        return schemaPrompt;
    }

    #endregion

    #region Health & Stats

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));
            await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("RETURN 1");
                await cursor.SingleAsync();
            });

            _logger.LogInformation("Neo4j bağlantısı başarılı");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo4j bağlantı hatası");
            return false;
        }
    }

    public async Task<SchemaCatalogStats> GetStatsAsync()
    {
        var stats = new SchemaCatalogStats();

        if (!_settings.Enabled)
            return stats;

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (s:Schema)
                    WITH count(s) AS schemaCount
                    MATCH (t:Table)
                    WITH schemaCount, 
                         count(CASE WHEN t.type = 'Table' THEN 1 END) AS tableCount,
                         count(CASE WHEN t.type = 'View' THEN 1 END) AS viewCount
                    MATCH (c:Column)
                    WITH schemaCount, tableCount, viewCount, count(c) AS columnCount
                    MATCH (c:Column {isForeignKey: true})
                    RETURN schemaCount, tableCount, viewCount, columnCount, count(c) AS fkCount";

                var cursor = await tx.RunAsync(query);
                return await cursor.SingleOrDefaultAsync();
            });

            if (result != null)
            {
                stats.TotalSchemas = result["schemaCount"].As<int>();
                stats.TotalTables = result["tableCount"].As<int>();
                stats.TotalViews = result["viewCount"].As<int>();
                stats.TotalColumns = result["columnCount"].As<int>();
                stats.TotalForeignKeys = result["fkCount"].As<int>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İstatistik getirme hatası");
        }

        return stats;
    }

    #endregion

    #region Private Methods

    private string PrepareSearchQuery(string query)
    {
        // Full-text search için query hazırla
        // Türkçe karakterleri koru, Lucene özel karakterlerini escape et
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2)
            .Select(w => w.Trim('?', '!', '.', ',', ';', ':'))
            .Select(EscapeLuceneSpecialChars)
            .Where(w => !string.IsNullOrWhiteSpace(w));

        return string.Join(" ", words);
    }

    /// <summary>
    /// Lucene query parser özel karakterlerini escape eder.
    /// Özel karakterler: + - && || ! ( ) { } [ ] ^ " ~ * ? : \ /
    /// </summary>
    private static string EscapeLuceneSpecialChars(string term)
    {
        var sb = new StringBuilder(term.Length + 4);
        foreach (var c in term)
        {
            if (c is '+' or '-' or '!' or '(' or ')' or '{' or '}' 
                 or '[' or ']' or '^' or '"' or '~' or '*' or '?' 
                 or ':' or '\\' or '/')
            {
                sb.Append('\\');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Neo4j'den dönen boolean değerleri güvenli bir şekilde parse eder
    /// Neo4j farklı formatlar döndürebilir (bool, string "true"/"false", int 0/1)
    /// </summary>
    private static bool ParseBooleanValue(object? value)
    {
        if (value == null)
            return false;

        if (value is bool b)
            return b;

        if (value is string s)
            return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";

        if (value is int i)
            return i != 0;

        if (value is long l)
            return l != 0;

        return false;
    }

    #endregion

    #region Source-aware Methods (Çoklu Veritabanı Desteği)

    public async Task<IEnumerable<TableInfo>> FindRelevantTablesBySourceAsync(
        string userQuery, 
        string source, 
        int maxResults = 10)
    {
        if (!_settings.Enabled)
            return Enumerable.Empty<TableInfo>();

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                // Source filtrelemeli full-text search
                var query = @"
                    CALL db.index.fulltext.queryNodes('table_search', $query) 
                    YIELD node, score
                    WHERE score > $minScore AND node.source = $source
                    RETURN node.name AS name, 
                           node.fullName AS fullName,
                           node.schema AS schema,
                           node.description AS description,
                           node.type AS type,
                           node.source AS source,
                           score
                    ORDER BY score DESC
                    LIMIT $limit";

                var cursor = await tx.RunAsync(query, new
                {
                    query = PrepareSearchQuery(userQuery),
                    minScore = _settings.MinRelevanceScore,
                    source,
                    limit = maxResults
                });

                return await cursor.ToListAsync();
            });

            var tables = new List<TableInfo>();
            foreach (var r in result)
            {
                try
                {
                    tables.Add(new TableInfo
                    {
                        Name = r["name"]?.As<string>() ?? string.Empty,
                        FullName = r["fullName"]?.As<string>() ?? string.Empty,
                        Schema = r["schema"]?.As<string>() ?? string.Empty,
                        Description = r["description"]?.As<string>() ?? string.Empty,
                        Type = r["type"]?.As<string>() ?? "Table",
                        RelevanceScore = r["score"]?.As<double>() ?? 0.0
                    });
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Tablo parse hatası (source-aware), atlanıyor");
                }
            }

            _logger.LogDebug(
                "FindRelevantTablesBySourceAsync: {Count} tablo bulundu (Source: {Source})", 
                tables.Count, source);
            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tablo arama hatası (Source: {Source}): {Query}", source, userQuery);
            return Enumerable.Empty<TableInfo>();
        }
    }

    public async Task<IEnumerable<TableInfo>> GetTablesBySourceAsync(string source)
    {
        if (!_settings.Enabled)
            return Enumerable.Empty<TableInfo>();

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (t:Table {source: $source})
                    RETURN t.name AS name,
                           t.fullName AS fullName,
                           t.schema AS schema,
                           t.description AS description,
                           t.type AS type
                    ORDER BY t.schema, t.name";

                var cursor = await tx.RunAsync(query, new { source });
                return await cursor.ToListAsync();
            });

            var tables = new List<TableInfo>();
            foreach (var r in result)
            {
                try
                {
                    tables.Add(new TableInfo
                    {
                        Name = r["name"]?.As<string>() ?? string.Empty,
                        FullName = r["fullName"]?.As<string>() ?? string.Empty,
                        Schema = r["schema"]?.As<string>() ?? string.Empty,
                        Description = r["description"]?.As<string>() ?? string.Empty,
                        Type = r["type"]?.As<string>() ?? "Table"
                    });
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Tablo parse hatası, atlanıyor");
                }
            }

            _logger.LogDebug("GetTablesBySourceAsync: {Count} tablo (Source: {Source})", tables.Count, source);
            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tablo listesi getirme hatası (Source: {Source})", source);
            return Enumerable.Empty<TableInfo>();
        }
    }

    public async Task<IEnumerable<SchemaInfo>> GetSchemasBySourceAsync(string source)
    {
        if (!_settings.Enabled)
            return Enumerable.Empty<SchemaInfo>();

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (s:Schema {source: $source})
                    OPTIONAL MATCH (s)-[:CONTAINS]->(t:Table)
                    RETURN s.name AS name,
                           s.description AS description,
                           count(t) AS tableCount
                    ORDER BY s.name";

                var cursor = await tx.RunAsync(query, new { source });
                return await cursor.ToListAsync();
            });

            var schemas = new List<SchemaInfo>();
            foreach (var r in result)
            {
                try
                {
                    schemas.Add(new SchemaInfo
                    {
                        Name = r["name"]?.As<string>() ?? string.Empty,
                        Description = r["description"]?.As<string>() ?? string.Empty,
                        TableCount = r["tableCount"]?.As<int>() ?? 0
                    });
                }
                catch (Exception itemEx)
                {
                    _logger.LogWarning(itemEx, "Schema parse hatası, atlanıyor");
                }
            }
            return schemas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şema listesi getirme hatası (Source: {Source})", source);
            return Enumerable.Empty<SchemaInfo>();
        }
    }

    public async Task<SchemaCatalogStats> GetStatsBySourceAsync(string source)
    {
        var stats = new SchemaCatalogStats();

        if (!_settings.Enabled)
            return stats;

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (s:Schema {source: $source})
                    WITH count(s) AS schemaCount
                    MATCH (t:Table {source: $source})
                    WITH schemaCount, 
                         count(CASE WHEN t.type = 'Table' THEN 1 END) AS tableCount,
                         count(CASE WHEN t.type = 'View' THEN 1 END) AS viewCount
                    MATCH (c:Column)-[:BELONGS_TO]->(t2:Table {source: $source})
                    WITH schemaCount, tableCount, viewCount, count(DISTINCT c) AS columnCount
                    MATCH (c2:Column {isForeignKey: true})-[:BELONGS_TO]->(t3:Table {source: $source})
                    RETURN schemaCount, tableCount, viewCount, columnCount, count(DISTINCT c2) AS fkCount";

                var cursor = await tx.RunAsync(query, new { source });
                return await cursor.SingleOrDefaultAsync();
            });

            if (result != null)
            {
                stats.TotalSchemas = result["schemaCount"].As<int>();
                stats.TotalTables = result["tableCount"].As<int>();
                stats.TotalViews = result["viewCount"].As<int>();
                stats.TotalColumns = result["columnCount"].As<int>();
                stats.TotalForeignKeys = result["fkCount"].As<int>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İstatistik getirme hatası (Source: {Source})", source);
        }

        return stats;
    }

    public async Task<IEnumerable<string>> GetAvailableSourcesAsync()
    {
        if (!_settings.Enabled)
            return Enumerable.Empty<string>();

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (t:Table)
                    RETURN DISTINCT t.source AS source
                    ORDER BY source";

                var cursor = await tx.RunAsync(query);
                return await cursor.ToListAsync();
            });

            return result
                .Select(r => r["source"]?.As<string>())
                .Where(s => !string.IsNullOrEmpty(s))
                .Cast<string>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kaynak listesi getirme hatası");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> DeleteSourceDataAsync(string source)
    {
        if (!_settings.Enabled)
            return false;

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_settings.Database));

        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                // Önce kolonları sil
                await tx.RunAsync(@"
                    MATCH (c:Column)-[:BELONGS_TO]->(t:Table {source: $source})
                    DETACH DELETE c", new { source });

                // Sonra tabloları sil
                await tx.RunAsync(@"
                    MATCH (t:Table {source: $source})
                    DETACH DELETE t", new { source });

                // Şemaları sil
                await tx.RunAsync(@"
                    MATCH (s:Schema {source: $source})
                    DETACH DELETE s", new { source });
            });

            _logger.LogInformation("Kaynak verileri silindi: {Source}", source);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kaynak verilerini silme hatası: {Source}", source);
            return false;
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
        _logger.LogInformation("Neo4j driver disposed");
    }
}
