using AI.Application.DTOs;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// Dedicated query service for conversation metadata.
/// CQRS separation: Query responsibility ayrıştırıldı — Command ops IConversationRepository'de kalır.
/// </summary>
public class ConversationQueryService : IConversationQueryService
{
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<ConversationQueryService> _logger;

    public ConversationQueryService(
        ChatDbContext dbContext,
        ILogger<ConversationQueryService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConversationMetadata?> GetConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(conversationId, out var guid))
            {
                _logger.LogWarning("Invalid ConversationId format: {ConversationId}", conversationId);
                return null;
            }

            var conversation = await _dbContext.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == guid, cancellationToken);

            return conversation != null
                ? ConversationMetadata.FromConversation(conversation)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation metadata - ConversationId: {ConversationId}", conversationId);
            throw;
        }
    }
}
