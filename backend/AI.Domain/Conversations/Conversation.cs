using AI.Domain.Common;
using AI.Domain.Events;
using AI.Domain.Exceptions;

namespace AI.Domain.Conversations;

/// <summary>
/// Conversation aggregate root - domain entity
/// Mesaj oluşturma ve yönetimi bu entity üzerinden yapılmalıdır (DDD Aggregate Root pattern)
/// </summary>
public sealed class Conversation : AggregateRoot<Guid>
{
    public string ConnectionId { get; private set; } = null!; // EF Core will set via reflection
    public string? UserId { get; private set; }
    public string? Title { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public int MessageCount { get; private set; }
    public bool IsArchived { get; private set; }

    // Navigation property - IReadOnlyCollection ile dışarıdan manipülasyon engellenir
    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    // EF Core constructor - properties will be set via reflection
    private Conversation()
    {
    }

    // Internal constructor for Infrastructure reconstitution (EF Core, InMemory repo)
    internal Conversation(Guid id, string connectionId, string? userId, string? title, DateTime createdAt, DateTime updatedAt, DateTime? lastMessageAt, int messageCount, bool isArchived)
    {
        Id = id;
        ConnectionId = connectionId;
        UserId = userId;
        Title = title;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        LastMessageAt = lastMessageAt;
        MessageCount = messageCount;
        IsArchived = isArchived;
    }

    /// <summary>
    /// Reconstitute an existing Conversation from persisted state (cache, DTO, etc.).
    /// DDD Reconstitution Pattern — domain event raise etmez.
    /// </summary>
    public static Conversation Reconstitute(Guid id, string connectionId, string? userId, string? title,
        DateTime createdAt, DateTime updatedAt, DateTime? lastMessageAt, int messageCount, bool isArchived)
    {
        return new Conversation
        {
            Id = id,
            ConnectionId = connectionId,
            UserId = userId,
            Title = title,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            LastMessageAt = lastMessageAt,
            MessageCount = messageCount,
            IsArchived = isArchived
        };
    }

    // Factory method
    public static Conversation Create(string connectionId, string? userId = null, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("ConnectionId cannot be empty", nameof(connectionId));

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            UserId = userId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            MessageCount = 0,
            IsArchived = false
        };

        conversation.AddDomainEvent(new ConversationCreatedEvent(conversation.Id, connectionId, userId));

        return conversation;
    }

    /// <summary>
    /// Aggregate Root üzerinden yeni mesaj oluşturur ve koleksiyona ekler.
    /// DDD invariant koruması: Mesaj sadece bu metod ile oluşturulabilir.
    /// </summary>
    public Message AddMessage(string role, string content, string messageType,
        int? tokenCount = null, string? metadataJson = null)
    {
        if (IsArchived)
            throw new ConversationArchivedException(Id);

        var message = Message.Create(Id, role, content, messageType, tokenCount, metadataJson);
        _messages.Add(message);
        MessageCount++;
        LastMessageAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new MessageAddedEvent(Id, message.Id, role));

        return message;
    }

    /// <summary>
    /// Mevcut bir Message entity'sini koleksiyona ekler (EF Core materialization / repository reconstitution).
    /// internal: Sadece Infrastructure katmanı (repository) erişebilir.
    /// Yeni mesaj oluşturmak için AddMessage(string role, ...) kullanın.
    /// </summary>
    internal void AddExistingMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);
        MessageCount++;
        LastMessageAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ConversationArchivedEvent(Id));
    }

    public void Restore()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Timestamp günceller — sadece Infrastructure repo'larından çağrılır.
    /// </summary>
    internal void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}