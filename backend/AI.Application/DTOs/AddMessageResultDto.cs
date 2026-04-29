using AI.Domain.Conversations;

namespace AI.Application.DTOs;

/// <summary>
/// Mesaj ekleme işleminin sonucunu döndüren DTO.
/// Hem eklenen mesajı hem de güncel conversation'ı içerir.
/// </summary>
public sealed class AddMessageResultDto
{
    /// <summary>
    /// Güncel conversation nesnesi
    /// </summary>
    public required Conversation Conversation { get; init; }

    /// <summary>
    /// Eklenen mesaj nesnesi
    /// </summary>
    public required Message Message { get; init; }

    /// <summary>
    /// Eklenen mesajın ID'si (kolay erişim için)
    /// </summary>
    public Guid MessageId => Message.Id;

    /// <summary>
    /// Conversation ID'si (kolay erişim için)
    /// </summary>
    public Guid ConversationId => Conversation.Id;
}
