namespace AI.Application.DTOs;

/// <summary>
/// HistoryEndpoints conversation listesi sonucu
/// </summary>
public class ConversationListResultDto
{
    public List<ConversationWithMessagesDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Conversation + mesaj özeti (liste görünümü)
/// </summary>
public class ConversationWithMessagesDto
{
    public Guid Id { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Title { get; set; }
    public int? MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsArchived { get; set; }
    public List<MessageSummaryDto> Messages { get; set; } = [];
}

/// <summary>
/// Mesaj özeti (content truncation dahil)
/// </summary>
public class MessageSummaryDto
{
    public Guid Id { get; set; }
    public string? Role { get; set; }
    public string? Content { get; set; }
    public bool IsContentTruncated { get; set; }
    public string? MessageType { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? TokenCount { get; set; }
    public string? MetadataJson { get; set; }
}

/// <summary>
/// Conversation detay sonucu
/// </summary>
public class ConversationDetailResultDto
{
    public ConversationDto Conversation { get; set; } = null!;
    public List<MessageSummaryDto> Messages { get; set; } = [];
}

/// <summary>
/// Mesaj listesi sonucu (sayfalı)
/// </summary>
public class MessageListResultDto
{
    public List<MessageSummaryDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Conversation istatistikleri
/// </summary>
public class ConversationStatsDto
{
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int ArchivedConversations { get; set; }
    public int TotalMessages { get; set; }
    public double AverageMessagesPerConversation { get; set; }
}
