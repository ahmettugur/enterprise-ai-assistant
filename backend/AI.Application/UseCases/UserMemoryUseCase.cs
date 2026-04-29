using System.Text;
using System.Text.Json;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Documents;
using AI.Domain.Memory;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Vector;
using AI.Application.Results;
using AI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Application.UseCases;

/// <summary>
/// Kullanıcı uzun vadeli hafıza yönetim servisi implementasyonu.
/// L1 (CurrentUserService) + L2 (Semantic search) stratejisi kullanır.
/// </summary>
public sealed class UserMemoryUseCase : IUserMemoryUseCase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserMemoryRepository _repository;
    private readonly IQdrantService _qdrantService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<UserMemoryUseCase> _logger;

    private const string MEMORY_COLLECTION = "user_memories";
    private const int MAX_MEMORIES_PER_USER = 100;
    private const float MIN_EXTRACTION_CONFIDENCE = 0.6f;
    private const int MIN_MESSAGE_LENGTH_FOR_EXTRACTION = 10;

    public UserMemoryUseCase(
        ICurrentUserService currentUserService,
        IUserMemoryRepository repository,
        IQdrantService qdrantService,
        IEmbeddingService embeddingService,
        IChatCompletionService chatCompletionService,
        ILogger<UserMemoryUseCase> logger)
    {
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _qdrantService = qdrantService ?? throw new ArgumentNullException(nameof(qdrantService));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<string> BuildMemoryContextAsync(string query, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        try
        {
            // ═══════════════════════════════════════════════════════════════════
            // L0: Tarih bilgisi - LLM'in tarih hesaplamaları için (BEDAVA)
            // "Son 3 ay", "geçen hafta", "bu yıl" gibi ifadeleri çözümleyebilir
            // ═══════════════════════════════════════════════════════════════════
            var now = DateTime.Now;
            var turkishCulture = new System.Globalization.CultureInfo("tr-TR");
            sb.Append($"Tarih: {now.ToString("d MMMM yyyy, dddd", turkishCulture)}");
            sb.Append($" (Saat: {now:HH:mm})");

            // ═══════════════════════════════════════════════════════════════════
            // L1: CurrentUserService'den kritik bilgiler - BEDAVA (zaten JWT'de)
            // ═══════════════════════════════════════════════════════════════════
            if (_currentUserService.IsAuthenticated)
            {
                var displayName = _currentUserService.DisplayName;
                if (!string.IsNullOrEmpty(displayName))
                {
                    sb.Append($". Kullanıcı: {displayName}");
                }

                var roles = _currentUserService.Roles.ToList();
                if (roles.Count > 0)
                {
                    sb.Append($" ({string.Join(", ", roles)})");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // L2: Öğrenilen tercihler - Semantic search ile (~50-80 token)
            // ═══════════════════════════════════════════════════════════════════
            var userId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrWhiteSpace(query))
            {
                var relevantMemories = await GetRelevantMemoriesAsync(query, topK: 5, cancellationToken);

                if (relevantMemories.Count > 0)
                {
                    if (sb.Length > 0)
                        sb.Append(". ");

                    sb.Append("Context: ");
                    sb.Append(string.Join("; ", relevantMemories.Select(m => $"{m.Key}: {m.Value}")));
                }
            }
        }
        catch (Exception ex)
        {
            // Memory context oluşturma başarısız olsa bile chat devam etmeli
            _logger.LogWarning(ex, "Failed to build memory context, continuing without it");
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task<List<UserMemoryDto>> GetRelevantMemoriesAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        try
        {
            // Collection var mı kontrol et
            var collections = await _qdrantService.GetCollectionsAsync(cancellationToken);
            if (!collections.Contains(MEMORY_COLLECTION))
            {
                _logger.LogDebug("Memory collection does not exist yet");
                return [];
            }

            // Query embedding oluştur
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

            // Qdrant'ta semantic search
            var filter = new Dictionary<string, object>
            {
                ["user_id"] = userId
            };

            var searchResults = await _qdrantService.SearchAsync(
                MEMORY_COLLECTION,
                queryEmbedding,
                limit: topK,
                minScore: 0.2f,
                filter: filter,
                cancellationToken: cancellationToken
            );

            // Sonuçları DTO'ya dönüştür
            var memories = searchResults
                .Where(r => r.Metadata != null && r.Metadata.Count > 0)
                .Select(r => new UserMemoryDto
                {
                    Id = Guid.TryParse(r.Metadata.GetValueOrDefault("id")?.ToString(), out var id) ? id : Guid.Empty,
                    Key = r.Metadata.GetValueOrDefault("key")?.ToString() ?? string.Empty,
                    Value = r.Metadata.GetValueOrDefault("value")?.ToString() ?? string.Empty,
                    Category = Enum.TryParse<MemoryCategory>(r.Metadata.GetValueOrDefault("category")?.ToString(), out var cat) ? cat : MemoryCategory.Preference,
                    Confidence = float.TryParse(r.Metadata.GetValueOrDefault("confidence")?.ToString(), out var conf) ? conf : 0f,
                    UsageCount = int.TryParse(r.Metadata.GetValueOrDefault("usage_count")?.ToString(), out var count) ? count : 0
                })
                .Where(m => !string.IsNullOrEmpty(m.Key))
                .ToList();

            // Erişim sayılarını güncelle (fire-and-forget)
            _ = Task.Run(async () =>
            {
                foreach (var memory in memories.Where(m => m.Id != Guid.Empty))
                {
                    try
                    {
                        var dbMemory = await _repository.GetByIdAsync(memory.Id, CancellationToken.None);
                        if (dbMemory != null)
                        {
                            dbMemory.IncrementUsage();
                            await _repository.UpdateAsync(dbMemory, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to increment usage count for memory: {MemoryId}", memory.Id);
                    }
                }
            }, cancellationToken);

            return memories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting relevant memories for user: {UserId}", userId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<List<UserMemoryDto>> GetAllMemoriesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        var memories = await _repository.GetAllByUserIdAsync(userId, cancellationToken);

        return memories.Select(m => new UserMemoryDto
        {
            Id = m.Id,
            Key = m.Key,
            Value = m.Value,
            Category = m.Category,
            Confidence = m.Confidence,
            UsageCount = m.UsageCount,
            CreatedAt = m.CreatedAt,
            LastAccessedAt = m.LastAccessedAt
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task ExtractAndStoreMemoriesAsync(string userMessage, string assistantResponse, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));

        // Çok kısa mesajlarda extraction yapma
        if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Length < MIN_MESSAGE_LENGTH_FOR_EXTRACTION)
            return;

        try
        {
            // Kullanıcının hafıza limiti kontrolü
            var currentCount = await _repository.GetCountByUserIdAsync(userId, cancellationToken);
            if (currentCount >= MAX_MEMORIES_PER_USER)
            {
                _logger.LogDebug("User {UserId} has reached memory limit: {Count}", userId, currentCount);
                return;
            }

            var extractionPrompt = @"Bu konuşmayı analiz et ve gelecekteki konuşmalar için faydalı olabilecek kullanıcı bilgilerini çıkar.

Kullanıcı mesajı: " + userMessage + @"
Asistan yanıtı: " + assistantResponse + @"

Şu tür bilgileri ara (doğrudan söylenmiş veya ima edilmiş olabilir):
- Kullanıcının adı veya nasıl hitap edilmesini istediği
- Çalıştığı departman veya pozisyon
- Tercih edilen rapor formatları (Excel, PDF, grafik vb.)
- İlgilenilen konular, metrikler veya KPI'lar
- Sık sorulan rapor türleri veya zaman dilimleri (haftalık, aylık vb.)
- Çalışma tarzı tercihleri
- Herhangi bir kişisel tercih veya alışkanlık

Çıkarılan bilgileri JSON dizisi olarak döndür:
[
  {""key"": ""kullanici_adi"", ""value"": ""Ahmet"", ""category"": ""Preference"", ""confidence"": 0.95},
  {""key"": ""departman"", ""value"": ""Finans"", ""category"": ""WorkContext"", ""confidence"": 0.85}
]

Geçerli kategoriler: Preference, Interaction, Feedback, WorkContext

Kurallar:
- Key küçük harfle ve alt çizgi ile olmalı
- Value kısa olmalı (maksimum 100 karakter)
- SADECE JSON dizisini döndür, başka metin ekleme
- Çıkarılacak bilgi yoksa boş dizi [] döndür
- Açıkça belirtilen bilgiler için yüksek confidence (0.85-1.0)
- Dolaylı veya ima edilen bilgiler için orta confidence (0.6-0.8)";

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddSystemMessage("Sen bir bilgi çıkarma asistanısın. Konuşmalardan kullanıcı tercihlerini ve bağlamını çıkar. Her zaman geçerli JSON ile yanıt ver.");
            chatHistory.AddUserMessage(extractionPrompt);

            _logger.LogDebug("Memory extraction - Calling LLM with prompt length: {Length}", extractionPrompt.Length);

            var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);

            _logger.LogInformation("Memory extraction - Response object: {IsNull}, Content: {Content}",
                response == null ? "NULL" : "OK",
                response?.Content ?? "(null content)");

            var responseText = response?.Content?.Trim() ?? "[]";

            _logger.LogInformation("Memory extraction - LLM response: {Response}", responseText);

            // JSON parse
            var extractedMemories = ParseExtractedMemories(responseText);

            _logger.LogDebug("Memory extraction - Parsed {Count} memories with confidence >= {MinConfidence}",
                extractedMemories.Count(m => m.Confidence >= MIN_EXTRACTION_CONFIDENCE), MIN_EXTRACTION_CONFIDENCE);

            foreach (var extracted in extractedMemories.Where(m => m.Confidence >= MIN_EXTRACTION_CONFIDENCE))
            {
                await AddOrUpdateMemoryAsync(userId, extracted, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Extraction başarısız olsa bile ana akışı bozma
            _logger.LogError(ex, "Memory extraction failed for user {UserId}. Message: {Message}", userId, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ResultBase> AddMemoryAsync(string key, string value, MemoryCategory category, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return ResultBase.Error("User not authenticated");

        try
        {
            var extracted = new ExtractedMemoryDto
            {
                Key = key,
                Value = value,
                Category = category.ToString(),
                Confidence = 1.0f
            };

            await AddOrUpdateMemoryAsync(userId, extracted, cancellationToken);
            return ResultBase.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding memory for user: {UserId}", userId);
            return ResultBase.Error($"Failed to add memory: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ResultBase> UpdateMemoryAsync(Guid id, string newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            var memory = await _repository.GetByIdAsync(id, cancellationToken);
            if (memory == null)
                return ResultBase.Error("Memory not found");

            // Yetki kontrolü
            if (memory.UserId != _currentUserService.UserId)
                return ResultBase.Error("Access denied");

            memory.UpdateValue(newValue);
            await _repository.UpdateAsync(memory, cancellationToken);

            // Qdrant'ı da güncelle
            await UpdateVectorAsync(memory, cancellationToken);

            return ResultBase.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating memory: {Id}", id);
            return ResultBase.Error($"Failed to update memory: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ResultBase> DeleteMemoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var memory = await _repository.GetByIdAsync(id, cancellationToken);
            if (memory == null)
                return ResultBase.Error("Memory not found");

            // Yetki kontrolü
            if (memory.UserId != _currentUserService.UserId)
                return ResultBase.Error("Access denied");

            await _repository.DeleteAsync(id, cancellationToken);

            // Qdrant'tan da sil
            await _qdrantService.DeleteVectorAsync(MEMORY_COLLECTION, id, cancellationToken);

            return ResultBase.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting memory: {Id}", id);
            return ResultBase.Error($"Failed to delete memory: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ResultBase> ForgetUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return ResultBase.Error("User not authenticated");

        try
        {
            // Önce tüm memory ID'lerini al
            var memories = await _repository.GetAllByUserIdAsync(userId, cancellationToken);

            // DB'den sil (soft delete)
            await _repository.DeleteAllByUserIdAsync(userId, cancellationToken);

            // Qdrant'tan da sil
            foreach (var memory in memories)
            {
                try
                {
                    await _qdrantService.DeleteVectorAsync(MEMORY_COLLECTION, memory.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete vector for memory: {MemoryId}", memory.Id);
                }
            }

            _logger.LogInformation("Forgot user: {UserId}, deleted {Count} memories", userId, memories.Count);
            return ResultBase.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forgetting user: {UserId}", userId);
            return ResultBase.Error($"Failed to forget user: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ResultBase> InitializeCollectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _qdrantService.GetCollectionsAsync(cancellationToken);
            if (collections.Contains(MEMORY_COLLECTION))
            {
                _logger.LogDebug("Memory collection already exists");
                return ResultBase.Success();
            }

            var success = await _qdrantService.CreateCollectionAsync(
                MEMORY_COLLECTION,
                _embeddingService.EmbeddingDimension,
                cancellationToken
            );

            if (success)
            {
                _logger.LogInformation("Created memory collection: {Collection}", MEMORY_COLLECTION);
                return ResultBase.Success();
            }

            return ResultBase.Error("Failed to create memory collection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing memory collection");
            return ResultBase.Error($"Failed to initialize collection: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task AddOrUpdateMemoryAsync(string userId, ExtractedMemoryDto extracted, CancellationToken cancellationToken)
    {
        var normalizedKey = extracted.Key.ToLowerInvariant().Trim();
        var existing = await _repository.GetByKeyAsync(userId, normalizedKey, cancellationToken);

        if (existing != null)
        {
            // Mevcut kaydı güncelle
            existing.UpdateValue(extracted.Value);
            existing.BoostConfidence();
            await _repository.UpdateAsync(existing, cancellationToken);
            await UpdateVectorAsync(existing, cancellationToken);

            _logger.LogDebug("Updated existing memory for user: {UserId}, key: {Key}", userId, normalizedKey);
        }
        else
        {
            // Yeni kayıt oluştur
            if (!Enum.TryParse<MemoryCategory>(extracted.Category, out var category))
                category = MemoryCategory.Preference;

            var memory = UserMemory.Create(userId, normalizedKey, extracted.Value, category, extracted.Confidence);
            await _repository.AddAsync(memory, cancellationToken);
            await UpsertVectorAsync(memory, cancellationToken);

            _logger.LogDebug("Added new memory for user: {UserId}, key: {Key}", userId, normalizedKey);
        }
    }

    private async Task UpsertVectorAsync(UserMemory memory, CancellationToken cancellationToken)
    {
        try
        {
            // Collection var mı kontrol et
            var collections = await _qdrantService.GetCollectionsAsync(cancellationToken);
            if (!collections.Contains(MEMORY_COLLECTION))
            {
                await InitializeCollectionAsync(cancellationToken);
            }

            // Embedding oluştur
            var embeddingText = memory.ToEmbeddingText();
            var embedding = await _embeddingService.GenerateEmbeddingAsync(embeddingText, cancellationToken);

            // Metadata JSON olarak kaydet
            var metadataDict = new Dictionary<string, string>
            {
                ["id"] = memory.Id.ToString(),
                ["user_id"] = memory.UserId,
                ["key"] = memory.Key,
                ["value"] = memory.Value,
                ["category"] = memory.Category.ToString(),
                ["confidence"] = memory.Confidence.Value.ToString("F2"),
                ["usage_count"] = memory.UsageCount.ToString()
            };

            // Payload oluştur
            // UserMemory is not a document — use memory.Id as a semantic placeholder
            // to satisfy DDD validation (documentId cannot be Guid.Empty)
            var chunk = DocumentChunk.Create(
                documentId: memory.Id,
                chunkIndex: 0,
                content: embeddingText,
                startPosition: 0,
                endPosition: embeddingText.Length,
                metadata: JsonSerializer.Serialize(metadataDict)
            );

            await _qdrantService.UpsertVectorAsync(MEMORY_COLLECTION, chunk, embedding, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert vector for memory: {MemoryId}", memory.Id);
        }
    }

    private async Task UpdateVectorAsync(UserMemory memory, CancellationToken cancellationToken)
    {
        // Delete and re-insert (Qdrant update = upsert)
        await UpsertVectorAsync(memory, cancellationToken);
    }

    private List<ExtractedMemoryDto> ParseExtractedMemories(string jsonText)
    {
        try
        {
            // JSON array'i bul (bazen LLM ek metin ekleyebilir)
            var startIndex = jsonText.IndexOf('[');
            var endIndex = jsonText.LastIndexOf(']');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                jsonText = jsonText.Substring(startIndex, endIndex - startIndex + 1);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<ExtractedMemoryDto>>(jsonText, options) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse extracted memories JSON: {Json}", jsonText);
            return [];
        }
    }

    #endregion
}