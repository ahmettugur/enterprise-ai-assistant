using AI.Application.DTOs;
using AI.Application.DTOs.History;
using AI.Application.DTOs.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Chat history yönetimi için ana Use Case interface
/// </summary>
public interface IConversationUseCase
{
    /// <summary>
    /// Belirtilen conversation ID için chat history getirir
    /// </summary>
    /// <param name="request">Chat request</param>
    /// <param name="includeDbResponses">IsDbResponse metadata'sı true olan mesajları dahil et (default: false - filtreler)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Chat history instance</returns>
    Task<ChatHistory> GetChatHistoryAsync(ChatRequest request, bool includeDbResponses = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// ConversationID'nin geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="conversationId">Kontrol edilecek konuşma kimliği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Geçerli ise true, değilse false</returns>
    Task<bool> IsValidConversationIdAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen conversation ID'nin chat history'sini temizler
    /// </summary>
    /// <param name="conversationId">Temizlenecek konuşma kimliği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Başarılı ise true, değilse false</returns>
    Task<bool> RemoveConversationHistoryAsync(string conversationId, CancellationToken cancellationToken = default);


    // Mesaj Ekleme Metodları
    /// <summary>
    /// System mesajı ekler
    /// </summary>
    /// <param name="message">Eklenecek mesaj</param>
    /// <param name="metadata">Opsiyonel metadata</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task<AddMessageResultDto> AddSystemMessageAsync(ChatRequest request, string message, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// User mesajı ekler
    /// </summary>
    /// <param name="message">Eklenecek mesaj</param>
    /// <param name="messageType">Mesaj tipi</param>
    /// <param name="metadata">Opsiyonel metadata</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task<AddMessageResultDto> AddUserMessageAsync(ChatRequest request, string message, MessageType messageType = MessageType.User, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assistant mesajı ekler
    /// </summary>
    /// <param name="message">Eklenecek mesaj</param>
    /// <param name="metadata">Opsiyonel metadata</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task<AddMessageResultDto> AddAssistantMessageAsync(ChatRequest request, string message, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// System prompt'u değiştirir
    /// </summary>
    /// <param name="newSystemPrompt">Yeni system prompt</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task<ConversationDto> ReplaceSystemPromptAsync(ChatRequest request, string newSystemPrompt, CancellationToken cancellationToken = default);

    // Metadata Yönetimi
    /// <summary>
    /// Belirtilen tipte mesajları kaldırır
    /// </summary>
    /// <param name="messageType">Kaldırılacak mesaj tipi</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task RemoveMessagesByTypeAsync(ChatRequest request, MessageType messageType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat metadata'sını getirir
    /// </summary>
    /// <param name="conversationId">Konuşma kimliği</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Metadata dictionary</returns>
    Task<Dictionary<string, object>?> GetChatMetadataAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conversation başlığını günceller
    /// </summary>
    /// <param name="conversationId">Güncellenecek konuşma kimliği</param>
    /// <param name="newTitle">Yeni başlık</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Güncellenmiş ConversationDto</returns>
    Task<ConversationDto> UpdateConversationTitleAsync(string conversationId, string newTitle, CancellationToken cancellationToken = default);

    // ── History Query Methods ─────────────────────────────────────
    // HistoryEndpoints tarafından kullanılır — doğrudan repository erişimi yerine use case üzerinden.

    /// <summary>
    /// Kullanıcının tüm conversation'larını mesajları ile birlikte sayfalı getirir
    /// </summary>
    Task<ConversationListResultDto> GetConversationsWithMessagesAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir conversation'ın detaylarını (mesajları dahil) getirir
    /// </summary>
    Task<ConversationDetailResultDto?> GetConversationDetailAsync(Guid conversationId, string userId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conversation mesajlarını sayfalı getirir
    /// </summary>
    Task<MessageListResultDto?> GetConversationMessagesPagedAsync(Guid conversationId, string userId, bool isAdmin = false, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının conversation istatistiklerini getirir
    /// </summary>
    Task<ConversationStatsDto> GetConversationStatsAsync(string userId, CancellationToken cancellationToken = default);
}
