using AI.Application.DTOs.DatabaseSchema;
using AI.Application.DTOs.Neo4j;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.Neo4j;

/// <summary>
/// SQL Server veritabanından şema bilgisini otomatik olarak çeken servis
/// </summary>
public class DatabaseSchemaReader : IDatabaseSchemaReader
{
    private readonly ILogger<DatabaseSchemaReader> _logger;

    public DatabaseSchemaReader(ILogger<DatabaseSchemaReader> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SchemaParseResult> ReadSchemaFromDatabaseAsync(
        string connectionString,
        string sourceName,
        AliasConfiguration? aliasConfig = null)
    {
        _logger.LogInformation("Veritabanından şema okunuyor: {Source}", sourceName);

        var result = new SchemaParseResult();

        try
        {
            // 1. Tabloları çek
            var tables = await GetTablesInternalAsync(connectionString, sourceName, aliasConfig);
            result.Tables.AddRange(tables);

            // 2. Kolonları çek ve tablolara ekle
            var columns = await GetAllColumnsAsync(connectionString, sourceName, aliasConfig);
            foreach (var table in result.Tables)
            {
                var tableColumns = columns.Where(c => c.TableName == table.FullName).ToList();
                table.Columns.AddRange(tableColumns);
            }

            // 3. Foreign Key ilişkilerini çek
            var foreignKeys = await GetForeignKeysAsync(connectionString);
            result.ForeignKeyRelations.AddRange(foreignKeys);

            // 4. Şemaları grupla
            var schemas = result.Tables
                .GroupBy(t => t.Schema)
                .Select(g => new SchemaInfo
                {
                    Name = g.Key,
                    TableCount = g.Count(),
                    Description = $"{g.Key} şeması"
                })
                .ToList();
            result.Schemas.AddRange(schemas);

            _logger.LogInformation(
                "Şema başarıyla okundu: {Source} - {TableCount} tablo, {ColumnCount} kolon, {FkCount} FK",
                sourceName,
                result.Tables.Count,
                result.Tables.Sum(t => t.Columns.Count),
                result.ForeignKeyRelations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanından şema okuma hatası: {Source}", sourceName);
            throw;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TableInfo>> GetTablesAsync(string connectionString, string sourceName)
    {
        var tables = await GetTablesInternalAsync(connectionString, sourceName, null);
        return tables.Select(t => new TableInfo
        {
            Name = t.Name,
            FullName = t.FullName,
            Schema = t.Schema,
            Description = t.Description,
            Type = t.Type
        });
    }

    private async Task<List<TableSchema>> GetTablesInternalAsync(
        string connectionString,
        string sourceName,
        AliasConfiguration? aliasConfig)
    {
        var tables = new List<TableSchema>();

        const string query = @"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName,
                s.name + '.' + t.name AS FullName,
                CASE 
                    WHEN t.type = 'U' THEN 'Table' 
                    WHEN t.type = 'V' THEN 'View' 
                    ELSE 'Unknown'
                END AS TableType,
                CAST(ep.value AS NVARCHAR(MAX)) AS Description
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            LEFT JOIN sys.extended_properties ep 
                ON ep.major_id = t.object_id 
                AND ep.minor_id = 0 
                AND ep.name = 'MS_Description'
            
            UNION ALL
            
            SELECT 
                s.name AS SchemaName,
                v.name AS TableName,
                s.name + '.' + v.name AS FullName,
                'View' AS TableType,
                CAST(ep.value AS NVARCHAR(MAX)) AS Description
            FROM sys.views v
            INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
            LEFT JOIN sys.extended_properties ep 
                ON ep.major_id = v.object_id 
                AND ep.minor_id = 0 
                AND ep.name = 'MS_Description'
            
            ORDER BY SchemaName, TableName";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fullName = reader.GetString(reader.GetOrdinal("FullName"));
            var dbDescription = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null
                : reader.GetString(reader.GetOrdinal("Description"));

            // Alias config'den Türkçe bilgileri al
            var alias = aliasConfig?.Tables.GetValueOrDefault(fullName);

            tables.Add(new TableSchema
            {
                Name = reader.GetString(reader.GetOrdinal("TableName")),
                FullName = fullName,
                Schema = reader.GetString(reader.GetOrdinal("SchemaName")),
                Type = reader.GetString(reader.GetOrdinal("TableType")),
                Description = alias?.Description ?? dbDescription ?? string.Empty,
                Source = sourceName
            });
        }

        _logger.LogDebug("Toplam {Count} tablo/view bulundu: {Source}", tables.Count, sourceName);
        return tables;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ColumnInfo>> GetColumnsAsync(string connectionString, string tableName)
    {
        return await GetColumnsForTableAsync(connectionString, tableName, null);
    }

    private async Task<List<ColumnInfo>> GetAllColumnsAsync(
        string connectionString,
        string sourceName,
        AliasConfiguration? aliasConfig)
    {
        var columns = new List<ColumnInfo>();

        const string query = @"
            SELECT 
                s.name AS SchemaName,
                t.name AS TableName,
                s.name + '.' + t.name AS FullTableName,
                c.name AS ColumnName,
                ty.name AS DataType,
                c.max_length AS MaxLength,
                c.precision AS Precision,
                c.scale AS Scale,
                c.is_nullable AS IsNullable,
                CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                CASE WHEN fkc.parent_column_id IS NOT NULL THEN 1 ELSE 0 END AS IsForeignKey,
                CASE 
                    WHEN fkc.parent_column_id IS NOT NULL THEN 
                        OBJECT_SCHEMA_NAME(fk.referenced_object_id) + '.' + OBJECT_NAME(fk.referenced_object_id)
                    ELSE NULL 
                END AS FkTable,
                CASE 
                    WHEN fkc.parent_column_id IS NOT NULL THEN 
                        COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)
                    ELSE NULL 
                END AS FkColumn,
                CAST(ep.value AS NVARCHAR(MAX)) AS Description
            FROM sys.columns c
            INNER JOIN sys.tables t ON c.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.index_columns ic
                INNER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                WHERE i.is_primary_key = 1
            ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
            LEFT JOIN sys.foreign_key_columns fkc 
                ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            LEFT JOIN sys.foreign_keys fk ON fkc.constraint_object_id = fk.object_id
            LEFT JOIN sys.extended_properties ep 
                ON ep.major_id = c.object_id 
                AND ep.minor_id = c.column_id 
                AND ep.name = 'MS_Description'
            ORDER BY s.name, t.name, c.column_id";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        command.CommandTimeout = 120; // 2 dakika timeout

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fullTableName = reader.GetString(reader.GetOrdinal("FullTableName"));
            var columnName = reader.GetString(reader.GetOrdinal("ColumnName"));
            var columnKey = $"{fullTableName}.{columnName}";
            var dbDescription = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null
                : reader.GetString(reader.GetOrdinal("Description"));

            // Alias config'den Türkçe bilgileri al
            var alias = aliasConfig?.Columns.GetValueOrDefault(columnKey);

            var dataType = FormatDataType(
                reader.GetString(reader.GetOrdinal("DataType")),
                reader.GetInt16(reader.GetOrdinal("MaxLength")),
                reader.GetByte(reader.GetOrdinal("Precision")),
                reader.GetByte(reader.GetOrdinal("Scale")));

            columns.Add(new ColumnInfo
            {
                Name = columnName,
                TableName = fullTableName,
                DataType = dataType,
                Alias = alias?.Alias ?? columnName,
                Description = alias?.Description ?? dbDescription ?? string.Empty,
                IsPrimaryKey = reader.GetInt32(reader.GetOrdinal("IsPrimaryKey")) == 1,
                IsForeignKey = reader.GetInt32(reader.GetOrdinal("IsForeignKey")) == 1,
                FkTable = reader.IsDBNull(reader.GetOrdinal("FkTable"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("FkTable")),
                FkColumn = reader.IsDBNull(reader.GetOrdinal("FkColumn"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("FkColumn"))
            });
        }

        _logger.LogDebug("Toplam {Count} kolon bulundu: {Source}", columns.Count, sourceName);
        return columns;
    }

    private async Task<List<ColumnInfo>> GetColumnsForTableAsync(
        string connectionString,
        string tableName,
        AliasConfiguration? aliasConfig)
    {
        var parts = tableName.Split('.');
        if (parts.Length != 2)
        {
            _logger.LogWarning("Geçersiz tablo adı formatı: {TableName}", tableName);
            return new List<ColumnInfo>();
        }

        var schemaName = parts[0];
        var tableNameOnly = parts[1];

        var allColumns = await GetAllColumnsAsync(connectionString, "temp", aliasConfig);
        return allColumns.Where(c => c.TableName == tableName).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ForeignKeyRelation>> GetForeignKeysAsync(string connectionString)
    {
        var foreignKeys = new List<ForeignKeyRelation>();

        const string query = """
            SELECT 
                OBJECT_SCHEMA_NAME(fk.parent_object_id) AS SourceSchema,
                OBJECT_NAME(fk.parent_object_id) AS SourceTable,
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS SourceColumn,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS TargetSchema,
                OBJECT_NAME(fk.referenced_object_id) AS TargetTable,
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS TargetColumn,
                fk.name AS FkName
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc 
                ON fk.object_id = fkc.constraint_object_id
            ORDER BY 1, 2, 3
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            foreignKeys.Add(new ForeignKeyRelation
            {
                SourceSchema = reader.GetString(reader.GetOrdinal("SourceSchema")),
                SourceTable = reader.GetString(reader.GetOrdinal("SourceTable")),
                SourceColumn = reader.GetString(reader.GetOrdinal("SourceColumn")),
                TargetSchema = reader.GetString(reader.GetOrdinal("TargetSchema")),
                TargetTable = reader.GetString(reader.GetOrdinal("TargetTable")),
                TargetColumn = reader.GetString(reader.GetOrdinal("TargetColumn")),
                FkName = reader.GetString(reader.GetOrdinal("FkName"))
            });
        }

        _logger.LogDebug("Toplam {Count} FK ilişkisi bulundu", foreignKeys.Count);
        return foreignKeys;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Veritabanı bağlantı testi başarısız");
            return false;
        }
    }

    /// <summary>
    /// Data type'ı formatlar (örn: nvarchar(50), decimal(18,2))
    /// </summary>
    private static string FormatDataType(string baseType, short maxLength, byte precision, byte scale)
    {
        return baseType.ToLower() switch
        {
            "nvarchar" or "nchar" when maxLength == -1 => $"{baseType}(MAX)",
            "nvarchar" or "nchar" => $"{baseType}({maxLength / 2})",
            "varchar" or "char" or "varbinary" when maxLength == -1 => $"{baseType}(MAX)",
            "varchar" or "char" or "varbinary" => $"{baseType}({maxLength})",
            "decimal" or "numeric" => $"{baseType}({precision},{scale})",
            _ => baseType
        };
    }
}
