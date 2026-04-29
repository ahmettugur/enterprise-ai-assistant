using AI.Domain.Conversations;

namespace AI.Application.DTOs;

/// <summary>
/// Conversation DTO for caching - lightweight representation without navigation properties
/// </summary>
public class ConversationDto
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// SignalR connection ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// User ID (optional)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Conversation title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Last message timestamp
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Total message count
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Archive status
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Convert from Conversation entity to DTO
    /// </summary>
    public static ConversationDto FromEntity(Conversation entity)
    {
        return new ConversationDto
        {
            Id = entity.Id,
            ConnectionId = entity.ConnectionId,
            UserId = entity.UserId,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            LastMessageAt = entity.LastMessageAt,
            MessageCount = entity.MessageCount,
            IsArchived = entity.IsArchived
        };
    }

    /// <summary>
    /// Convert from DTO back to Conversation entity (reconstitution from cache)
    /// </summary>
    public Conversation ToEntity()
    {
        return Conversation.Reconstitute(
            Id,
            ConnectionId,
            UserId,
            Title,
            CreatedAt,
            UpdatedAt,
            LastMessageAt,
            MessageCount,
            IsArchived
        );
    }
}
