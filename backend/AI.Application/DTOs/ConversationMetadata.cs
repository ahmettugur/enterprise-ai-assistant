using AI.Domain.Conversations;

namespace AI.Application.DTOs;

/// <summary>
/// Conversation metadata for caching and quick access
/// Application-layer DTO — Domain'de olmamalı çünkü public setter'lar içerir
/// </summary>
public sealed class ConversationMetadata
{
    public Guid ConversationId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public bool IsArchived { get; set; }

    public static ConversationMetadata FromConversation(Conversation conversation)
    {
        return new ConversationMetadata
        {
            ConversationId = conversation.Id,
            ConnectionId = conversation.ConnectionId,
            UserId = conversation.UserId,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            LastMessageAt = conversation.LastMessageAt,
            MessageCount = conversation.MessageCount,
            IsArchived = conversation.IsArchived
        };
    }
}
