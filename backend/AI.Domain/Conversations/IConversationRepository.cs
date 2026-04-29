


namespace AI.Domain.Conversations;

/// <summary>
/// Repository interface for chat history persistence — Domain sözleşmesi.
/// Concrete implementation Infrastructure'da yaşar.
/// </summary>
public interface IConversationRepository
{
    // Conversation Operations
    Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetAllConversationsAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetAllConversationsWithMessagesAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<Conversation> CreateConversationAsync(string connectionId, string userId, string? title = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<int> GetActiveConversationCountAsync(CancellationToken cancellationToken = default);
    Task<int> ClearAllConversationsAsync(CancellationToken cancellationToken = default);
    Task<bool> ConversationExistsAsync(string connectionId, CancellationToken cancellationToken = default);

    // Message Operations (read-only — message creation goes through Conversation aggregate root)
    Task<List<Message>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<List<Message>> GetMessagesAsync(Guid conversationId, int skip, int take, CancellationToken cancellationToken = default);
    Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent messages with limit - optimized for sliding window cache
    /// </summary>
    Task<List<Message>> GetRecentMessagesAsync(Guid conversationId, int limit = 50, CancellationToken cancellationToken = default);

    Task<int> RemoveMessagesByTypeAsync(Guid conversationId, string messageType, CancellationToken cancellationToken = default);

    // System Prompt Operations
    Task ReplaceSystemPromptAsync(Guid conversationId, string newSystemPrompt, CancellationToken cancellationToken = default);

    // Conversation Update Operations
    Task UpdateConversationTitleAsync(Guid conversationId, string newTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked entity olarak conversation getirir — aggregate root üzerinden değişiklik yapılacaksa kullanılır.
    /// </summary>
    Task<Conversation?> GetConversationForUpdateAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregate root state'ini (conversation + yeni mesajlar) persist eder.
    /// Message ekleme sadece conversation.AddMessage() üzerinden yapılmalıdır.
    /// </summary>
    Task SaveConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
}
