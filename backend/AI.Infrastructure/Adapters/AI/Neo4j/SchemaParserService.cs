using AI.Application.DTOs.Neo4j;
using Neo4jColumnInfo = AI.Application.DTOs.Neo4j.ColumnInfo;

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.Neo4j;

/// <summary>
/// adventurerworks_schema.md dosyasını parse eden servis
/// </summary>
public class SchemaParserService : ISchemaParserService
{
    private readonly ILogger<SchemaParserService> _logger;
    
    // Regex patterns — \r? tolerates both LF and CRLF line endings
    private static readonly Regex TableHeaderRegex = new(
        @"^###\s+(.+?)\s+Tablosu(?:\s+\(Görünüm\))?\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);
    
    private static readonly Regex SchemaHeaderRegex = new(
        @"^##\s+(.+?)\s+Şeması",
        RegexOptions.Compiled | RegexOptions.Multiline);
    
    private static readonly Regex PropertyTableRowRegex = new(
        @"\|\s*\*\*(.+?)\*\*\s*\|\s*(.+?)\s*\|",
        RegexOptions.Compiled);
    
    private static readonly Regex ColumnTableRowRegex = new(
        @"\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|",
        RegexOptions.Compiled);
    
    private static readonly Regex ForeignKeyRegex = new(
        @"\((.+?)\s+tablosuna\s+Foreign\s+Key\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex PrimaryKeyRegex = new(
        @"\(Primary\s+Key\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // FK bölümü için regex - | Tablo | Tablo_PK | FK_Kolon | Referans_Şema | Referans_Tablo | Referans_Kolon |
    private static readonly Regex ForeignKeyTableRowRegex = new(
        @"\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|",
        RegexOptions.Compiled);
    
    // FK Şema başlığı - ### HumanResources Şeması İlişkileri
    private static readonly Regex FKSchemaHeaderRegex = new(
        @"^###\s+(\w+)\s+Şeması\s+İlişkileri\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public SchemaParserService(ILogger<SchemaParserService> logger)
    {
        _logger = logger;
    }

    public SchemaParseResult ParseMarkdown(string markdownContent)
    {
        markdownContent = markdownContent.Replace("\r\n", "\n").Replace("\r", "\n");
        
        var result = new SchemaParseResult();
        var currentSchema = string.Empty;
        var schemaDescriptions = new Dictionary<string, string>
        {
            ["Sales"] = "Satış İşlemleri - Müşteri, sipariş ve satış temsilcisi verileri",
            ["Production"] = "Üretim ve Envanter - Ürünler, stok ve üretim verileri",
            ["Person"] = "Kişi ve İletişim - Müşteri ve çalışan kişisel bilgileri",
            ["Purchasing"] = "Satın Alma ve Tedarikçiler",
            ["HumanResources"] = "İnsan Kaynakları - Çalışan ve departman verileri",
        };

        try
        {
            // 1. Dosyayı tablo bloklarına böl ve parse et
            var tableBlocks = SplitIntoTableBlocks(markdownContent);
            _logger.LogInformation("Toplam {Count} tablo bloğu bulundu", tableBlocks.Count);

            foreach (var block in tableBlocks)
            {
                try
                {
                    var table = ParseTableBlock(block, ref currentSchema);
                    if (table != null)
                    {
                        result.Tables.Add(table);
                        _logger.LogDebug("Tablo parse edildi: {TableName}", table.FullName);
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Tablo parse hatası: {ex.Message}";
                    result.Errors.Add(errorMsg);
                    _logger.LogWarning(ex, "Tablo parse hatası: {Block}", block.Substring(0, Math.Min(100, block.Length)));
                }
            }

            // 2. FK ilişkileri bölümünü parse et
            var fkRelations = ParseForeignKeySection(markdownContent);
            result.ForeignKeyRelations.AddRange(fkRelations);
            _logger.LogInformation("FK ilişkileri parse edildi: {Count} ilişki", fkRelations.Count);

            // 3. FK ilişkilerini kolonlara uygula (eksik olanları tamamla)
            ApplyForeignKeyRelationsToColumns(result);

            // 4. Schema bilgilerini oluştur
            var schemaGroups = result.Tables.GroupBy(t => t.Schema);
            foreach (var group in schemaGroups)
            {
                result.Schemas.Add(new SchemaInfo
                {
                    Name = group.Key,
                    Description = schemaDescriptions.GetValueOrDefault(group.Key, $"{group.Key} Şeması"),
                    TableCount = group.Count()
                });
            }

            _logger.LogInformation(
                "Parse tamamlandı. Şema: {SchemaCount}, Tablo: {TableCount}, Kolon: {ColumnCount}, FK: {FkCount}, ExplicitFK: {ExplicitFkCount}",
                result.Schemas.Count,
                result.TotalTableCount,
                result.TotalColumnCount,
                result.TotalForeignKeyCount,
                result.TotalExplicitForeignKeyCount);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Genel parse hatası: {ex.Message}");
            _logger.LogError(ex, "Markdown parse hatası");
        }

        return result;
    }
    
    /// <summary>
    /// FK ilişkileri bölümünü parse eder
    /// </summary>
    private List<ForeignKeyRelation> ParseForeignKeySection(string content)
    {
        var relations = new List<ForeignKeyRelation>();
        
        // FK bölümünün başlangıcını bul
        var fkSectionStart = content.IndexOf("## 🔗 Foreign Key İlişkileri", StringComparison.OrdinalIgnoreCase);
        if (fkSectionStart == -1)
        {
            _logger.LogDebug("FK ilişkileri bölümü bulunamadı");
            return relations;
        }
        
        var fkSection = content.Substring(fkSectionStart);
        var lines = fkSection.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var currentSchema = string.Empty;
        var inTable = false;
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            
            // Şema başlığını kontrol et
            var schemaMatch = FKSchemaHeaderRegex.Match(line);
            if (schemaMatch.Success)
            {
                currentSchema = schemaMatch.Groups[1].Value.Trim();
                inTable = false;
                continue;
            }
            
            // Tablo başlığı satırını atla
            if (line.Contains("| Tablo |") && line.Contains("| FK_Kolon |"))
            {
                inTable = true;
                continue;
            }
            
            // Ayırıcı satırı atla
            if (line.Trim().StartsWith("|") && line.Contains("---"))
            {
                continue;
            }
            
            // Bölüm sonu - "## Son" veya "## İlişki Özeti"
            if (line.StartsWith("## Son") || line.StartsWith("## 📊 İlişki Özeti"))
            {
                break;
            }
            
            // FK satırını parse et
            if (inTable && !string.IsNullOrEmpty(currentSchema) && line.Trim().StartsWith("|"))
            {
                var relation = ParseForeignKeyRow(line, currentSchema);
                if (relation != null)
                {
                    relations.Add(relation);
                }
            }
        }
        
        return relations;
    }
    
    /// <summary>
    /// FK tablo satırını parse eder
    /// | Tablo | Tablo_PK | FK_Kolon | Referans_Şema | Referans_Tablo | Referans_Kolon |
    /// </summary>
    private ForeignKeyRelation? ParseForeignKeyRow(string line, string sourceSchema)
    {
        var parts = line.Split('|')
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .ToList();
        
        // En az 6 kolon olmalı: Tablo, Tablo_PK, FK_Kolon, Referans_Şema, Referans_Tablo, Referans_Kolon
        if (parts.Count < 6)
            return null;
        
        var sourceTable = parts[0];
        // parts[1] = Tablo_PK (kullanılmıyor)
        var fkColumn = parts[2];
        var targetSchema = parts[3];
        var targetTable = parts[4];
        var targetColumn = parts[5];
        
        // Geçersiz satırları filtrele
        if (sourceTable == "Tablo" || sourceTable.Contains("---"))
            return null;
        
        return new ForeignKeyRelation
        {
            SourceSchema = sourceSchema,
            SourceTable = sourceTable,
            SourceColumn = fkColumn,
            TargetSchema = targetSchema,
            TargetTable = targetTable,
            TargetColumn = targetColumn
        };
    }
    
    /// <summary>
    /// FK ilişkilerini kolonlara uygular
    /// </summary>
    private void ApplyForeignKeyRelationsToColumns(SchemaParseResult result)
    {
        var columnLookup = result.Tables
            .SelectMany(t => t.Columns.Select(c => new { Table = t, Column = c }))
            .ToLookup(x => $"{x.Table.Schema}.{x.Table.Name}.{x.Column.Name}", StringComparer.OrdinalIgnoreCase);
        
        foreach (var fk in result.ForeignKeyRelations)
        {
            var key = $"{fk.SourceSchema}.{fk.SourceTable}.{fk.SourceColumn}";
            var matches = columnLookup[key].ToList();
            
            foreach (var match in matches)
            {
                // Kolonu FK olarak işaretle
                match.Column.IsForeignKey = true;
                match.Column.FkTable = fk.TargetFullName;
                match.Column.FkColumn = fk.TargetColumn;
                
                _logger.LogDebug("FK uygulandı: {Source} -> {Target}.{Column}",
                    key, fk.TargetFullName, fk.TargetColumn);
            }
        }
    }

    public async Task<SchemaParseResult> ParseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new SchemaParseResult
            {
                Errors = { $"Dosya bulunamadı: {filePath}" }
            };
        }

        var content = await File.ReadAllTextAsync(filePath);
        return ParseMarkdown(content);
    }

    public async Task<SchemaParseResult> ParseFromResourceAsync(string resourceName = "adventurerworks_schema.md")
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith(resourceName));

            if (string.IsNullOrEmpty(resourcePath))
            {
                // Dosya sisteminden oku
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var possiblePaths = new[]
                {
                    Path.Combine(basePath, "Common", "Resources", "Prompts", resourceName),
                    Path.Combine(basePath, "..", "..", "..", "Common", "Resources", "Prompts", resourceName),
                    Path.Combine(Directory.GetCurrentDirectory(), "AI.Application", "Common", "Resources", "Prompts", resourceName)
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return await ParseFromFileAsync(path);
                    }
                }

                return new SchemaParseResult
                {
                    Errors = { $"Resource bulunamadı: {resourceName}" }
                };
            }

            await using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                return new SchemaParseResult
                {
                    Errors = { $"Resource stream açılamadı: {resourcePath}" }
                };
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            return ParseMarkdown(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resource parse hatası: {ResourceName}", resourceName);
            return new SchemaParseResult
            {
                Errors = { $"Resource parse hatası: {ex.Message}" }
            };
        }
    }

    public string GenerateCypherImportScript(SchemaParseResult parseResult)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// ═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine("// AdventureWorks Schema Catalog - Neo4j Import Script");
        sb.AppendLine($"// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"// Total: {parseResult.TotalTableCount} tables, {parseResult.TotalColumnCount} columns");
        sb.AppendLine("// ═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Constraints ve Indexes
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("// CONSTRAINTS & INDEXES");
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine("CREATE CONSTRAINT schema_name IF NOT EXISTS FOR (s:Schema) REQUIRE s.name IS UNIQUE;");
        sb.AppendLine("CREATE CONSTRAINT table_fullname IF NOT EXISTS FOR (t:Table) REQUIRE t.fullName IS UNIQUE;");
        sb.AppendLine("CREATE INDEX table_name_idx IF NOT EXISTS FOR (t:Table) ON (t.name);");
        sb.AppendLine("CREATE INDEX table_schema_idx IF NOT EXISTS FOR (t:Table) ON (t.schema);");
        sb.AppendLine("CREATE INDEX column_name_idx IF NOT EXISTS FOR (c:Column) ON (c.name);");
        sb.AppendLine("CREATE INDEX column_alias_idx IF NOT EXISTS FOR (c:Column) ON (c.alias);");
        sb.AppendLine("CREATE INDEX column_tablename_idx IF NOT EXISTS FOR (c:Column) ON (c.tableName);");
        sb.AppendLine();

        // Full-text indexes
        sb.AppendLine("// Full-text search indexes");
        sb.AppendLine("CREATE FULLTEXT INDEX table_search IF NOT EXISTS FOR (t:Table) ON EACH [t.name, t.description];");
        sb.AppendLine("CREATE FULLTEXT INDEX column_search IF NOT EXISTS FOR (c:Column) ON EACH [c.name, c.alias, c.description];");
        sb.AppendLine();

        // Schema nodes
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("// SCHEMA NODES");
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine();
        
        foreach (var schema in parseResult.Schemas)
        {
            sb.AppendLine($"MERGE (s:Schema {{name: '{EscapeCypher(schema.Name)}'}})");
            sb.AppendLine($"SET s.description = '{EscapeCypher(schema.Description)}',");
            sb.AppendLine($"    s.tableCount = {schema.TableCount},");
            sb.AppendLine($"    s.updatedAt = datetime();");
            sb.AppendLine();
        }

        // Table ve Column nodes
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("// TABLE & COLUMN NODES");
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine();

        foreach (var table in parseResult.Tables)
        {
            sb.AppendLine($"// {table.FullName}");
            sb.AppendLine($"MATCH (s:Schema {{name: '{EscapeCypher(table.Schema)}'}})");
            sb.AppendLine($"MERGE (t:Table {{fullName: '{EscapeCypher(table.FullName)}'}})");
            sb.AppendLine($"SET t.name = '{EscapeCypher(table.Name)}',");
            sb.AppendLine($"    t.schema = '{EscapeCypher(table.Schema)}',");
            sb.AppendLine($"    t.description = '{EscapeCypher(table.Description)}',");
            sb.AppendLine($"    t.type = '{EscapeCypher(table.Type)}',");
            sb.AppendLine($"    t.columnCount = {table.Columns.Count},");
            sb.AppendLine($"    t.updatedAt = datetime()");
            sb.AppendLine("MERGE (s)-[:CONTAINS]->(t);");
            sb.AppendLine();

            // Columns
            foreach (var column in table.Columns)
            {
                var columnKey = $"{table.FullName}.{column.Name}";
                sb.AppendLine($"MATCH (t:Table {{fullName: '{EscapeCypher(table.FullName)}'}})");
                sb.AppendLine($"MERGE (c:Column {{key: '{EscapeCypher(columnKey)}'}})");
                sb.AppendLine($"SET c.name = '{EscapeCypher(column.Name)}',");
                sb.AppendLine($"    c.tableName = '{EscapeCypher(table.FullName)}',");
                sb.AppendLine($"    c.dataType = '{EscapeCypher(column.DataType)}',");
                sb.AppendLine($"    c.alias = '{EscapeCypher(column.Alias)}',");
                sb.AppendLine($"    c.description = '{EscapeCypher(column.Description)}',");
                sb.AppendLine($"    c.isPrimaryKey = {column.IsPrimaryKey.ToString().ToLower()},");
                sb.AppendLine($"    c.isForeignKey = {column.IsForeignKey.ToString().ToLower()}");
                
                if (column.IsForeignKey && !string.IsNullOrEmpty(column.FkTable))
                {
                    sb.AppendLine($"SET c.fkTable = '{EscapeCypher(column.FkTable)}',");
                    sb.AppendLine($"    c.fkColumn = '{EscapeCypher(column.FkColumn ?? column.Name)}'");
                }
                
                sb.AppendLine("MERGE (t)-[:HAS_COLUMN]->(c);");
                sb.AppendLine();
            }
        }

        // Foreign Key relationships from column definitions
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("// FOREIGN KEY RELATIONSHIPS (From Column Definitions)");
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine("MATCH (c:Column)");
        sb.AppendLine("WHERE c.isForeignKey = true AND c.fkTable IS NOT NULL");
        sb.AppendLine("MATCH (t:Table {fullName: c.fkTable})");
        sb.AppendLine("MERGE (c)-[:REFERENCES]->(t);");
        sb.AppendLine();

        // Explicit Foreign Key relationships from FK section
        if (parseResult.ForeignKeyRelations.Any())
        {
            sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
            sb.AppendLine($"// EXPLICIT FK RELATIONSHIPS ({parseResult.ForeignKeyRelations.Count} relations)");
            sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
            sb.AppendLine();
            
            foreach (var fk in parseResult.ForeignKeyRelations)
            {
                sb.AppendLine($"// {fk.SourceFullName}.{fk.SourceColumn} -> {fk.TargetFullName}.{fk.TargetColumn}");
                sb.AppendLine($"MATCH (src:Table {{fullName: '{EscapeCypher(fk.SourceFullName)}'}})");
                sb.AppendLine($"MATCH (tgt:Table {{fullName: '{EscapeCypher(fk.TargetFullName)}'}})");
                sb.AppendLine($"MERGE (src)-[j:JOINS_WITH {{");
                sb.AppendLine($"    via: '{EscapeCypher(fk.SourceColumn)}',");
                sb.AppendLine($"    fkColumn: '{EscapeCypher(fk.TargetColumn)}',");
                sb.AppendLine($"    sourceColumn: '{EscapeCypher(fk.SourceColumn)}',");
                sb.AppendLine($"    targetColumn: '{EscapeCypher(fk.TargetColumn)}'");
                sb.AppendLine($"}}]->(tgt);");
                sb.AppendLine();
            }
        }

        // JOINS_WITH relationships from column FK definitions
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine("// JOINS_WITH RELATIONSHIPS (From Column FK Definitions)");
        sb.AppendLine("// ─────────────────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine("MATCH (t1:Table)-[:HAS_COLUMN]->(c:Column)-[:REFERENCES]->(t2:Table)");
        sb.AppendLine("WHERE t1 <> t2");
        sb.AppendLine("MERGE (t1)-[j:JOINS_WITH]->(t2)");
        sb.AppendLine("ON CREATE SET j.via = c.name, j.fkColumn = c.fkColumn");
        sb.AppendLine("ON MATCH SET j.via = COALESCE(j.via, c.name), j.fkColumn = COALESCE(j.fkColumn, c.fkColumn);");
        sb.AppendLine();

        sb.AppendLine("// ═══════════════════════════════════════════════════════════════════════");
        sb.AppendLine("// IMPORT COMPLETE");
        sb.AppendLine("// ═══════════════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    #region Private Methods

    private List<string> SplitIntoTableBlocks(string content)
    {
        var blocks = new List<string>();
        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var currentBlock = new StringBuilder();
        var inTable = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            
            // ## seviye başlık (FK bölümü, İlişki Özeti vb.) tablo bloğunu sonlandırır
            if (inTable && line.TrimStart().StartsWith("## ") && !line.TrimStart().StartsWith("### "))
            {
                if (currentBlock.Length > 0)
                {
                    blocks.Add(currentBlock.ToString());
                    currentBlock.Clear();
                }
                inTable = false;
                continue;
            }

            // Yeni tablo başlığı?
            if (TableHeaderRegex.IsMatch(line))
            {
                // Önceki bloğu kaydet
                if (inTable && currentBlock.Length > 0)
                {
                    blocks.Add(currentBlock.ToString());
                    currentBlock.Clear();
                }
                inTable = true;
            }

            if (inTable)
            {
                currentBlock.AppendLine(line);
            }
        }

        // Son bloğu kaydet
        if (currentBlock.Length > 0)
        {
            blocks.Add(currentBlock.ToString());
        }

        return blocks;
    }

    private TableSchema? ParseTableBlock(string block, ref string currentSchema)
    {
        var lines = block.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
        if (lines.Count == 0) return null;

        // Tablo başlığını bul
        var headerLine = lines.FirstOrDefault(l => TableHeaderRegex.IsMatch(l));
        if (string.IsNullOrEmpty(headerLine)) return null;

        var headerMatch = TableHeaderRegex.Match(headerLine);
        var tableName = headerMatch.Groups[1].Value.Trim();
        var isView = headerLine.Contains("(Görünüm)");

        var table = new TableSchema
        {
            Name = tableName,
            Type = isView ? "View" : "Table"
        };

        // Özellik tablosunu parse et
        var columnSection = false;

        foreach (var line in lines)
        {
            // Özellik satırları
            var propMatch = PropertyTableRowRegex.Match(line);
            if (propMatch.Success)
            {
                var propName = propMatch.Groups[1].Value.Trim();
                var propValue = propMatch.Groups[2].Value.Trim();

                switch (propName)
                {
                    case "Şema Adı":
                        table.Schema = propValue;
                        currentSchema = propValue;
                        break;
                    case "Tablo Adı":
                        table.Name = propValue;
                        break;
                    case "Tablo Açıklaması":
                        table.Description = propValue;
                        break;
                }

                continue;
            }

            // Kolon tablosu başlığı
            if (line.Contains("COLUMN_NAME") && line.Contains("DATA_TYPE"))
            {
                columnSection = true;
                continue;
            }

            // Kolon tablosu ayırıcı satır (|---|---|---|---|)
            if (columnSection && line.Trim().StartsWith("|") && line.Contains("---"))
            {
                continue;
            }

            // Kolon satırı
            if (columnSection && line.Trim().StartsWith("|") && !line.Contains("COLUMN_NAME"))
            {
                var column = ParseColumnRow(line, table.FullName);
                if (column != null)
                {
                    table.Columns.Add(column);
                }
            }
        }

        // FullName oluştur
        if (!string.IsNullOrEmpty(table.Schema) && !string.IsNullOrEmpty(table.Name))
        {
            table.FullName = $"{table.Schema}.{table.Name}";
        }

        return table.Columns.Count > 0 ? table : null;
    }

    private Neo4jColumnInfo? ParseColumnRow(string line, string tableName)
    {
        // | COLUMN_NAME | DATA_TYPE | ALİAS | COLUMN_INFORMATION |
        var parts = line.Split('|')
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .ToList();

        if (parts.Count < 4) return null;

        var columnName = parts[0];
        var dataType = parts[1];
        var alias = parts[2];
        var description = parts[3];

        // Geçersiz satırları filtrele
        if (columnName == "COLUMN_NAME" || columnName.Contains("---"))
            return null;

        var column = new Neo4jColumnInfo
        {
            Name = columnName,
            TableName = tableName,
            DataType = dataType,
            Alias = alias,
            Description = CleanDescription(description)
        };

        // Primary Key kontrolü
        if (PrimaryKeyRegex.IsMatch(description))
        {
            column.IsPrimaryKey = true;
        }

        // Foreign Key kontrolü
        var fkMatch = ForeignKeyRegex.Match(description);
        if (fkMatch.Success)
        {
            column.IsForeignKey = true;
            var fkTableName = fkMatch.Groups[1].Value.Trim();
            
            // FK tablo adını schema ile birleştir
            column.FkTable = ResolveFkTableName(fkTableName, tableName);
            
            // FK kolon adını tahmin et (genellikle aynı isim veya ID ile biter)
            column.FkColumn = GuessFkColumn(columnName, fkTableName);
        }

        return column;
    }

    private string CleanDescription(string description)
    {
        // FK ve PK notlarını temizle
        var cleaned = PrimaryKeyRegex.Replace(description, "");
        cleaned = ForeignKeyRegex.Replace(cleaned, "");
        return cleaned.Trim().TrimEnd(',').Trim();
    }

    private string ResolveFkTableName(string fkTableName, string currentTable)
    {
        // Zaten schema içeriyorsa
        if (fkTableName.Contains('.'))
            return fkTableName;

        // Schema mapping
        var schemaMapping = new Dictionary<string, string>
        {
            // Person
            ["Person"] = "Person.Person",
            ["BusinessEntity"] = "Person.BusinessEntity",
            ["Address"] = "Person.Address",
            ["AddressType"] = "Person.AddressType",
            ["ContactType"] = "Person.ContactType",
            ["CountryRegion"] = "Person.CountryRegion",
            ["EmailAddress"] = "Person.EmailAddress",
            ["Password"] = "Person.Password",
            ["PersonPhone"] = "Person.PersonPhone",
            ["PhoneNumberType"] = "Person.PhoneNumberType",
            ["StateProvince"] = "Person.StateProvince",
            ["BusinessEntityAddress"] = "Person.BusinessEntityAddress",
            ["BusinessEntityContact"] = "Person.BusinessEntityContact",
            
            // Sales
            ["Customer"] = "Sales.Customer",
            ["Store"] = "Sales.Store",
            ["SalesTerritory"] = "Sales.SalesTerritory",
            ["SalesPerson"] = "Sales.SalesPerson",
            ["SalesOrderHeader"] = "Sales.SalesOrderHeader",
            ["SalesOrderDetail"] = "Sales.SalesOrderDetail",
            ["SalesReason"] = "Sales.SalesReason",
            ["SpecialOffer"] = "Sales.SpecialOffer",
            ["SpecialOfferProduct"] = "Sales.SpecialOfferProduct",
            ["Currency"] = "Sales.Currency",
            ["CurrencyRate"] = "Sales.CurrencyRate",
            ["CreditCard"] = "Sales.CreditCard",
            ["ShipMethod"] = "Purchasing.ShipMethod",
            
            // Production
            ["Product"] = "Production.Product",
            ["ProductCategory"] = "Production.ProductCategory",
            ["ProductSubcategory"] = "Production.ProductSubcategory",
            ["ProductModel"] = "Production.ProductModel",
            ["ProductDescription"] = "Production.ProductDescription",
            ["ProductPhoto"] = "Production.ProductPhoto",
            ["UnitMeasure"] = "Production.UnitMeasure",
            ["Location"] = "Production.Location",
            ["WorkOrder"] = "Production.WorkOrder",
            ["BillOfMaterials"] = "Production.BillOfMaterials",
            ["TransactionHistory"] = "Production.TransactionHistory",
            ["ProductInventory"] = "Production.ProductInventory",
            ["Culture"] = "Production.Culture",
            ["ScrapReason"] = "Production.ScrapReason",
            
            // Purchasing
            ["Vendor"] = "Purchasing.Vendor",
            ["ProductVendor"] = "Purchasing.ProductVendor",
            ["PurchaseOrderHeader"] = "Purchasing.PurchaseOrderHeader",
            ["PurchaseOrderDetail"] = "Purchasing.PurchaseOrderDetail",
            
            // HumanResources
            ["Employee"] = "HumanResources.Employee",
            ["Department"] = "HumanResources.Department",
            ["Shift"] = "HumanResources.Shift",
            ["JobCandidate"] = "HumanResources.JobCandidate"
        };

        if (schemaMapping.TryGetValue(fkTableName, out var fullName))
            return fullName;

        // Mevcut tablo ile aynı şemada olabilir
        var currentSchema = currentTable.Split('.')[0];
        return $"{currentSchema}.{fkTableName}";
    }

    private string GuessFkColumn(string columnName, string fkTableName)
    {
        // Genellikle FK kolon adı ile aynı
        // Örn: CustomerID -> CustomerID
        // Örn: PersonID -> BusinessEntityID (Person tablosunda)
        
        if (fkTableName == "Person" && columnName == "PersonID")
            return "BusinessEntityID";
            
        if (fkTableName == "BusinessEntity")
            return "BusinessEntityID";
            
        // Varsayılan: Aynı kolon adı veya ID
        if (columnName.EndsWith("ID"))
            return columnName;
            
        return $"{fkTableName}ID";
    }

    private string EscapeCypher(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
            
        return value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", "")
            .Replace("\n", " ");
    }

    #endregion
}
