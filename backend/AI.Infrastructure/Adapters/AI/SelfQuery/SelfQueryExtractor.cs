using System.Text.Json;
using System.Text.RegularExpressions;
using AI.Application.Configuration;
using AI.Application.DTOs.AdvancedRag;
using AI.Application.Ports.Secondary.Services.AIChat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AI.Infrastructure.Adapters.AI.SelfQuery;

/// <summary>
/// LLM tabanlı Self-Query implementasyonu
/// Kullanıcı sorgusundan metadata filtrelerini otomatik çıkarır
/// </summary>
public class SelfQueryExtractor : ISelfQueryExtractor
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly AdvancedRagSettings _settings;
    private readonly ILogger<SelfQueryExtractor> _logger;

    // Varsayılan metadata alanları
    private static readonly List<MetadataFieldInfo> DefaultMetadataFields = new()
    {
        new MetadataFieldInfo
        {
            FieldName = "category",
            Description = "Doküman kategorisi (örn: finans, hukuk, insan kaynakları)",
            FieldType = MetadataFieldType.String
        },
        new MetadataFieldInfo
        {
            FieldName = "year",
            Description = "Dokümanın yılı",
            FieldType = MetadataFieldType.Integer
        },
        new MetadataFieldInfo
        {
            FieldName = "fileName",
            Description = "Dosya adı",
            FieldType = MetadataFieldType.String
        },
        new MetadataFieldInfo
        {
            FieldName = "documentType",
            Description = "Doküman tipi (örn: politika, prosedür, kılavuz)",
            FieldType = MetadataFieldType.String
        },
        new MetadataFieldInfo
        {
            FieldName = "department",
            Description = "İlgili departman",
            FieldType = MetadataFieldType.String
        },
        new MetadataFieldInfo
        {
            FieldName = "language",
            Description = "Doküman dili (tr, en)",
            FieldType = MetadataFieldType.String
        }
    };

    public SelfQueryExtractor(
        IChatCompletionService chatCompletionService,
        IOptions<AdvancedRagSettings> settings,
        ILogger<SelfQueryExtractor> logger)
    {
        _chatCompletionService = chatCompletionService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _settings.EnableSelfQuery;

    /// <inheritdoc />
    public async Task<SelfQueryResult> ExtractAsync(
        string userQuery,
        List<MetadataFieldInfo>? availableMetadataFields = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Self-Query devre dışı");
            return new SelfQueryResult
            {
                OriginalQuery = userQuery,
                SemanticQuery = userQuery
            };
        }

        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return new SelfQueryResult
            {
                OriginalQuery = userQuery,
                SemanticQuery = userQuery
            };
        }

        try
        {
            _logger.LogInformation("Self-Query extraction başlatılıyor: {Query}", userQuery);

            var fields = availableMetadataFields ?? DefaultMetadataFields;
            var fieldsDescription = BuildFieldsDescription(fields);

            var prompt = $@"Sen bir sorgu analiz asistanısın. Kullanıcının sorgusunu analiz edip:
1. Semantic arama için kullanılacak ana sorguyu
2. Metadata filtrelerini
ayırman gerekiyor.

Kullanılabilir metadata alanları:
{fieldsDescription}

Kullanıcı sorgusu:
""{userQuery}""

Kurallar:
- Eğer sorguda tarih/yıl belirtilmişse ""year"" filtresine çevir
- Eğer kategori/departman belirtilmişse ilgili filtreye çevir
- Semantic sorgudan metadata bilgilerini ÇIKAR, sadece arama yapılacak konuyu bırak
- Eğer filtre bulunamazsa filters boş obje olsun

Örnekler:
- ""2024 yılına ait finans dökümanlarındaki komisyon bilgisi""
  → {{""semanticQuery"": ""komisyon bilgisi"", ""filters"": {{""year"": 2024, ""category"": ""finans""}}}}

- ""İade politikası nedir?""
  → {{""semanticQuery"": ""iade politikası"", ""filters"": {{}}}}

- ""İK departmanının izin prosedürleri""
  → {{""semanticQuery"": ""izin prosedürleri"", ""filters"": {{""department"": ""insan kaynakları""}}}}

SADECE JSON formatında yanıt ver:
{{""semanticQuery"": ""..."", ""filters"": {{...}}}}";

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 300,
                    Temperature = 0.0f
                },
                cancellationToken: cancellationToken);

            var content = response?.Content?.Trim() ?? "";

            return ParseResult(content, userQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Self-Query extraction hatası");
            return new SelfQueryResult
            {
                OriginalQuery = userQuery,
                SemanticQuery = userQuery,
                Explanation = $"Extraction hatası: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Metadata alanlarının açıklamasını oluşturur
    /// </summary>
    private static string BuildFieldsDescription(List<MetadataFieldInfo> fields)
    {
        return string.Join("\n", fields.Select(f =>
            $"- {f.FieldName} ({f.FieldType}): {f.Description}" +
            (f.PossibleValues != null ? $" [Olası değerler: {string.Join(", ", f.PossibleValues)}]" : "")));
    }

    /// <summary>
    /// LLM yanıtını parse eder
    /// </summary>
    private SelfQueryResult ParseResult(string content, string originalQuery)
    {
        try
        {
            // JSON objesini bul
            var jsonMatch = Regex.Match(content, @"\{[\s\S]*\}");
            if (!jsonMatch.Success)
            {
                _logger.LogWarning("JSON bulunamadı, orijinal sorgu kullanılıyor");
                return new SelfQueryResult
                {
                    OriginalQuery = originalQuery,
                    SemanticQuery = originalQuery
                };
            }

            var parsed = JsonSerializer.Deserialize<SelfQueryResponse>(jsonMatch.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                throw new JsonException("Parse result null");
            }

            var result = new SelfQueryResult
            {
                OriginalQuery = originalQuery,
                SemanticQuery = parsed.SemanticQuery ?? originalQuery,
                Filters = parsed.Filters ?? new Dictionary<string, object>()
            };

            // Filters'daki JsonElement'leri native tiplere çevir
            result.Filters = ConvertFilters(result.Filters);

            _logger.LogInformation(
                "Self-Query extraction tamamlandı: SemanticQuery='{SemanticQuery}', FilterCount={FilterCount}",
                result.SemanticQuery, result.Filters.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Parse hatası, orijinal sorgu kullanılıyor");
            return new SelfQueryResult
            {
                OriginalQuery = originalQuery,
                SemanticQuery = originalQuery,
                Explanation = $"Parse hatası: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// JsonElement değerlerini native tiplere çevirir
    /// </summary>
    private static Dictionary<string, object> ConvertFilters(Dictionary<string, object> filters)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in filters)
        {
            if (kvp.Value is JsonElement jsonElement)
            {
                result[kvp.Key] = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString() ?? "",
                    JsonValueKind.Number => jsonElement.TryGetInt32(out var intVal) ? intVal : jsonElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => jsonElement.ToString()
                };
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private class SelfQueryResponse
    {
        public string? SemanticQuery { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
    }
}
