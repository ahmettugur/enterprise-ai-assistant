
using AI.Application.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using System.Text.RegularExpressions;
using AI.Application.Ports.Secondary.Services.Database;

namespace AI.Infrastructure.Adapters.AI.Agents.SqlAgents;

/// <summary>
/// SQL sorgularını optimize eden agent implementasyonu.
/// LLM kullanarak performans iyileştirmeleri önerir ve uygular.
/// </summary>
public class SqlOptimizationAgent : ISqlOptimizationAgent
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly ILogger<SqlOptimizationAgent> _logger;
    
    private const string PromptFileName = "sql_optimization_agent_prompt.md";

    private static readonly Regex JsonBlockRegex = new(@"```json\s*(.*?)\s*```", 
        RegexOptions.Singleline | RegexOptions.Compiled);

    public SqlOptimizationAgent(
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ILogger<SqlOptimizationAgent> logger)
    {
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SqlOptimizationResult> OptimizeAsync(
        string sql,
        string databaseType,
        string? schemaInfo = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SQL optimization başlatıldı - DatabaseType: {DatabaseType}", databaseType);

        try
        {
            // 1. Hızlı ön kontroller (LLM çağrısı öncesi)
            var quickOptimizations = ApplyQuickOptimizations(sql, databaseType);
            if (quickOptimizations.HasChanges)
            {
                sql = quickOptimizations.OptimizedSql;
                _logger.LogDebug("Hızlı optimizasyonlar uygulandı: {Count} değişiklik", quickOptimizations.Changes.Count);
            }

            // 2. LLM ile derin optimizasyon
            var prompt = BuildOptimizationPrompt(sql, databaseType, schemaInfo);
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.1f,
                MaxTokens = 2000
            };

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory, settings, _kernel, cancellationToken).ConfigureAwait(false);

            var responseText = response.Content ?? "";
            _logger.LogDebug("LLM optimization yanıtı: {Response}", responseText);

            // 3. Yanıtı parse et
            var optimizationResponse = ParseOptimizationResponse(responseText);

            // 4. Quick optimizations ve LLM optimizations'ı birleştir
            var allOptimizations = quickOptimizations.Changes.ToList();
            allOptimizations.AddRange(optimizationResponse.Optimizations);

            if (optimizationResponse.IsOptimized || quickOptimizations.HasChanges)
            {
                var finalSql = optimizationResponse.IsOptimized 
                    ? optimizationResponse.OptimizedSql 
                    : sql;

                _logger.LogInformation("SQL optimize edildi - {Count} optimizasyon uygulandı", allOptimizations.Count);
                
                return SqlOptimizationResult.Optimized(
                    sql,
                    finalSql,
                    allOptimizations,
                    optimizationResponse.EstimatedImprovementPercent,
                    optimizationResponse.Explanation
                );
            }

            _logger.LogInformation("SQL zaten optimize durumda");
            return SqlOptimizationResult.NoOptimization(sql, optimizationResponse.Explanation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL optimization sırasında hata oluştu");
            // Hata durumunda orijinal SQL'i döndür (fail-safe)
            return SqlOptimizationResult.NoOptimization(sql, "Optimization atlandı: " + ex.Message);
        }
    }

    private QuickOptimizationResult ApplyQuickOptimizations(string sql, string databaseType)
    {
        var changes = new List<SqlOptimization>();
        var optimizedSql = sql;

        // 1. SELECT * kontrolü
        if (Regex.IsMatch(optimizedSql, @"SELECT\s+\*\s+FROM", RegexOptions.IgnoreCase))
        {
            changes.Add(new SqlOptimization
            {
                Type = SqlOptimizationType.SelectColumnSpecification,
                Description = "SELECT * kullanımı tespit edildi. Performans için belirli kolonlar seçilmeli.",
                Before = "SELECT *",
                After = "Belirli kolonlar önerilir",
                Impact = "Gereksiz veri transferini azaltır"
            });
            // Not: Otomatik düzeltme yapmıyoruz çünkü kolon isimlerini bilmiyoruz
        }

        // 2. UNION vs UNION ALL
        var unionPattern = @"\bUNION\s+(?!ALL\b)";
        if (Regex.IsMatch(optimizedSql, unionPattern, RegexOptions.IgnoreCase))
        {
            // Sadece uyarı ekle, otomatik değiştirme yapma (çünkü duplicate kontrolü gerekebilir)
            changes.Add(new SqlOptimization
            {
                Type = SqlOptimizationType.UnionToUnionAll,
                Description = "UNION kullanımı tespit edildi. Duplicate kontrolü gerekmiyorsa UNION ALL daha hızlıdır.",
                Before = "UNION",
                After = "UNION ALL (eğer uygunsa)",
                Impact = "Duplicate elimination overhead'ini kaldırır"
            });
        }

        // 3. Oracle için ROWNUM pagination optimizasyonu
        if (databaseType.Equals("oracle", StringComparison.OrdinalIgnoreCase))
        {
            // Büyük sonuç seti için ROWNUM/FETCH FIRST kontrolü
            if (!Regex.IsMatch(optimizedSql, @"\bROWNUM\b|\bFETCH\s+FIRST\b|\bOFFSET\b", RegexOptions.IgnoreCase))
            {
                changes.Add(new SqlOptimization
                {
                    Type = SqlOptimizationType.Pagination,
                    Description = "Pagination limiti yok. Büyük sonuç setleri için FETCH FIRST veya ROWNUM kullanımı önerilir.",
                    Before = "Limit yok",
                    After = "FETCH FIRST n ROWS ONLY veya WHERE ROWNUM <= n",
                    Impact = "Bellek kullanımını ve sorgu süresini azaltır"
                });
            }
        }

        // 4. SQL Server için TOP kontrolü
        if (databaseType.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            if (!Regex.IsMatch(optimizedSql, @"\bTOP\s+\d+\b|\bOFFSET\b", RegexOptions.IgnoreCase))
            {
                changes.Add(new SqlOptimization
                {
                    Type = SqlOptimizationType.Pagination,
                    Description = "Pagination limiti yok. Büyük sonuç setleri için TOP veya OFFSET-FETCH kullanımı önerilir.",
                    Before = "Limit yok",
                    After = "SELECT TOP n ... veya OFFSET x ROWS FETCH NEXT y ROWS ONLY",
                    Impact = "Bellek kullanımını ve sorgu süresini azaltır"
                });
            }
        }

        // 5. Gereksiz DISTINCT kontrolü
        if (Regex.IsMatch(optimizedSql, @"\bSELECT\s+DISTINCT\b", RegexOptions.IgnoreCase))
        {
            // Eğer PRIMARY KEY veya UNIQUE constraint varsa DISTINCT gereksiz olabilir
            changes.Add(new SqlOptimization
            {
                Type = SqlOptimizationType.DistinctOptimization,
                Description = "DISTINCT kullanımı tespit edildi. Eğer sonuçlar zaten benzersizse, DISTINCT'i kaldırmak performansı artırır.",
                Before = "SELECT DISTINCT",
                After = "SELECT (eğer uygunsa)",
                Impact = "Sort ve comparison overhead'ini kaldırır"
            });
        }

        // 6. ORDER BY olmadan DISTINCT (potansiyel sorun)
        if (Regex.IsMatch(optimizedSql, @"\bDISTINCT\b", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(optimizedSql, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase))
        {
            changes.Add(new SqlOptimization
            {
                Type = SqlOptimizationType.OrderByOptimization,
                Description = "DISTINCT var ama ORDER BY yok. Sonuç sırası önemliyse ORDER BY eklenmelidir.",
                Before = "DISTINCT without ORDER BY",
                After = "DISTINCT ... ORDER BY column",
                Impact = "Tutarlı sonuç sırası sağlar"
            });
        }

        return new QuickOptimizationResult
        {
            HasChanges = changes.Count > 0,
            OptimizedSql = optimizedSql,
            Changes = changes
        };
    }

    private string BuildOptimizationPrompt(string sql, string databaseType, string? schemaInfo)
    {
        var schemaSection = string.IsNullOrEmpty(schemaInfo) 
            ? "" 
            : $"\n\n## Veritabanı Schema Bilgisi\n{schemaInfo}";

        var dbType = databaseType.ToUpperInvariant();
        
        // MD dosyasından prompt template'i oku
        var promptTemplate = Helper.ReadFileContent("Common/Resources/Prompts", PromptFileName);
        
        // Placeholder'ları değiştir
        var prompt = promptTemplate
            .Replace("{{DATABASE_TYPE}}", dbType)
            .Replace("{{SQL_QUERY}}", sql)
            .Replace("{{SCHEMA_SECTION}}", schemaSection);

        return prompt;
    }

    private OptimizationResponse ParseOptimizationResponse(string responseText)
    {
        try
        {
            var jsonStr = responseText;
            var match = JsonBlockRegex.Match(responseText);
            if (match.Success)
            {
                jsonStr = match.Groups[1].Value;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new SafeEnumConverter<SqlOptimizationType>() }
            };

            var response = JsonSerializer.Deserialize<OptimizationResponse>(jsonStr, options);
            return response ?? new OptimizationResponse { IsOptimized = false };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Optimization yanıtı parse edilemedi");
            return new OptimizationResponse { IsOptimized = false };
        }
    }

    /// <summary>
    /// Bilinmeyen enum değerlerini 'Other' olarak parse eden converter
    /// </summary>
    private class SafeEnumConverter<T> : System.Text.Json.Serialization.JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (Enum.TryParse<T>(stringValue, ignoreCase: true, out var result))
                {
                    return result;
                }
                // Bilinmeyen değerler için 'Other' döndür
                if (Enum.TryParse<T>("Other", ignoreCase: true, out var otherResult))
                {
                    return otherResult;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                var intValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(T), intValue))
                {
                    return (T)(object)intValue;
                }
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private class QuickOptimizationResult
    {
        public bool HasChanges { get; init; }
        public string OptimizedSql { get; init; } = string.Empty;
        public List<SqlOptimization> Changes { get; init; } = [];
    }

    private class OptimizationResponse
    {
        public bool IsOptimized { get; set; }
        public string OptimizedSql { get; set; } = string.Empty;
        public List<SqlOptimization> Optimizations { get; set; } = [];
        public int? EstimatedImprovementPercent { get; set; }
        public string? Explanation { get; set; }
    }
}
