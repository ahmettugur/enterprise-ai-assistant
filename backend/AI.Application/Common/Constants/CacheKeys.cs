namespace AI.Application.Common.Constants;

/// <summary>
/// Cache key builder for consistent cache key generation
/// Tüm cache key'leri merkezi olarak yönetilir, tutarlılık sağlanır
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "chat";

    /// <summary>
    /// ChatHistory için cache key
    /// Format: chat:history:{conversationId}:full
    /// </summary>
    public static string ChatHistory(string conversationId)
        => $"{Prefix}:history:{conversationId}:full";

    /// <summary>
    /// Conversation metadata için cache key
    /// Format: chat:metadata:{conversationId}
    /// </summary>
    public static string ConversationMetadata(string conversationId)
        => $"{Prefix}:metadata:{conversationId}";

    /// <summary>
    /// Conversation DTO için cache key
    /// Format: chat:dto:{conversationId}
    /// </summary>
    public static string ConversationDto(string conversationId)
        => $"{Prefix}:dto:{conversationId}";

    /// <summary>
    /// Sayfalanmış mesajlar için cache key
    /// Format: chat:messages:{conversationId}:page:{pageNumber}
    /// </summary>
    public static string MessagesPage(string conversationId, int pageNumber)
        => $"{Prefix}:messages:{conversationId}:page:{pageNumber}";

    /// <summary>
    /// Kullanıcının conversation listesi için cache key
    /// Format: chat:user:{userId}:conversations
    /// </summary>
    public static string UserConversations(string userId)
        => $"{Prefix}:user:{userId}:conversations";

    /// <summary>
    /// Conversation istatistikleri için cache key
    /// Format: chat:stats:{conversationId}
    /// </summary>
    public static string ConversationStats(string conversationId)
        => $"{Prefix}:stats:{conversationId}";

    /// <summary>
    /// Key'in bu servis tarafından oluşturulup oluşturulmadığını kontrol eder
    /// </summary>
    public static bool IsOwnedKey(string key)
        => key.StartsWith($"{Prefix}:", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Key'den conversation ID'yi çıkarır (mümkünse)
    /// </summary>
    public static string? ExtractConversationId(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var parts = key.Split(':');
        // chat:history:{conversationId}:full veya chat:metadata:{conversationId}
        if (parts.Length >= 3)
            return parts[2];

        return null;
    }
}
