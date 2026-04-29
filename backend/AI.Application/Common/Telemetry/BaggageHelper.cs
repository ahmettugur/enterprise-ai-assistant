using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace AI.Application.Common.Telemetry;

/// <summary>
/// Helper class for managing baggage items (context metadata)
/// </summary>
public static class BaggageHelper
{
    public const string UserIdBaggage = "user_id";
    public const string ConversationIdBaggage = "conversation_id";
    public const string RequestIdBaggage = "request_id";
    public const string OperationNameBaggage = "operation_name";
    public const string DocumentIdBaggage = "document_id";
    public const string ChunkCountBaggage = "chunk_count";
    public const string EmbeddingModelBaggage = "embedding_model";
    public const string VectorSizeBaggage = "vector_size";
    public const string SimilarityScoreBaggage = "similarity_score";

    /// <summary>
    /// Set baggage item for distributed tracing context
    /// </summary>
    public static void SetBaggage(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Activity.Current?.AddTag($"baggage.{key}", value);
        }
    }

    /// <summary>
    /// Get baggage item value
    /// </summary>
    public static string? GetBaggage(string key)
    {
        return Activity.Current?.GetTagItem($"baggage.{key}")?.ToString();
    }

    /// <summary>
    /// Set all context-related baggage items from an activity
    /// </summary>
    public static void SetContextBaggage(string? userId = null, string? conversationId = null, string? requestId = null)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            SetBaggage(UserIdBaggage, userId);

        if (!string.IsNullOrWhiteSpace(conversationId))
            SetBaggage(ConversationIdBaggage, conversationId);

        if (!string.IsNullOrWhiteSpace(requestId))
            SetBaggage(RequestIdBaggage, requestId);
    }

    /// <summary>
    /// Set document-related baggage items
    /// </summary>
    public static void SetDocumentBaggage(string? documentId = null, int? chunkCount = null)
    {
        if (!string.IsNullOrWhiteSpace(documentId))
            SetBaggage(DocumentIdBaggage, documentId);

        if (chunkCount.HasValue)
            SetBaggage(ChunkCountBaggage, chunkCount.Value.ToString());
    }

    /// <summary>
    /// Set embedding-related baggage items
    /// </summary>
    public static void SetEmbeddingBaggage(string? model = null, int? vectorSize = null)
    {
        if (!string.IsNullOrWhiteSpace(model))
            SetBaggage(EmbeddingModelBaggage, model);

        if (vectorSize.HasValue)
            SetBaggage(VectorSizeBaggage, vectorSize.Value.ToString());
    }

    /// <summary>
    /// Set search-related baggage items
    /// </summary>
    public static void SetSearchBaggage(float? similarityScore = null)
    {
        if (similarityScore.HasValue && similarityScore.Value >= 0)
            SetBaggage(SimilarityScoreBaggage, similarityScore.Value.ToString("F4"));
    }

    /// <summary>
    /// Extract context from request headers and set baggage
    /// </summary>
    
    public static void ExtractContextFromHeaders(IHeaderDictionary headers)
    {
        var userId = headers["X-User-Id"].FirstOrDefault();
        var conversationId = headers["X-Conversation-Id"].FirstOrDefault();
        var requestId = headers["X-Request-Id"].FirstOrDefault();

        SetContextBaggage(userId, conversationId, requestId);
    }

    /// <summary>
    /// Add baggage items as tags to an activity
    /// </summary>
    public static void AddBaggageToActivity(Activity? activity)
    {
        if (activity == null)
            return;

        // Tags are already added via SetBaggage method to Activity.Current
        // This method is kept for compatibility
    }
}
