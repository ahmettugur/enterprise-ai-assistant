using AI.Domain.Common;
using AI.Domain.Conversations;
using AI.Domain.Enums;
using AI.Domain.Events;

namespace AI.Domain.Feedback;

/// <summary>
/// Message feedback entity - represents user feedback (thumbs up/down) on AI messages
/// Used for AI quality measurement and continuous improvement
/// </summary>
public sealed class MessageFeedback : AggregateRoot<Guid>
{
    public Guid MessageId { get; private set; }
    public Guid ConversationId { get; private set; }
    public string UserId { get; private set; } = null!;
    public FeedbackType Type { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Indicates if this feedback has been analyzed for AI improvement
    /// </summary>
    public bool IsAnalyzed { get; private set; }

    /// <summary>
    /// When the feedback was analyzed
    /// </summary>
    public DateTime? AnalyzedAt { get; private set; }

    /// <summary>
    /// Denormalized message content — repository tarafından populate edilir.
    /// Cross-aggregate navigation property yerine kullanılır.
    /// </summary>
    public string? MessageContent { get; internal set; }

    // Navigation properties — EF Core ve query filter için gerekli.
    // internal: Cross-aggregate referans dışarıdan erişilemez.
    internal Message Message { get; private set; } = null!;
    internal Conversation Conversation { get; private set; } = null!;

    // EF Core constructor
    private MessageFeedback()
    {
    }

    /// <summary>
    /// Factory method to create a new feedback
    /// </summary>
    public static MessageFeedback Create(
        Guid messageId,
        Guid conversationId,
        string userId,
        FeedbackType type,
        string? comment = null)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("MessageId cannot be empty", nameof(messageId));

        if (conversationId == Guid.Empty)
            throw new ArgumentException("ConversationId cannot be empty", nameof(conversationId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        var feedback = new MessageFeedback
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ConversationId = conversationId,
            UserId = userId,
            Type = type,
            Comment = comment?.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsAnalyzed = false,
            AnalyzedAt = null
        };

        feedback.AddDomainEvent(new FeedbackSubmittedEvent(
            feedback.Id, messageId, conversationId, type.ToString()));

        return feedback;
    }

    /// <summary>
    /// Update the feedback comment
    /// </summary>
    public void UpdateComment(string? comment)
    {
        Comment = comment?.Trim();
    }

    /// <summary>
    /// Mark the feedback as analyzed for AI improvement
    /// </summary>
    public void MarkAsAnalyzed()
    {
        IsAnalyzed = true;
        AnalyzedAt = DateTime.UtcNow;
    }
}

