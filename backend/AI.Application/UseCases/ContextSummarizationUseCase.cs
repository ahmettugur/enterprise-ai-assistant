using AI.Application.Common.Telemetry;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Conversations;
using AI.Application.Ports.Secondary.Services.Cache;
using AI.Application.DTOs.History;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using System.Text;

namespace AI.Application.UseCases;

/// <summary>
/// Context Summarization servisi implementasyonu
/// </summary>
public sealed class ContextSummarizationUseCase : IContextSummarizationUseCase
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly IChatCacheService _cacheService;
    private readonly IConversationRepository _historyRepository;
    private readonly ContextSummarizationSettings _settings;
    private readonly ILogger<ContextSummarizationUseCase> _logger;

    // Token tahminleme sabitleri (ortalama karakter/token oranı)
    private const double AverageCharsPerToken = 4.0;

    public ContextSummarizationUseCase(
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        IChatCacheService cacheService,
        IConversationRepository historyRepository,
        IOptions<ContextSummarizationSettings> settings,
        ILogger<ContextSummarizationUseCase> logger)
    {
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _settings = settings?.Value ?? new ContextSummarizationSettings();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ChatHistory> GetSummarizedChatHistoryAsync(
        Guid conversationId,
        ChatHistory fullHistory,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Chat.StartActivity("GetSummarizedChatHistory");
        activity?.SetTag("conversation.id", conversationId.ToString());

        try
        {
            // Özetleme gerekli değilse orijinal history'yi döndür
            if (!_settings.Enabled || !RequiresSummarization(fullHistory))
            {
                _logger.LogDebug("Özetleme gerekli değil - ConversationId: {ConversationId}", conversationId);
                return fullHistory;
            }

            // Cache'den özet kontrolü
            var cacheKey = $"summary:{conversationId}";
            var cachedSummary = await GetCachedSummaryAsync(cacheKey, cancellationToken);

            string summary;
            int slidingWindowStart;

            if (cachedSummary != null)
            {
                summary = cachedSummary.Summary;
                slidingWindowStart = cachedSummary.LastSummarizedIndex;
                _logger.LogDebug("Özet cache'den alındı - ConversationId: {ConversationId}", conversationId);
            }
            else
            {
                // Yeni özet oluştur
                var messagesToSummarize = GetMessagesToSummarize(fullHistory);
                summary = await SummarizeMessagesAsync(messagesToSummarize, cancellationToken);
                slidingWindowStart = fullHistory.Count - _settings.SlidingWindowSize;

                // Cache'e kaydet
                await CacheSummaryAsync(cacheKey, summary, slidingWindowStart, cancellationToken);
                _logger.LogInformation("Yeni özet oluşturuldu - ConversationId: {ConversationId}, " +
                    "Özetlenen mesaj: {SummarizedCount}, Window başlangıç: {WindowStart}",
                    conversationId, slidingWindowStart, slidingWindowStart);
            }

            // Yeni ChatHistory oluştur: System + Özet + Sliding Window
            return BuildSummarizedChatHistory(fullHistory, summary, slidingWindowStart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Özetleme hatası - ConversationId: {ConversationId}", conversationId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            // Hata durumunda orijinal history'yi döndür
            return fullHistory;
        }
    }

    /// <inheritdoc />
    public bool RequiresSummarization(ChatHistory history)
    {
        if (history == null || history.Count == 0)
            return false;

        var estimatedTokens = EstimateTokenCount(history);
        var requires = estimatedTokens > _settings.MaxTokenThreshold;

        _logger.LogDebug("Token tahmini: {Tokens}, Threshold: {Threshold}, Özetleme gerekli: {Required}",
            estimatedTokens, _settings.MaxTokenThreshold, requires);

        return requires;
    }

    /// <inheritdoc />
    public async Task<string> SummarizeMessagesAsync(
        IEnumerable<ChatMessageContent> messages,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Chat.StartActivity("SummarizeMessages");

        try
        {
            // Mesajları metne çevir
            var conversationText = BuildConversationText(messages);

            // Özet prompt'u oluştur
            var prompt = _settings.SummaryPromptTemplate.Replace("{conversation}", conversationText);

            // LLM'den özet al
            var summaryHistory = new ChatHistory();
            summaryHistory.AddUserMessage(prompt);

            var executionSettings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    ["max_tokens"] = _settings.SummaryMaxTokens,
                    ["temperature"] = 0.3 // Daha deterministik özet için düşük temperature
                }
            };

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                summaryHistory,
                executionSettings,
                _kernel,
                cancellationToken);

            var summary = response?.Content ?? string.Empty;

            activity?.SetTag("summary.length", summary.Length);
            activity?.SetTag("original.message_count", messages.Count());

            _logger.LogDebug("Özet oluşturuldu - Karakter: {Length}", summary.Length);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj özetleme hatası");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// Token sayısını tahmin eder (karakter sayısına göre)
    /// Daha doğru tahmin için tiktoken kullanılabilir
    /// </summary>
    private int EstimateTokenCount(ChatHistory history)
    {
        var totalChars = history.Sum(m => m.Content?.Length ?? 0);
        return (int)(totalChars / AverageCharsPerToken);
    }

    /// <summary>
    /// Özetlenecek mesajları belirler (sliding window dışında kalanlar)
    /// </summary>
    private IEnumerable<ChatMessageContent> GetMessagesToSummarize(ChatHistory history)
    {
        // System mesajını atla, sliding window'u da atla
        var messagesToSummarize = history
            .Where(m => m.Role != AuthorRole.System)
            .SkipLast(_settings.SlidingWindowSize)
            .ToList();

        return messagesToSummarize;
    }

    /// <summary>
    /// Mesajları okunabilir metin formatına çevirir
    /// </summary>
    private static string BuildConversationText(IEnumerable<ChatMessageContent> messages)
    {
        var sb = new StringBuilder();

        foreach (var message in messages)
        {
            var role = message.Role == AuthorRole.User ? "Kullanıcı" : "Asistan";
            sb.AppendLine($"{role}: {message.Content}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Özetlenmiş ChatHistory oluşturur
    /// Format: [System] + [Özet (System rolünde)] + [Son N mesaj]
    /// </summary>
    private ChatHistory BuildSummarizedChatHistory(
        ChatHistory fullHistory,
        string summary,
        int slidingWindowStart)
    {
        var summarizedHistory = new ChatHistory();

        // 1. Orijinal system prompt'u ekle
        var systemMessage = fullHistory.FirstOrDefault(m => m.Role == AuthorRole.System);
        if (systemMessage != null)
        {
            summarizedHistory.AddSystemMessage(systemMessage.Content ?? string.Empty);
        }

        // 2. Özeti system mesajı olarak ekle (bağlam için)
        if (!string.IsNullOrWhiteSpace(summary))
        {
            var summaryContext = $"\n\n[Önceki Konuşma Özeti]\n{summary}\n[Özet Sonu]\n";
            
            // Eğer system message varsa, ona ekle
            if (systemMessage != null && summarizedHistory.Count > 0)
            {
                var existingSystem = summarizedHistory[0];
                summarizedHistory.RemoveAt(0);
                summarizedHistory.Insert(0, new ChatMessageContent(
                    AuthorRole.System,
                    existingSystem.Content + summaryContext));
            }
            else
            {
                summarizedHistory.AddSystemMessage(summaryContext);
            }
        }

        // 3. Sliding window mesajlarını ekle (son N mesaj)
        var slidingWindowMessages = fullHistory
            .Where(m => m.Role != AuthorRole.System)
            .Skip(Math.Max(0, slidingWindowStart))
            .ToList();

        foreach (var message in slidingWindowMessages)
        {
            summarizedHistory.Add(message);
        }

        _logger.LogDebug("Özetlenmiş history oluşturuldu - " +
            "Toplam mesaj: {Total}, Sliding window: {Window}",
            summarizedHistory.Count, slidingWindowMessages.Count);

        return summarizedHistory;
    }

    /// <summary>
    /// Cache'den özet bilgisini alır
    /// </summary>
    private async Task<SummaryCacheEntry?> GetCachedSummaryAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            return await _cacheService.GetAsync<SummaryCacheEntry>(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Özet cache'den alınamadı - Key: {Key}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Özeti cache'e kaydeder
    /// </summary>
    private async Task CacheSummaryAsync(string cacheKey, string summary, int lastSummarizedIndex, CancellationToken cancellationToken)
    {
        try
        {
            var entry = new SummaryCacheEntry
            {
                Summary = summary,
                LastSummarizedIndex = lastSummarizedIndex,
                CreatedAt = DateTime.UtcNow
            };

            await _cacheService.SetAsync(
                cacheKey,
                entry,
                TimeSpan.FromMinutes(_settings.SummaryCacheTtlMinutes),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Özet cache'e kaydedilemedi - Key: {Key}", cacheKey);
        }
    }

    #endregion
}