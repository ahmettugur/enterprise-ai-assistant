using AI.Application.Common.Constants;
using AI.Application.Configuration;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Conversations;
using AI.Application.DTOs.History;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Cache;
using AI.Application.DTOs.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Polly;
using Polly.Retry;

namespace AI.Application.UseCases;

/// <summary>
/// Orchestration service for chat history management
/// Coordinates between repository and cache layers
/// Maintains backward compatibility with existing IConversationUseCase interface
/// 
/// İyileştirmeler:
/// - Configuration-based TTL değerleri (CacheSettings)
/// - Retry politikası (Polly)
/// - Tutarlı cache key kullanımı (CacheKeys sınıfı)
/// - Result pattern için hazırlık
/// - ICurrentUserService ile kullanıcı bilgisi
/// </summary>
public sealed class ConversationUseCase : IConversationUseCase, IDisposable
{
    private readonly IConversationRepository _repository;
    private readonly IConversationQueryService _historyQueryService;
    private readonly IChatCacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ConversationUseCase> _logger;
    private readonly CacheSettings _cacheSettings;
    private readonly AsyncRetryPolicy _retryPolicy;
    private bool _disposed;

    public ConversationUseCase(
        IConversationRepository repository,
        IConversationQueryService historyQueryService,
        IChatCacheService cacheService,
        ICurrentUserService currentUserService,
        IOptions<CacheSettings> cacheSettings,
        ILogger<ConversationUseCase> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _historyQueryService = historyQueryService ?? throw new ArgumentNullException(nameof(historyQueryService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheSettings = cacheSettings?.Value ?? new CacheSettings();

        // Retry politikası: 3 deneme, exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {RetryCount} after {Delay}ms - Operation: {Operation}",
                        retryCount, timeSpan.TotalMilliseconds, context.OperationKey);
                });
    }

    #region IConversationUseCase Implementation

    /// <inheritdoc />
    public async Task<ChatHistory> GetChatHistoryAsync(ChatRequest request,
        bool includeDbResponses = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            _logger.LogWarning("Boş ConversationId için chat history alınamaz");
            return new ChatHistory();
        }

        try
        {
            var conversationId = request.ConversationId;

            // Önce cache'den kontrol et - tutarlı cache key kullan
            // Not: Cache'de filtreleme yapılmaz, bu yüzden includeDbResponses false ise cache'i bypass et
            var cacheKey = CacheKeys.ChatHistory(conversationId);
            ChatHistory? cachedHistory = null;
            if (includeDbResponses)
            {
                cachedHistory = await _cacheService.GetChatHistoryAsync(conversationId, cancellationToken);
                if (cachedHistory != null)
                {
                    _logger.LogDebug("Chat history cache'den alındı - ConversationId: {ConversationId}", conversationId);
                    return cachedHistory;
                }
            }

            // Cache miss - conversation'ı bul veya oluştur
            _logger.LogDebug("ConversationId ile conversation aranıyor - ConversationId: {ConversationId}",
                conversationId);
            var conversation =
                await GetOrCreateConversationByIdAsync(conversationId, request.ConnectionId, cancellationToken);
            _logger.LogDebug("Conversation bulundu - ConversationId: {ConversationId}, Id: {Id}", conversationId,
                conversation.Id);

            // Get messages for the conversation (with retry)
            var messages = await _retryPolicy.ExecuteAsync(
                async (ctx) => await _repository.GetMessagesAsync(conversation.Id, cancellationToken),
                new Context("GetMessagesAsync"));

            // Convert to ChatHistory - includeDbResponses parametresini geç
            var chatHistory = ConvertToChatHistory(messages, includeDbResponses);

            // Cache the result using conversationId (sadece includeDbResponses true ise cache'le)
            if (includeDbResponses)
            {
                await _cacheService.SetChatHistoryAsync(conversationId, chatHistory, _cacheSettings.ChatHistoryTtl, cancellationToken);
            }

            _logger.LogDebug("Chat history repository'den alındı - ConversationId: {ConversationId}, IncludeDbResponses: {IncludeDbResponses}",
                conversationId, includeDbResponses);
            return chatHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat history alınırken hata oluştu - ConversationId: {ConversationId}",
                request?.ConversationId);
            return new ChatHistory();
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsValidConversationIdAsync(string conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                _logger.LogWarning("Boş ConversationId geçersiz kabul edildi");
                return false;
            }

            var conversation = Guid.TryParse(conversationId, out var guid)
                ? await _repository.GetConversationByIdAsync(guid, cancellationToken)
                : null;
            var isValid = conversation != null;

            if (!isValid)
            {
                _logger.LogWarning("Geçersiz ConversationId: {ConversationId}", conversationId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConversationId doğrulanırken hata - ConversationId: {ConversationId}",
                conversationId ?? "NULL");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveConversationHistoryAsync(string conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                _logger.LogWarning("Boş ConversationId için history silinemez");
                return false;
            }

            // Get conversation first to verify it exists
            var conversation = Guid.TryParse(conversationId, out var guid)
                ? await _repository.GetConversationByIdAsync(guid, cancellationToken)
                : null;

            if (conversation == null)
            {
                _logger.LogWarning("Silinecek conversation bulunamadı - ConversationId: {ConversationId}",
                    conversationId);
                return false;
            }

            // Delete from repository using Guid
            var removed = await _repository.DeleteConversationAsync(conversation.Id, cancellationToken);

            if (removed)
            {
                // Invalidate cache using conversationId
                await _cacheService.InvalidateChatHistoryAsync(conversationId, cancellationToken);
                await _cacheService.InvalidateConversationMetadataAsync(conversationId, cancellationToken);

                _logger.LogInformation("Chat history silindi - ConversationId: {ConversationId}", conversationId);
            }

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat history silinirken hata - ConversationId: {ConversationId}",
                conversationId ?? "NULL");
            return false;
        }
    }


    #endregion

    #region Message Management Methods

    /// <inheritdoc />
    public async Task<AddMessageResultDto> AddSystemMessageAsync(ChatRequest request, string message,
        Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        return await AddMessageInternalAsync(request.ConversationId, request.ConnectionId, message, AuthorRole.System,
            MessageType.System, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AddMessageResultDto> AddUserMessageAsync(ChatRequest request, string message,
        MessageType messageType = MessageType.User, Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return await AddMessageInternalAsync(request.ConversationId, request.ConnectionId, message, AuthorRole.User,
            messageType, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AddMessageResultDto> AddAssistantMessageAsync(ChatRequest request, string message,
        Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        return await AddMessageInternalAsync(request.ConversationId, request.ConnectionId, message,
            AuthorRole.Assistant, MessageType.Assistant, metadata, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConversationDto> ReplaceSystemPromptAsync(ChatRequest request, string newSystemPrompt,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(request.ConversationId, newSystemPrompt);

        try
        {
            // Get conversation by conversationId
            var conversation =
                await GetOrCreateConversationByIdAsync(request.ConversationId, request.ConnectionId, cancellationToken);

            // Replace system prompt in repository
            await _repository.ReplaceSystemPromptAsync(conversation.Id, newSystemPrompt, cancellationToken);
            request.ConversationId = conversation.Id.ToString();
            // Invalidate cache to force refresh using conversationId
            await _cacheService.InvalidateChatHistoryAsync(request.ConversationId, cancellationToken);

            _logger.LogDebug("System prompt değiştirildi - ConversationId: {ConversationId}", request.ConversationId);
            return ConversationDto.FromEntity(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System prompt değiştirilirken hata - ConversationId: {ConversationId}",
                request.ConversationId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveMessagesByTypeAsync(ChatRequest request, MessageType messageType,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(request.ConversationId, "dummy");

        try
        {
            // Get conversation by conversationId
            var conversation = await GetOrCreateConversationByIdAsync(request.ConversationId, request.ConnectionId, cancellationToken);

            // Remove messages by type from repository
            await _repository.RemoveMessagesByTypeAsync(conversation.Id, messageType.ToString(), cancellationToken);

            // Invalidate cache to force refresh using conversationId
            await _cacheService.InvalidateChatHistoryAsync(request.ConversationId, cancellationToken);

            _logger.LogDebug("{MessageType} mesajları silindi - ConversationId: {ConversationId}", messageType,
                request.ConversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Mesajlar silinirken hata - ConversationId: {ConversationId}, MessageType: {MessageType}",
                request.ConversationId, messageType);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>?> GetChatMetadataAsync(string conversationId,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(conversationId, "dummy");

        try
        {
            // First try to get from cache using conversationId
            var cachedMetadata = await _cacheService.GetConversationMetadataAsync(conversationId, cancellationToken);
            if (cachedMetadata != null)
            {
                return ConvertMetadataToDictionary(cachedMetadata);
            }

            var metadata = await _historyQueryService.GetConversationMetadataAsync(conversationId, cancellationToken);
            if (metadata != null)
            {
                // Cache the result using conversationId
                await _cacheService.SetConversationMetadataAsync(conversationId, metadata, null, cancellationToken);
                return ConvertMetadataToDictionary(metadata);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metadata alınırken hata - ConversationId: {ConversationId}", conversationId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ConversationDto> UpdateConversationTitleAsync(string conversationId, string newTitle,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(conversationId, newTitle);

        try
        {
            // Parse conversationId as GUID
            if (!Guid.TryParse(conversationId, out var guid))
            {
                _logger.LogWarning("Geçersiz Conversation ID format - ConversationId: {ConversationId}", conversationId);
                throw new ArgumentException($"Invalid conversation ID format: {conversationId}", nameof(conversationId));
            }

            // Get conversation from repository - must exist
            var conversation = await _repository.GetConversationByIdAsync(guid, cancellationToken);
            if (conversation == null)
            {
                _logger.LogWarning("Conversation bulunamadı - ConversationId: {ConversationId}", conversationId);
                throw new InvalidOperationException($"Conversation with ID {conversationId} not found");
            }

            // Update title in repository
            await _repository.UpdateConversationTitleAsync(conversation.Id, newTitle, cancellationToken);

            // Invalidate cache to force refresh using conversationId
            await _cacheService.InvalidateChatHistoryAsync(conversationId, cancellationToken);
            await _cacheService.InvalidateConversationMetadataAsync(conversationId, cancellationToken);

            _logger.LogInformation("Conversation başlığı güncellendi - ConversationId: {ConversationId}, Yeni Başlık: {Title}",
                conversationId, newTitle);

            // Return updated conversation as DTO
            var updatedConversation = await _repository.GetConversationByIdAsync(conversation.Id, cancellationToken);
            return ConversationDto.FromEntity(updatedConversation ?? conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversation başlığı güncellenirken hata - ConversationId: {ConversationId}",
                conversationId);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Internal method for adding messages with role and type
    /// </summary>
    private async Task<AddMessageResultDto> AddMessageInternalAsync(string conversationId, string connectionId, string message,
        AuthorRole role, MessageType messageType, Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(conversationId, message);

        try
        {
            // Get conversation — aggregate root üzerinden mesaj eklenecek
            var conversation = await GetOrCreateConversationForUpdateAsync(conversationId, connectionId, cancellationToken);

            // Metadata'ya kullanıcı adı ve tarih bilgisi ekle
            metadata ??= new Dictionary<string, object>();

            // Kullanıcı bilgisi (User mesajları için)
            if (role == AuthorRole.User && _currentUserService.IsAuthenticated)
            {
                var displayName = _currentUserService.DisplayName;
                if (!string.IsNullOrEmpty(displayName))
                {
                    metadata["UserDisplayName"] = displayName;
                }
            }

            // Tarih bilgisi - LLM'in tarih hesaplamaları için
            var now = DateTime.Now;
            metadata["Timestamp"] = now.ToString("yyyy-MM-dd HH:mm:ss");
            metadata["Date"] = now.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            metadata["DayOfWeek"] = now.ToString("dddd", new System.Globalization.CultureInfo("tr-TR"));

            var metadataJson = metadata.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(metadata)
                : null;

            // DDD: Message creation through Aggregate Root
            var addedMessage = conversation.AddMessage(
                role.ToString().ToLowerInvariant(),
                message,
                messageType.ToString(),
                null,
                metadataJson);

            // Aggregate root state'ini persist et
            await _repository.SaveConversationAsync(conversation, cancellationToken);

            // Incremental cache update - tam invalidate yerine cache'i güncelle
            await UpdateChatHistoryCacheIncrementallyAsync(
                conversationId,
                addedMessage,
                role,
                cancellationToken);

            _logger.LogDebug("{Role} mesajı eklendi - ConversationId: {ConversationId}", role, conversationId);

            return new AddMessageResultDto
            {
                Conversation = conversation,
                Message = addedMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj eklenirken hata - ConversationId: {ConversationId}, Role: {Role}",
                conversationId, role);
            throw;
        }
    }


    private async Task<Conversation> GetOrCreateConversationByIdAsync(string conversationId, string connectionId,
        CancellationToken cancellationToken)
    {
        // Tutarlı cache key kullan
        var cacheKey = CacheKeys.ConversationDto(conversationId);

        // 1. Cache'den kontrol et - DB verification YAPMA (performans için)
        if (!string.IsNullOrEmpty(conversationId))
        {
            var cachedDto = await _cacheService.GetAsync<ConversationDto>(cacheKey, cancellationToken);
            if (cachedDto != null)
            {
                _logger.LogDebug("Conversation cache'den alındı - ConversationId: {ConversationId}", conversationId);

                // Cache'den entity oluştur - DB'ye gitme!
                // Bu yaklaşım cache'in gerçek faydasını sağlar
                return cachedDto.ToEntity();
            }
        }

        // 2. Cache miss - GUID olarak dene ve DB'den getir (with retry)
        if (Guid.TryParse(conversationId, out var guidConversationId))
        {
            var byId = await _retryPolicy.ExecuteAsync(
                async (ctx) => await _repository.GetConversationByIdAsync(guidConversationId, cancellationToken),
                new Context("GetConversationByIdAsync"));

            if (byId != null)
            {
                // Cache'e ekle
                var dtoToCache = ConversationDto.FromEntity(byId);
                await _cacheService.SetAsync(cacheKey, dtoToCache, _cacheSettings.ConversationDtoTtl, cancellationToken);
                return byId;
            }
        }

        // 3. Bulunamazsa yeni conversation oluştur (with retry)
        // Kullanıcı ID'sini ICurrentUserService'den al
        var userId = _currentUserService.UserId
            ?? throw new InvalidOperationException("UserId is required to create a conversation. User must be authenticated.");

        var newConversation = await _retryPolicy.ExecuteAsync(
            async (ctx) => await _repository.CreateConversationAsync(connectionId, userId, null, cancellationToken),
            new Context("CreateConversationAsync"));

        // Yeni conversation'ı cache'e ekle
        var newConversationDto = ConversationDto.FromEntity(newConversation);
        var newCacheKey = CacheKeys.ConversationDto(newConversation.Id.ToString());
        await _cacheService.SetAsync(newCacheKey, newConversationDto, _cacheSettings.ConversationDtoTtl, cancellationToken);

        return newConversation;
    }

    /// <summary>
    /// Tracked conversation entity getirir — aggregate root üzerinden değişiklik yapılacaksa kullanılır.
    /// </summary>
    private async Task<Conversation> GetOrCreateConversationForUpdateAsync(string conversationId, string connectionId,
        CancellationToken cancellationToken)
    {
        // GUID olarak dene ve tracked entity olarak getir
        if (Guid.TryParse(conversationId, out var guidConversationId))
        {
            var existing = await _retryPolicy.ExecuteAsync(
                async (ctx) => await _repository.GetConversationForUpdateAsync(guidConversationId, cancellationToken),
                new Context("GetConversationForUpdateAsync"));

            if (existing != null)
                return existing;
        }

        // Bulunamazsa yeni conversation oluştur
        var userId = _currentUserService.UserId
            ?? throw new InvalidOperationException("UserId is required to create a conversation. User must be authenticated.");

        var newConversation = await _retryPolicy.ExecuteAsync(
            async (ctx) => await _repository.CreateConversationAsync(connectionId, userId, null, cancellationToken),
            new Context("CreateConversationAsync"));

        return newConversation;
    }


    /// <summary>
    /// Converts domain Message entities to Semantic Kernel ChatHistory
    /// Only includes the system prompt with the highest sequence number
    /// Optimized with single-pass algorithm and pre-allocated capacity to reduce memory allocations
    /// </summary>
    private static ChatHistory ConvertToChatHistory(IEnumerable<Message> messages, bool includeDbResponses = false)
    {
        // Materialized list for count - avoid multiple enumeration
        var messageList = messages as IList<Message> ?? messages.ToList();

        if (messageList.Count == 0)
            return new ChatHistory();

        // Pre-allocate with estimated capacity
        var chatHistory = new ChatHistory();
        Message? latestSystemMessage = null;

        // Pre-allocate list with capacity hint to avoid resizing
        var nonSystemMessages = new List<(DateTime createdAt, AuthorRole role, string content)>(messageList.Count);

        // Tek geçişte tüm mesajları işle - string allocation minimize edildi
        foreach (var message in messageList)
        {
            // IsDbResponse metadata kontrolü - includeDbResponses false ise filtrele
            // Metadata'da IsDbResponse key'i varsa (değeri ne olursa olsun) mesajı atla
            if (!includeDbResponses && !string.IsNullOrEmpty(message.MetadataJson))
            {
                try
                {
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message.MetadataJson);
                    if (metadata != null && metadata.ContainsKey("IsDbResponse"))
                    {
                        // IsDbResponse key'i varsa bu mesajı atla (değer kontrolü yapmıyoruz)
                        continue;
                    }
                }
                catch
                {
                    // JSON parse hatası durumunda mesajı dahil et (güvenli taraf)
                }
            }

            // String.Equals ile case-insensitive karşılaştırma - ToLowerInvariant() allocasyonu önlendi
            if (string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase))
            {
                // En son oluşturulan system mesajını tut
                if (latestSystemMessage == null || message.CreatedAt > latestSystemMessage.CreatedAt)
                {
                    latestSystemMessage = message;
                }
            }
            else
            {
                // System dışı mesajları listeye ekle
                var role = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? AuthorRole.Assistant
                    : AuthorRole.User;

                nonSystemMessages.Add((message.CreatedAt, role, message.Content));
            }
        }

        // System mesajını ilk sıraya ekle
        if (latestSystemMessage != null)
        {
            chatHistory.Add(new ChatMessageContent(AuthorRole.System, latestSystemMessage.Content));
        }

        // Diğer mesajları CreatedAt sırasıyla ekle
        // LINQ OrderBy yerine Sort kullan - daha az allocation
        nonSystemMessages.Sort((a, b) => a.createdAt.CompareTo(b.createdAt));

        foreach (var (_, role, content) in nonSystemMessages)
        {
            chatHistory.Add(new ChatMessageContent(role, content));
        }

        return chatHistory;
    }

    /// <summary>
    /// Converts ConversationMetadata to Dictionary for backward compatibility
    /// </summary>
    private static Dictionary<string, object> ConvertMetadataToDictionary(ConversationMetadata metadata)
    {
        return new Dictionary<string, object>
        {
            ["conversationId"] = metadata.ConversationId,
            ["connectionId"] = metadata.ConnectionId,
            ["messageCount"] = metadata.MessageCount,
            ["lastMessageAt"] = metadata?.LastMessageAt ?? DateTime.MinValue,
            ["createdAt"] = metadata?.CreatedAt ?? DateTime.MinValue,
            ["isArchived"] = metadata?.IsArchived ?? false
        };
    }

    /// <summary>
    /// Validates input parameters
    /// </summary>
    private static void ValidateInputs(string connectionId, string message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mesaj boş olamaz", nameof(message));
    }

    /// <summary>
    /// Updates chat history cache incrementally instead of full invalidation
    /// This prevents cache storms and reduces database load
    /// </summary>
    private async Task UpdateChatHistoryCacheIncrementallyAsync(
        string conversationId,
        Message addedMessage,
        AuthorRole role,
        CancellationToken cancellationToken)
    {
        try
        {
            // Mevcut cache'i al
            var existingHistory = await _cacheService.GetChatHistoryAsync(conversationId, cancellationToken);

            if (existingHistory != null)
            {
                // Cache varsa, yeni mesajı ekle - tam rebuild gereksiz
                existingHistory.Add(new ChatMessageContent(role, addedMessage.Content));

                // Güncellenmiş cache'i yaz
                await _cacheService.SetChatHistoryAsync(
                    conversationId,
                    existingHistory,
                    _cacheSettings.ChatHistoryTtl,
                    cancellationToken);

                _logger.LogDebug("ChatHistory cache incrementally updated - ConversationId: {ConversationId}",
                    conversationId);
            }
            else
            {
                // Cache yoksa invalidate etmeye gerek yok - zaten yok
                // Bir sonraki GetChatHistoryAsync çağrısında DB'den yüklenecek
                _logger.LogDebug("ChatHistory cache not found for incremental update, will be loaded on next request - ConversationId: {ConversationId}",
                    conversationId);
            }
        }
        catch (Exception ex)
        {
            // Cache hatası application'ı kırmamalı
            _logger.LogWarning(ex, "Failed to update cache incrementally, falling back to invalidation - ConversationId: {ConversationId}",
                conversationId);

            // Fallback: cache'i invalidate et
            await _cacheService.InvalidateChatHistoryAsync(conversationId, cancellationToken);
        }
    }

    #endregion

    #region History Query Methods

    private const int MaxContentLength = 10_000;

    /// <inheritdoc />
    public async Task<ConversationListResultDto> GetConversationsWithMessagesAsync(
        string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var skip = (page - 1) * pageSize;
        var conversations = await _repository.GetAllConversationsWithMessagesAsync(userId, skip, pageSize, cancellationToken);

        var data = conversations.Select(c => new ConversationWithMessagesDto
        {
            Id = c.Id,
            ConnectionId = c.ConnectionId,
            UserId = c.UserId,
            Title = c.Title,
            MessageCount = c.Messages?.Count(m =>
                string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase)),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            IsArchived = c.IsArchived,
            Messages = c.Messages?
                .Where(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                .Select(ToMessageSummary)
                .ToList() ?? []
        }).ToList();

        return new ConversationListResultDto
        {
            Data = data,
            TotalCount = conversations.Count,
            Page = page,
            PageSize = pageSize,
            TotalPages = conversations.Count > 0 ? (int)Math.Ceiling((double)conversations.Count / pageSize) : 0
        };
    }

    /// <inheritdoc />
    public async Task<ConversationDetailResultDto?> GetConversationDetailAsync(
        Guid conversationId, string userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetConversationByIdAsync(conversationId, cancellationToken);
        if (conversation == null) return null;

        // Authorization check
        if (conversation.UserId != userId && !isAdmin) return null;

        var messages = await _repository.GetMessagesAsync(conversation.Id, cancellationToken);

        return new ConversationDetailResultDto
        {
            Conversation = ConversationDto.FromEntity(conversation),
            Messages = messages.Select(ToMessageSummary).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<MessageListResultDto?> GetConversationMessagesPagedAsync(
        Guid conversationId, string userId, bool isAdmin = false,
        int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var conversation = await _repository.GetConversationByIdAsync(conversationId, cancellationToken);
        if (conversation == null) return null;

        if (conversation.UserId != userId && !isAdmin) return null;

        var skip = (page - 1) * pageSize;
        var messages = await _repository.GetMessagesAsync(conversation.Id, skip, pageSize, cancellationToken);

        return new MessageListResultDto
        {
            Data = messages.Select(ToMessageSummary).ToList(),
            TotalCount = conversation.MessageCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)conversation.MessageCount / pageSize)
        };
    }

    /// <inheritdoc />
    public async Task<ConversationStatsDto> GetConversationStatsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var conversations = await _repository.GetAllConversationsAsync(userId, 0, 10000, cancellationToken);
        var totalCount = conversations.Count;
        var activeCount = conversations.Count(c => !c.IsArchived);
        var archivedCount = conversations.Count(c => c.IsArchived);
        var totalMessages = conversations.Sum(c => c.MessageCount);

        return new ConversationStatsDto
        {
            TotalConversations = totalCount,
            ActiveConversations = activeCount,
            ArchivedConversations = archivedCount,
            TotalMessages = totalMessages,
            AverageMessagesPerConversation = totalCount > 0 ? (double)totalMessages / totalCount : 0
        };
    }

    private static MessageSummaryDto ToMessageSummary(Message m) => new()
    {
        Id = m.Id,
        Role = m.Role,
        Content = m.Content != null && m.Content.Length > MaxContentLength
            ? m.Content[..MaxContentLength] + "\n\n[...içerik çok büyük, kısaltıldı...]"
            : m.Content,
        IsContentTruncated = m.Content != null && m.Content.Length > MaxContentLength,
        MessageType = m.MessageTypeValue,
        CreatedAt = m.CreatedAt,
        TokenCount = m.TokenCount,
        MetadataJson = m.MetadataJson
    };

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (_disposed)
            return;

        // Dispose managed resources if any
        _disposed = true;
    }

    #endregion
}