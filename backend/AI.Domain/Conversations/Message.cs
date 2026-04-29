using AI.Domain.Common;

namespace AI.Domain.Conversations;

/// <summary>
/// Message entity - represents a single chat message
/// </summary>
public sealed class Message : Entity<Guid>
{
    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = null!; // "user", "assistant", "system" - EF Core will set via reflection
    public string Content { get; private set; } = null!; // EF Core will set via reflection
    public DateTime CreatedAt { get; private set; }
    public int? TokenCount { get; private set; }
    public string MessageTypeValue { get; private set; } = null!; // "System", "User", "Assistant", "Temporary" - EF Core will set via reflection
    public string? MetadataJson { get; private set; } // JSON string for flexibility
    public DateTime? DeletedAt { get; private set; }

    // Navigation property
    public Conversation Conversation { get; private set; } = null!;

    // EF Core constructor - properties will be set via reflection
    private Message()
    {
    }

    // Factory method - internal: Message creation must go through Conversation aggregate root
    internal static Message Create(
        Guid conversationId,
        string role,
        string content,
        string messageType,
        int? tokenCount = null,
        string? metadataJson = null)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        if (string.IsNullOrWhiteSpace(messageType))
            throw new ArgumentException("MessageType cannot be empty", nameof(messageType));

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role.ToLowerInvariant(),
            Content = content,
            CreatedAt = DateTime.UtcNow,
            TokenCount = tokenCount,
            MessageTypeValue = messageType,
            MetadataJson = metadataJson
        };
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
    }

    public bool IsDeleted => DeletedAt.HasValue;
}