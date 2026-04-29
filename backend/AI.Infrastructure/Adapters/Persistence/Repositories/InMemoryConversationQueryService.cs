using AI.Application.DTOs;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Conversations;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IConversationQueryService.
/// CQRS separation: Query responsibility for InMemory mode.
/// Uses the same in-memory data store as InMemoryConversationRepository via shared registration.
/// </summary>
public class InMemoryConversationQueryService : IConversationQueryService
{
    private readonly InMemoryConversationRepository _repository;
    private readonly ILogger<InMemoryConversationQueryService> _logger;

    public InMemoryConversationQueryService(
        InMemoryConversationRepository repository,
        ILogger<InMemoryConversationQueryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

            var conversation = await _repository.GetConversationByIdAsync(guid, cancellationToken);

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
