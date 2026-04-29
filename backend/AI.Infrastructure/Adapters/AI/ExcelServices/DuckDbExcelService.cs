using ExcelColumnInfo = AI.Application.DTOs.ExcelAnalysis.ColumnInfo;

using System.Diagnostics;
using System.Security;
using System.Text;
using AI.Application.Common.Helpers;
using AI.Application.Common.Telemetry;
using AI.Application.DTOs.ExcelAnalysis;
using AI.Application.Ports.Secondary.Services.Report;
using DuckDB.NET.Data;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.ExcelServices;

/// <summary>
/// DuckDB tabanlı Excel/CSV analiz servisi
/// Büyük dosyalarda (100K+ satır) verimli SQL sorguları çalıştırır
/// </summary>
public class DuckDbExcelService : IExcelAnalysisService
{
    private readonly ILogger<DuckDbExcelService> _logger;
    private static readonly string[] SupportedExtensions = { ".xlsx", ".xls", ".csv" };
    private const int MaxSampleRows = 5;
    private const int MaxResultRows = 1000;
    private const int QueryTimeoutSeconds = 30;

    public DuckDbExcelService(ILogger<DuckDbExcelService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Public Methods

    public IEnumerable<string> GetSupportedExtensions() => SupportedExtensions;

    public bool IsSupported(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<ExcelSchemaResult> GetSchemaAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.DocumentProcessing.StartActivity("DuckDB_GetSchema");
        activity?.SetTag("file.name", fileName);

        var result = new ExcelSchemaResult();
        var tempPath = string.Empty;

        try
        {
            // Geçici dosyaya kaydet
            tempPath = await SaveToTempFileAsync(fileStream, fileName, cancellationToken);
            var tableName = TurkishEncodingHelper.SanitizeTableName(fileName);
            result.TableName = tableName;

            await using var connection = new DuckDBConnection("DataSource=:memory:");
            await connection.OpenAsync(cancellationToken);

            // Extension'ları yükle
            await LoadExtensionsAsync(connection, fileName, cancellationToken);

            var readFunction = GetReadFunction(fileName, tempPath);

            // Sütun bilgilerini al
            result.Columns = await GetColumnsAsync(connection, readFunction, cancellationToken);

            // Satır sayısını al
            result.RowCount = await GetRowCountAsync(connection, readFunction, cancellationToken);

            // Örnek satırları al
            result.SampleRows = await GetSampleRowsAsync(connection, readFunction, MaxSampleRows, cancellationToken);

            result.Success = true;
            activity?.SetTag("columns.count", result.Columns.Count);
            activity?.SetTag("rows.count", result.RowCount);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Excel şeması çıkarıldı - Dosya: {FileName}, Tablo: {TableName}, Sütun: {ColumnCount}, Satır: {RowCount}",
                fileName, tableName, result.Columns.Count, result.RowCount);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Şema çıkarma hatası: {ex.Message}";
            _logger.LogError(ex, "DuckDB şema çıkarma hatası - Dosya: {FileName}", fileName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
        finally
        {
            CleanupTempFile(tempPath);
        }

        return result;
    }

    public async Task<ExcelQueryResult> ExecuteQueryAsync(Stream fileStream, string fileName, string sqlQuery, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.DocumentProcessing.StartActivity("DuckDB_ExecuteQuery");
        activity?.SetTag("file.name", fileName);

        var result = new ExcelQueryResult();
        var tempPath = string.Empty;
        var sw = Stopwatch.StartNew();

        try
        {
            // SQL güvenlik kontrolü
            ValidateSql(sqlQuery);

            // Geçici dosyaya kaydet
            tempPath = await SaveToTempFileAsync(fileStream, fileName, cancellationToken);
            var tableName = TurkishEncodingHelper.SanitizeTableName(fileName);

            await using var connection = new DuckDBConnection("DataSource=:memory:");
            await connection.OpenAsync(cancellationToken);

            // Extension'ları yükle
            await LoadExtensionsAsync(connection, fileName, cancellationToken);

            var readFunction = GetReadFunction(fileName, tempPath);

            // SQL'deki tablo adını gerçek read fonksiyonu ile değiştir
            var executableSql = ReplacePlaceholders(sqlQuery, tableName, readFunction);
            result.ExecutedSql = executableSql;

            // Sorguyu çalıştır
            await using var command = connection.CreateCommand();
            command.CommandText = executableSql;
            command.CommandTimeout = QueryTimeoutSeconds;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Sütun adlarını al
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }
            result.Columns = columns.ToArray();

            // Sonuçları oku (limit ile)
            var data = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync(cancellationToken) && data.Count < MaxResultRows)
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columns[i]] = value;
                }
                data.Add(row);
            }

            result.Data = data;
            result.RowCount = data.Count;
            result.Success = true;

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;

            activity?.SetTag("result.rows", result.RowCount);
            activity?.SetTag("execution.ms", result.ExecutionTimeMs);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "DuckDB sorgusu çalıştırıldı - Dosya: {FileName}, Sonuç: {RowCount} satır, Süre: {ExecutionTime}ms",
                fileName, result.RowCount, result.ExecutionTimeMs);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.ErrorMessage = $"Sorgu hatası: {ex.Message}";
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            _logger.LogError(ex, "DuckDB sorgu hatası - Dosya: {FileName}, SQL: {Sql}", fileName, sqlQuery);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
        finally
        {
            CleanupTempFile(tempPath);
        }

        return result;
    }

    public async Task<ExcelAnalysisResult> AnalyzeAsync(Stream fileStream, string fileName, string userQuery, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.DocumentProcessing.StartActivity("DuckDB_Analyze");
        activity?.SetTag("file.name", fileName);
        activity?.SetTag("user.query", userQuery);

        var result = new ExcelAnalysisResult();

        try
        {
            // Stream'i kopyala (iki kez okumak için)
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);

            // 1. Şemayı al
            memoryStream.Position = 0;
            result.Schema = await GetSchemaAsync(memoryStream, fileName, cancellationToken);

            if (!result.Schema.Success)
            {
                result.Success = false;
                result.ErrorMessage = result.Schema.ErrorMessage;
                return result;
            }

            // NOT: SQL üretimi AIChatUseCase tarafından LLM ile yapılacak
            // Bu method şemayı hazırlar, SQL üretimi dışarıda yapılır

            result.Success = true;
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Analiz hatası: {ex.Message}";
            _logger.LogError(ex, "DuckDB analiz hatası - Dosya: {FileName}", fileName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }

        return result;
    }

    #endregion

    #region Private Methods

    private async Task<string> SaveToTempFileAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var tempPath = Path.Combine(Path.GetTempPath(), $"duckdb_{Guid.NewGuid()}{extension}");

        await using var fileStream = File.Create(tempPath);
        stream.Position = 0;
        await stream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogDebug("Geçici dosya oluşturuldu: {TempPath}", tempPath);
        return tempPath;
    }

    private void CleanupTempFile(string tempPath)
    {
        if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
        {
            try
            {
                File.Delete(tempPath);
                _logger.LogDebug("Geçici dosya silindi: {TempPath}", tempPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geçici dosya silinemedi: {TempPath}", tempPath);
            }
        }
    }

    private async Task LoadExtensionsAsync(DuckDBConnection connection, string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        await using var command = connection.CreateCommand();

        // Excel dosyaları için spatial extension gerekli
        if (extension is ".xlsx" or ".xls")
        {
            command.CommandText = "INSTALL spatial; LOAD spatial;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("DuckDB spatial extension yüklendi");
        }
    }

    private string GetReadFunction(string fileName, string filePath)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var escapedPath = filePath.Replace("\\", "/").Replace("'", "''");

        return extension switch
        {
            ".xlsx" or ".xls" => $"st_read('{escapedPath}')",
            ".csv" => $"read_csv('{escapedPath}', auto_detect=true, header=true)",
            _ => throw new NotSupportedException($"Desteklenmeyen dosya formatı: {extension}")
        };
    }

    private async Task<List<ExcelColumnInfo>> GetColumnsAsync(DuckDBConnection connection, string readFunction, CancellationToken cancellationToken)
    {
        var columns = new List<ExcelColumnInfo>();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DESCRIBE SELECT * FROM {readFunction}";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ExcelColumnInfo
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES"
            });
        }

        return columns;
    }

    private async Task<long> GetRowCountAsync(DuckDBConnection connection, string readFunction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {readFunction}";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private async Task<List<Dictionary<string, object?>>> GetSampleRowsAsync(DuckDBConnection connection, string readFunction, int limit, CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {readFunction} LIMIT {limit}";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columns[i]] = value;
            }
            rows.Add(row);
        }

        return rows;
    }

    private void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL sorgusu boş olamaz.");

        var upperSql = sql.ToUpperInvariant();

        // Sadece SELECT izin ver
        if (!upperSql.TrimStart().StartsWith("SELECT"))
            throw new SecurityException("Sadece SELECT sorguları desteklenir.");

        // Tehlikeli komutları engelle
        var forbidden = new[] { "DROP", "DELETE", "INSERT", "UPDATE", "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE" };
        foreach (var keyword in forbidden)
        {
            if (upperSql.Contains(keyword))
                throw new SecurityException($"Tehlikeli SQL komutu tespit edildi: {keyword}");
        }
    }

    private string ReplacePlaceholders(string sql, string tableName, string readFunction)
    {
        // Olası tablo adı placeholder'larını değiştir
        var result = sql
            .Replace($"FROM {tableName}", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
            .Replace($"FROM \"{tableName}\"", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
            .Replace("FROM data", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
            .Replace("FROM DATA", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
            .Replace("FROM tablo", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
            .Replace("FROM table", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase);

        return result;
    }

    #endregion

    #region HTML Generation

    /// <summary>
    /// Sorgu sonucundan HTML tablo oluşturur
    /// </summary>
    public static string GenerateHtmlTable(ExcelQueryResult result)
    {
        if (!result.Success || result.Data.Count == 0)
            return "<p>Sonuç bulunamadı.</p>";

        var sb = new StringBuilder();
        sb.AppendLine("<div class=\"table-responsive\">");
        sb.AppendLine("<table class=\"table table-striped table-hover\">");

        // Header
        sb.AppendLine("<thead class=\"table-dark\">");
        sb.AppendLine("<tr>");
        foreach (var column in result.Columns)
        {
            sb.AppendLine($"<th>{System.Web.HttpUtility.HtmlEncode(column)}</th>");
        }
        sb.AppendLine("</tr>");
        sb.AppendLine("</thead>");

        // Body
        sb.AppendLine("<tbody>");
        foreach (var row in result.Data)
        {
            sb.AppendLine("<tr>");
            foreach (var column in result.Columns)
            {
                var value = row.TryGetValue(column, out var v) ? v?.ToString() ?? "" : "";
                sb.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(value)}</td>");
            }
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody>");

        sb.AppendLine("</table>");
        sb.AppendLine("</div>");

        // Footer
        sb.AppendLine($"<p class=\"text-muted\"><small>Toplam {result.RowCount} satır, {result.ExecutionTimeMs}ms</small></p>");

        return sb.ToString();
    }

    #endregion
}
