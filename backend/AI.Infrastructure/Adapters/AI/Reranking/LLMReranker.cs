using System.Text.Json;
using System.Text.RegularExpressions;
using AI.Application.Configuration;
using AI.Application.DTOs;
using AI.Application.DTOs.AdvancedRag;
using AI.Application.Ports.Secondary.Services.AIChat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AI.Infrastructure.Adapters.AI.Reranking;

/// <summary>
/// LLM tabanlı reranking servisi
/// Arama sonuçlarını LLM ile değerlendirerek yeniden sıralar
/// </summary>
public class LLMReranker : IReranker
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly AdvancedRagSettings _settings;
    private readonly ILogger<LLMReranker> _logger;

    public LLMReranker(
        IChatCompletionService chatCompletionService,
        IOptions<AdvancedRagSettings> settings,
        ILogger<LLMReranker> logger)
    {
        _chatCompletionService = chatCompletionService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _settings.EnableReranking;

    /// <inheritdoc />
    public async Task<List<SearchResult>> RerankAsync(
        string query,
        List<SearchResult> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Reranking devre dışı, orijinal sonuçlar döndürülüyor");
            return candidates.Take(topK).ToList();
        }

        if (candidates.Count == 0)
        {
            return new List<SearchResult>();
        }

        // Eğer aday sayısı topK'dan az veya eşitse, reranking gereksiz
        if (candidates.Count <= topK)
        {
            _logger.LogDebug("Aday sayısı ({Count}) topK'dan ({TopK}) az, reranking atlanıyor", 
                candidates.Count, topK);
            return candidates;
        }

        try
        {
            _logger.LogInformation("Reranking başlatılıyor: {CandidateCount} aday, topK={TopK}", 
                candidates.Count, topK);

            var startTime = DateTime.UtcNow;

            // Batch reranking için prompt oluştur
            var scoredResults = await ScoreCandidatesAsync(query, candidates, cancellationToken);

            // Skorlara göre sırala ve topK kadar döndür
            var rerankedResults = scoredResults
                .OrderByDescending(r => r.RerankScore)
                .Take(topK)
                .Select(r =>
                {
                    // Orijinal skoru güncelle
                    r.Result.Score = r.RerankScore;
                    return r.Result;
                })
                .ToList();

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation("Reranking tamamlandı: {ElapsedMs}ms, {ResultCount} sonuç döndürüldü",
                elapsed.TotalMilliseconds, rerankedResults.Count);

            return rerankedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reranking hatası, orijinal sonuçlar döndürülüyor");
            return candidates.Take(topK).ToList();
        }
    }

    /// <summary>
    /// Adayları LLM ile skorlar
    /// </summary>
    private async Task<List<RerankResult>> ScoreCandidatesAsync(
        string query,
        List<SearchResult> candidates,
        CancellationToken cancellationToken)
    {
        // Batch boyutuna göre grupla (token limiti için)
        var batchSize = _settings.RerankBatchSize;
        var results = new List<RerankResult>();

        for (int i = 0; i < candidates.Count; i += batchSize)
        {
            var batch = candidates.Skip(i).Take(batchSize).ToList();
            var batchResults = await ScoreBatchAsync(query, batch, i, cancellationToken);
            results.AddRange(batchResults);
        }

        return results;
    }

    /// <summary>
    /// Bir batch'i skorlar
    /// </summary>
    private async Task<List<RerankResult>> ScoreBatchAsync(
        string query,
        List<SearchResult> batch,
        int startIndex,
        CancellationToken cancellationToken)
    {
        var documentsText = string.Join("\n\n", batch.Select((r, idx) =>
            $"[DOC_{startIndex + idx}]\n{TruncateContent(r.Content, _settings.MaxContentLengthForRerank)}"));

        var prompt = $@"Sen bir doküman alakalılık değerlendirme asistanısın.
            
Kullanıcının sorusu:
""{query}""

Aşağıdaki dokümanları soruyla alakalılıklarına göre 0-10 arası puanla.

Puanlama kriterleri:
- 10: Tam olarak soruyu yanıtlıyor
- 7-9: Büyük ölçüde alakalı, faydalı bilgi içeriyor
- 4-6: Kısmen alakalı, dolaylı bilgi içeriyor
- 1-3: Az alakalı, sınırlı fayda
- 0: Alakasız

Dokümanlar:
{documentsText}

SADECE JSON formatında yanıt ver, başka bir şey yazma:
[
  {{""doc_id"": ""DOC_0"", ""score"": 8}},
  {{""doc_id"": ""DOC_1"", ""score"": 5}}
]";

        var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,
                Temperature = 0.0f // Deterministik scoring
            },
            cancellationToken: cancellationToken);

        var content = response?.Content?.Trim() ?? "[]";

        // JSON parse
        return ParseScores(content, batch, startIndex);
    }

    /// <summary>
    /// LLM yanıtından skorları parse eder
    /// </summary>
    private List<RerankResult> ParseScores(string content, List<SearchResult> batch, int startIndex)
    {
        var results = new List<RerankResult>();

        try
        {
            // JSON array'i bul
            var jsonMatch = Regex.Match(content, @"\[[\s\S]*\]");
            if (!jsonMatch.Success)
            {
                _logger.LogWarning("JSON array bulunamadı, varsayılan skorlar kullanılıyor");
                return batch.Select((r, idx) => new RerankResult
                {
                    Result = r,
                    RerankScore = r.Score // Orijinal skoru kullan
                }).ToList();
            }

            var scores = JsonSerializer.Deserialize<List<DocScore>>(jsonMatch.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (scores == null)
            {
                throw new JsonException("Scores null");
            }

            // Skorları eşleştir
            foreach (var (result, idx) in batch.Select((r, i) => (r, i)))
            {
                var docId = $"DOC_{startIndex + idx}";
                var scoreEntry = scores.FirstOrDefault(s => s.DocId == docId);
                var score = scoreEntry?.Score ?? 5; // Varsayılan 5

                results.Add(new RerankResult
                {
                    Result = result,
                    RerankScore = score / 10.0f // 0-10'u 0-1'e normalize et
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skor parse hatası, orijinal skorlar kullanılıyor");
            results = batch.Select(r => new RerankResult
            {
                Result = r,
                RerankScore = r.Score
            }).ToList();
        }

        return results;
    }

    /// <summary>
    /// İçeriği belirli uzunlukta keser
    /// </summary>
    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "...";
    }

    private class DocScore
    {
        public string DocId { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
