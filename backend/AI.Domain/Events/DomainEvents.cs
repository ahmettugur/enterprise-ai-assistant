using AI.Domain.Common;

namespace AI.Domain.Events;

/// <summary>
/// Yeni conversation oluşturulduğunda
/// </summary>
public sealed record ConversationCreatedEvent(
    Guid ConversationId,
    string ConnectionId,
    string? UserId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Conversation'a yeni mesaj eklendiğinde
/// </summary>
public sealed record MessageAddedEvent(
    Guid ConversationId,
    Guid MessageId,
    string Role) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Conversation arşivlendiğinde
/// </summary>
public sealed record ConversationArchivedEvent(
    Guid ConversationId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Yeni kullanıcı oluşturulduğunda
/// </summary>
public sealed record UserCreatedEvent(
    string UserId,
    string Email,
    string AuthenticationSource) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Kullanıcı hesabı kilitlendiğinde
/// </summary>
public sealed record UserLockedOutEvent(
    string UserId,
    int FailedAttempts,
    DateTime LockoutEnd) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Yeni geri bildirim oluşturulduğunda
/// </summary>
public sealed record FeedbackSubmittedEvent(
    Guid FeedbackId,
    Guid MessageId,
    Guid ConversationId,
    string FeedbackType) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

