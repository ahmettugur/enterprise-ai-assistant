using AI.Domain.Conversations;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IConversationRepository (Command side).
/// Query responsibility → InMemoryConversationQueryService (CQRS separation).
/// </summary>
public sealed class InMemoryConversationRepository : IConversationRepository, IDisposable
{
    private readonly ConcurrentDictionary<string, Conversation> _conversationsByConnectionId;
    private readonly ConcurrentDictionary<Guid, Conversation> _conversationsById;
    private readonly ConcurrentDictionary<Guid, List<Message>> _messagesByConversationId;
    private readonly ILogger<InMemoryConversationRepository> _logger;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public InMemoryConversationRepository(ILogger<InMemoryConversationRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _conversationsByConnectionId = new ConcurrentDictionary<string, Conversation>();
        _conversationsById = new ConcurrentDictionary<Guid, Conversation>();
        _messagesByConversationId = new ConcurrentDictionary<Guid, List<Message>>();
        _semaphore = new SemaphoreSlim(1, 1);
    }

    #region Conversation Operations




    public Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _conversationsById.TryGetValue(conversationId, out var conversation);
            return Task.FromResult(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation by Id: {ConversationId}", conversationId);
            throw;
        }
    }

    public Task<List<Conversation>> GetAllConversationsAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);

            var conversations = _conversationsByConnectionId.Values
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            return Task.FromResult(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all conversations for user: {UserId}", userId);
            throw;
        }
    }

    public Task<List<Conversation>> GetAllConversationsWithMessagesAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);

            var conversations = _conversationsByConnectionId.Values
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            // Her conversation için mesajları yükle - Optimized
            foreach (var conversation in conversations)
            {
                if (_messagesByConversationId.TryGetValue(conversation.Id, out var messages))
                {
                    var sortedMessages = messages
                        .Where(m => !m.IsDeleted)
                        .OrderBy(m => m.CreatedAt)
                        .ToList();

                    // AddExistingMessage kullanarak IReadOnlyCollection'a mesaj ekle
                    foreach (var message in sortedMessages)
                    {
                        conversation.AddExistingMessage(message);
                    }
                }
            }

            return Task.FromResult(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all conversations with messages");
            throw;
        }
    }

    public async Task<Conversation> CreateConversationAsync(string connectionId, string userId, string? title = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var conversation = Conversation.Create(connectionId, userId, title);

            _conversationsByConnectionId[connectionId] = conversation;
            _conversationsById[conversation.Id] = conversation;
            _messagesByConversationId[conversation.Id] = new List<Message>();

            _logger.LogInformation("Conversation created - Id: {ConversationId}, ConnectionId: {ConnectionId}",
                conversation.Id, connectionId);

            return conversation;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_conversationsById.TryRemove(conversationId, out var conversation))
            {
                _logger.LogWarning("Conversation not found for deletion - ConversationId: {ConversationId}", conversationId);
                return false;
            }

            _conversationsByConnectionId.TryRemove(conversation.ConnectionId, out _);
            _messagesByConversationId.TryRemove(conversationId, out _);

            _logger.LogInformation("Conversation deleted - ConversationId: {ConversationId}", conversationId);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<int> GetActiveConversationCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = _conversationsByConnectionId.Count(c => !c.Value.IsArchived);
            return Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active conversation count");
            throw;
        }
    }

    public async Task<int> ClearAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var count = _conversationsByConnectionId.Count;

            _conversationsByConnectionId.Clear();
            _conversationsById.Clear();
            _messagesByConversationId.Clear();

            _logger.LogWarning("All conversations cleared - Count: {Count}", count);
            return count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<bool> ConversationExistsAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_conversationsByConnectionId.ContainsKey(connectionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking conversation existence - ConnectionId: {ConnectionId}", connectionId);
            throw;
        }
    }

    #endregion

    #region Message Operations

    public Task<List<Message>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // SemaphoreSlim gereksiz - ConcurrentDictionary zaten thread-safe
            // Read operasyonlarında lock almaya gerek yok
            if (!_messagesByConversationId.TryGetValue(conversationId, out var messages))
            {
                return Task.FromResult(new List<Message>());
            }

            return Task.FromResult(messages
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    public Task<List<Message>> GetMessagesAsync(Guid conversationId, int skip, int take, CancellationToken cancellationToken = default)
    {
        try
        {
            // Read operasyonu - lock gereksiz
            if (!_messagesByConversationId.TryGetValue(conversationId, out var messages))
            {
                return Task.FromResult(new List<Message>());
            }

            return Task.FromResult(messages
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated messages - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    public Task<List<Message>> GetRecentMessagesAsync(Guid conversationId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_messagesByConversationId.TryGetValue(conversationId, out var messages))
            {
                return Task.FromResult(new List<Message>());
            }

            // Son N mesajı getir
            return Task.FromResult(messages
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt)
                .ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent messages - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }



    public async Task<int> RemoveMessagesByTypeAsync(Guid conversationId, string messageType, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_messagesByConversationId.TryGetValue(conversationId, out var messages))
                return 0;

            var messagesToRemove = messages
                .Where(m => m.MessageTypeValue == messageType && !m.IsDeleted)
                .ToList();

            if (!messagesToRemove.Any())
                return 0;

            foreach (var message in messagesToRemove)
            {
                message.SoftDelete();
            }

            if (_conversationsById.TryGetValue(conversationId, out var conversation))
            {
                conversation.UpdateTimestamp();
            }

            _logger.LogInformation("Messages removed by type - ConversationId: {ConversationId}, Type: {MessageType}, Count: {Count}",
                conversationId, messageType, messagesToRemove.Count);

            return messagesToRemove.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = _messagesByConversationId.Values
                .SelectMany(messages => messages)
                .FirstOrDefault(m => m.Id == messageId && !m.IsDeleted);

            return Task.FromResult(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message - MessageId: {MessageId}", messageId);
            throw;
        }
    }

    #endregion

    #region System Prompt Operations

    public async Task ReplaceSystemPromptAsync(Guid conversationId, string newSystemPrompt, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_messagesByConversationId.TryGetValue(conversationId, out var messages))
                throw new InvalidOperationException($"Conversation not found: {conversationId}");

            var systemMessages = messages
                .Where(m => m.MessageTypeValue == "System" && !m.IsDeleted)
                .ToList();

            foreach (var systemMessage in systemMessages)
            {
                systemMessage.SoftDelete();
            }

            if (!_conversationsById.TryGetValue(conversationId, out var conversation))
                throw new InvalidOperationException($"Conversation not found: {conversationId}");

            // DDD: Message creation through Aggregate Root
            var newSystemMessage = conversation.AddMessage("system", newSystemPrompt, "System");
            messages.Insert(0, newSystemMessage);

            // Not: AddMessage zaten UpdatedAt'i güncelliyor, UpdateTimestamp gereksiz
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion


    #region Conversation Update Operations

    public async Task UpdateConversationTitleAsync(Guid conversationId, string newTitle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_conversationsById.TryGetValue(conversationId, out var conversation))
            {
                _logger.LogWarning("Conversation not found for title update - ConversationId: {ConversationId}", conversationId);
                throw new InvalidOperationException($"Conversation with Id {conversationId} not found");
            }

            conversation.UpdateTitle(newTitle);
            _logger.LogInformation("Conversation title updated successfully - ConversationId: {ConversationId}, NewTitle: {Title}",
                conversationId, newTitle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation title - ConversationId: {ConversationId}", conversationId);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _semaphore?.Dispose();
        _conversationsByConnectionId?.Clear();
        _conversationsById?.Clear();
        _messagesByConversationId?.Clear();

        _disposed = true;
    }

    #endregion

    #region Aggregate Root Persistence

    public Task<Conversation?> GetConversationForUpdateAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _conversationsById.TryGetValue(conversationId, out var conversation);
        return Task.FromResult(conversation);
    }

    public async Task SaveConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Conversation'daki mesajları in-memory store'a senkronize et
            if (!_messagesByConversationId.TryGetValue(conversation.Id, out var messages))
            {
                messages = new List<Message>();
                _messagesByConversationId[conversation.Id] = messages;
            }

            // Yeni mesajları ekle (mevcut olmayan)
            foreach (var message in conversation.Messages)
            {
                if (!messages.Any(m => m.Id == message.Id))
                {
                    messages.Add(message);
                }
            }

            _conversationsById[conversation.Id] = conversation;

            _logger.LogDebug("Conversation saved - ConversationId: {ConversationId}", conversation.Id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion
}