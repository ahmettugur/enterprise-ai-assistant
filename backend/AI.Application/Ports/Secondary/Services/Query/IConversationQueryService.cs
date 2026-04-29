using AI.Application.DTOs;

namespace AI.Application.Ports.Secondary.Services.Query;

/// <summary>
/// Application-layer query service for conversation metadata.
/// DTO döndürdüğü için Domain'de değil, Application'da kalır.
/// </summary>
public interface IConversationQueryService
{
    Task<ConversationMetadata?> GetConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default);
}
