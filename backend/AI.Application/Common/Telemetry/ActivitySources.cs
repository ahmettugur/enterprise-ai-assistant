using System.Diagnostics;

namespace AI.Application.Common.Telemetry;

/// <summary>
/// Centralized ActivitySource definitions for business operations
/// </summary>
public static class ActivitySources
{
    /// <summary>
    /// Activity source for document processing operations
    /// </summary>
    public static readonly ActivitySource DocumentProcessing = new(
        "DocumentProcessing.ActivitySource",
        "1.0.0");

    /// <summary>
    /// Activity source for embedding generation operations
    /// </summary>
    public static readonly ActivitySource EmbeddingGeneration = new(
        "EmbeddingGeneration.ActivitySource",
        "1.0.0");

    /// <summary>
    /// Activity source for vector search operations
    /// </summary>
    public static readonly ActivitySource VectorSearch = new(
        "VectorSearch.ActivitySource",
        "1.0.0");

    /// <summary>
    /// Activity source for chat history operations
    /// </summary>
    public static readonly ActivitySource ChatHistory = new(
        "ChatHistory.ActivitySource",
        "1.0.0");

    /// <summary>
    /// Activity source for RAG search operations
    /// </summary>
    public static readonly ActivitySource RagSearch = new(
        "RagSearch.ActivitySource",
        "1.0.0");

    /// <summary>
    /// Activity source for chat operations (main conversation flow)
    /// </summary>
    public static readonly ActivitySource Chat = new(
        "Chat.ActivitySource",
        "1.0.0");
}
