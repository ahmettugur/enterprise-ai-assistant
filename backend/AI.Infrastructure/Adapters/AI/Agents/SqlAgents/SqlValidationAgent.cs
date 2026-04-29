
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
/// SQL sorgularını doğrulayan agent implementasyonu.
/// LLM kullanarak syntax, tablo/kolon isimleri ve güvenlik kontrolü yapar.
/// </summary>
public class SqlValidationAgent : ISqlValidationAgent
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly ILogger<SqlValidationAgent> _logger;
    
    private const string PromptFileName = "sql_validation_agent_prompt.md";

    private static readonly Regex JsonBlockRegex = new(@"```json\s*(.*?)\s*```", 
        RegexOptions.Singleline | RegexOptions.Compiled);

    // Tehlikeli SQL pattern'leri
    private static readonly string[] DangerousPatterns =
    [
        @"\bDROP\s+TABLE\b",
        @"\bDROP\s+DATABASE\b",
        @"\bTRUNCATE\s+TABLE\b",
        @"\bDELETE\s+FROM\b(?!\s+.*\bWHERE\b)",  // WHERE olmadan DELETE
        @"\bUPDATE\b(?!\s+.*\bWHERE\b)",          // WHERE olmadan UPDATE
        @"\bALTER\s+TABLE\b",
        @"\bCREATE\s+TABLE\b",
        @"\bINSERT\s+INTO\b",
        @"\bEXEC\s*\(",
        @"\bEXECUTE\s+",
        @"\bxp_cmdshell\b",
        @"\bsp_executesql\b",
        @";\s*--",                                 // Comment injection
        @"'\s*OR\s+'1'\s*=\s*'1",                  // SQL injection pattern
        @"UNION\s+SELECT\s+NULL"                   // Union-based injection
    ];

    public SqlValidationAgent(
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ILogger<SqlValidationAgent> logger)
    {
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SqlValidationResult> ValidateAsync(
        string sql,
        string databaseType,
        string? schemaInfo = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SQL validation başlatıldı - DatabaseType: {DatabaseType}", databaseType);

        try
        {
            // 1. Önce güvenlik kontrolü yap (LLM çağrısı öncesi)
            var securityIssues = CheckSecurityPatterns(sql);
            if (securityIssues.Any(s => s.Severity == SqlSecuritySeverity.Critical))
            {
                _logger.LogWarning("SQL güvenlik kontrolünden geçemedi - Kritik güvenlik sorunu tespit edildi");
                return SqlValidationResult.Invalid(
                    sql,
                    [new SqlValidationError
                    {
                        Code = "SEC001",
                        Message = "SQL sorgusu kritik güvenlik riski içeriyor.",
                        Severity = SqlErrorSeverity.Critical,
                        Suggestion = "Bu tür DDL/DML komutları rapor sorgularında kullanılamaz."
                    }],
                    "Güvenlik kontrolü başarısız: Tehlikeli SQL pattern'leri tespit edildi."
                ) with { SecurityIssues = securityIssues };
            }

            // 2. LLM ile syntax ve semantic validasyon
            var prompt = BuildValidationPrompt(sql, databaseType, schemaInfo);
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
            _logger.LogDebug("LLM validation yanıtı: {Response}", responseText);

            // 3. Yanıtı parse et
            var validationResponse = ParseValidationResponse(responseText);

            // 4. Sonucu oluştur
            if (validationResponse.IsValid)
            {
                if (!string.IsNullOrEmpty(validationResponse.CorrectedSql) && 
                    validationResponse.CorrectedSql != sql)
                {
                    _logger.LogInformation("SQL düzeltildi");
                    return SqlValidationResult.ValidWithCorrection(
                        sql,
                        validationResponse.CorrectedSql,
                        validationResponse.Explanation ?? "SQL sorgusu düzeltildi."
                    ) with { Warnings = validationResponse.Warnings, SecurityIssues = securityIssues };
                }

                _logger.LogInformation("SQL validation başarılı");
                return SqlValidationResult.Valid(sql, validationResponse.Explanation) 
                    with { Warnings = validationResponse.Warnings, SecurityIssues = securityIssues };
            }

            _logger.LogWarning("SQL validation başarısız - {ErrorCount} hata bulundu", validationResponse.Errors.Count);
            return SqlValidationResult.Invalid(
                sql,
                validationResponse.Errors,
                validationResponse.Explanation
            ) with { 
                Warnings = validationResponse.Warnings, 
                SecurityIssues = securityIssues,
                CorrectedSql = validationResponse.CorrectedSql 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL validation sırasında hata oluştu");
            // Hata durumunda orijinal SQL'i geçerli say (fail-open)
            return SqlValidationResult.Valid(sql, "Validation atlandı: " + ex.Message);
        }
    }

    private List<SqlSecurityIssue> CheckSecurityPatterns(string sql)
    {
        var issues = new List<SqlSecurityIssue>();

        foreach (var pattern in DangerousPatterns)
        {
            if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
            {
                var severity = pattern.Contains("DROP") || pattern.Contains("TRUNCATE") || pattern.Contains("xp_cmdshell")
                    ? SqlSecuritySeverity.Critical
                    : SqlSecuritySeverity.Warning;

                issues.Add(new SqlSecurityIssue
                {
                    Code = $"SEC{issues.Count + 1:D3}",
                    Message = $"Tehlikeli SQL pattern tespit edildi",
                    Severity = severity,
                    Pattern = pattern
                });
            }
        }

        return issues;
    }

    private string BuildValidationPrompt(string sql, string databaseType, string? schemaInfo)
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

    private ValidationResponse ParseValidationResponse(string responseText)
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
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<ValidationResponse>(jsonStr, options);
            return response ?? new ValidationResponse { IsValid = true, Explanation = "Parse edilemedi, geçerli kabul edildi" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validation yanıtı parse edilemedi, geçerli kabul ediliyor");
            return new ValidationResponse { IsValid = true, Explanation = "Parse hatası, geçerli kabul edildi" };
        }
    }

    private class ValidationResponse
    {
        public bool IsValid { get; set; } = true;
        public string? CorrectedSql { get; set; }
        public List<SqlValidationError> Errors { get; set; } = [];
        public List<SqlValidationWarning> Warnings { get; set; } = [];
        public string? Explanation { get; set; }
    }
}
