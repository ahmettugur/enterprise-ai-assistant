using AI.Application.DTOs;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Application.Ports.Secondary.Services.Cache;

/// <summary>
/// Cache service interface for chat history caching
/// Supports distributed caching (Redis) and local caching
/// </summary>
public interface IChatCacheService
{
    // ChatHistory caching
    Task<ChatHistory?> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default);
    Task SetChatHistoryAsync(string conversationId, ChatHistory chatHistory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task InvalidateChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default);

    // Conversation metadata caching
    Task<ConversationMetadata?> GetConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default);
    Task SetConversationMetadataAsync(string conversationId, ConversationMetadata metadata, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task InvalidateConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default);

    // Batch operations
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);

    // Generic cache operations
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    // Health check
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
