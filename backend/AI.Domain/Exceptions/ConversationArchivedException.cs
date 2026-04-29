namespace AI.Domain.Exceptions;

/// <summary>
/// Arşivlenmiş bir conversation üzerinde işlem yapılmaya çalışıldığında fırlatılır.
/// </summary>
public sealed class ConversationArchivedException : DomainException
{
    public Guid ConversationId { get; }

    public ConversationArchivedException(Guid conversationId)
        : base("CONVERSATION_ARCHIVED",
               $"Conversation '{conversationId}' arşivlenmiş durumda. Mesaj eklenemez.")
    {
        ConversationId = conversationId;
    }
}
