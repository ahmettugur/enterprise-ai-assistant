using AI.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// PostgreSQL implementation of IConversationRepository (Command side).
/// Query responsibility → ConversationQueryService (CQRS separation).
/// </summary>
public class PostgreSqlConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<PostgreSqlConversationRepository> _logger;

    public PostgreSqlConversationRepository(
        ChatDbContext dbContext,
        ILogger<PostgreSqlConversationRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Conversation Operations

    public async Task<Conversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // AsNoTracking kullanarak performans artır - mesajları Include etme (gereksiz)
            // Mesajlar sadece gerektiğinde GetMessagesAsync ile ayrı sorgulanmalı
            return await _dbContext.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation by Id: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<List<Conversation>> GetAllConversationsAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);

            return await _dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all conversations for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<Conversation>> GetAllConversationsWithMessagesAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);

            return await _dbContext.Conversations
                .Include(c => c.Messages!.Where(m => m.MessageTypeValue != "Temporary").OrderBy(m => m.CreatedAt))
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all conversations with messages for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<Conversation> CreateConversationAsync(string connectionId, string userId, string? title = null, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(userId);

            // Önce mevcut conversation'ı kontrol et (race condition'ı önlemek için)
            var existingConversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId, cancellationToken);

            if (existingConversation != null)
            {
                _logger.LogDebug("Conversation already exists for ConnectionId: {ConnectionId}, returning existing - Id: {ConversationId}",
                    connectionId, existingConversation.Id);
                return existingConversation;
            }

            var conversation = Conversation.Create(connectionId, userId, title);

            await _dbContext.Conversations.AddAsync(conversation, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Conversation created - Id: {ConversationId}, ConnectionId: {ConnectionId}",
                conversation.Id, connectionId);

            return conversation;
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Unique constraint violation - race condition durumunda mevcut conversation'ı getir
            _logger.LogDebug("Race condition detected for ConnectionId: {ConnectionId}, fetching existing conversation", connectionId);

            var existingConversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId, cancellationToken);

            if (existingConversation != null)
            {
                return existingConversation;
            }

            // Bu duruma düşmemeli, ama yine de loglayalım
            _logger.LogError(ex, "Unexpected state: Unique constraint violation but conversation not found for ConnectionId: {ConnectionId}", connectionId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for ConnectionId: {ConnectionId}", connectionId);
            throw;
        }
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conversation == null)
            {
                _logger.LogWarning("Conversation not found for deletion - ConversationId: {ConversationId}", conversationId);
                return false;
            }

            _dbContext.Conversations.Remove(conversation);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Conversation deleted - ConversationId: {ConversationId}", conversationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<int> GetActiveConversationCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Conversations
                .Where(c => !c.IsArchived)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active conversation count");
            throw;
        }
    }

    public async Task<int> ClearAllConversationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _dbContext.Conversations.CountAsync(cancellationToken);

            // TRUNCATE yerine DELETE kullan - daha güvenli ve constraint'leri kontrol eder
            // Büyük tablolarda batch delete daha performanslı olabilir
            await _dbContext.Database.ExecuteSqlRawAsync(@"
                DELETE FROM messages WHERE conversation_id IN (
                    SELECT id FROM conversations
                )", cancellationToken);

            await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM conversations", cancellationToken);

            _logger.LogWarning("All conversations cleared - Count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all conversations");
            throw;
        }
    }

    public async Task<bool> ConversationExistsAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Conversations
                .AnyAsync(c => c.ConnectionId == connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking conversation existence - ConnectionId: {ConnectionId}", connectionId);
            throw;
        }
    }

    #endregion

    #region Message Operations

    public async Task<List<Message>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<List<Message>> GetMessagesAsync(Guid conversationId, int skip, int take, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated messages - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<List<Message>> GetRecentMessagesAsync(Guid conversationId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            // Optimize: sadece son N mesajı getir - sliding window cache için ideal
            return await _dbContext.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt) // Tekrar doğru sıraya çevir
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent messages - ConversationId: {ConversationId}, Limit: {Limit}", conversationId, limit);
            throw;
        }
    }



    public async Task<int> RemoveMessagesByTypeAsync(Guid conversationId, string messageType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Önce conversation'ı getir (tracking - UpdateTimestamp için gerekli)
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conversation == null)
                return 0;

            // Mesajları no-tracking ile getir (rimov edeceğiz, bu entity'leri update etmeyeceğiz)
            var messagesToRemove = await _dbContext.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId && m.MessageTypeValue == messageType)
                .ToListAsync(cancellationToken);

            if (!messagesToRemove.Any())
                return 0;

            // Detached mesajları remove etmek için, sadece ID'lerine göre silelim
            // ExecuteDeleteAsync daha performanslı ve tracking problemi yoktur
            var deletedCount = await _dbContext.Messages
                .Where(m => m.ConversationId == conversationId && m.MessageTypeValue == messageType)
                .ExecuteDeleteAsync(cancellationToken);

            // Conversation'ı güncelle (bu zaten tracked)
            conversation.UpdateTimestamp();

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Messages removed by type - ConversationId: {ConversationId}, Type: {MessageType}, Count: {Count}",
                conversationId, messageType, deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing messages by type - ConversationId: {ConversationId}, Type: {MessageType}",
                conversationId, messageType);
            throw;
        }
    }

    public async Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
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
        try
        {
            // Get conversation and validate it exists
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conversation == null)
                throw new InvalidOperationException($"Conversation not found: {conversationId}");

            // Soft delete existing system messages
            await _dbContext.Messages
                .Where(m => m.ConversationId == conversationId && m.MessageTypeValue == "System" && m.DeletedAt == null)
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.DeletedAt, DateTime.UtcNow), cancellationToken);

            // DDD: Message creation through Aggregate Root
            var systemMessage = conversation.AddMessage("system", newSystemPrompt, "System");

            // Explicitly track: EF Core can't detect additions to the private readonly _messages backing field
            _dbContext.Messages.Add(systemMessage);

            conversation.UpdateTimestamp();

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("System prompt replaced successfully - ConversationId: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing system prompt - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    #endregion


    #region Conversation Update Operations

    public async Task UpdateConversationTitleAsync(Guid conversationId, string newTitle, CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

            if (conversation == null)
            {
                _logger.LogWarning("Conversation not found for title update - ConversationId: {ConversationId}", conversationId);
                throw new InvalidOperationException($"Conversation with Id {conversationId} not found");
            }

            conversation.UpdateTitle(newTitle);

            // Mark entity as modified to ensure EF Core tracks the change
            _dbContext.Entry(conversation).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            var rowsAffected = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Conversation title updated - ConversationId: {ConversationId}, NewTitle: {Title}, RowsAffected: {RowsAffected}",
                conversationId, newTitle, rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation title - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    #endregion

    #region Aggregate Root Persistence

    public async Task<Conversation?> GetConversationForUpdateAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tracking enabled — aggregate root üzerinden yapılacak değişiklikler için
            return await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation for update - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task SaveConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        try
        {
            // EF Core change tracker, conversation entity'sindeki değişiklikleri algılar.
            // Ancak private backing field '_messages' üzerinden eklenen yeni mesajları
            // otomatik algılayamaz — explicit track gerekir.
            foreach (var message in conversation.Messages)
            {
                var entry = _dbContext.Entry(message);
                if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    _dbContext.Messages.Add(message);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Conversation saved - ConversationId: {ConversationId}", conversation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation - ConversationId: {ConversationId}", conversation.Id);
            throw;
        }
    }

    #endregion
}