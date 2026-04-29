
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;


namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Context özetleme Use Case - Primary Port
/// Uzun konuşma geçmişlerini özetler
/// Token tasarrufu sağlar ve context window limitlerini aşmayı önler
/// </summary>
public interface IContextSummarizationUseCase
{
    /// <summary>
    /// Konuşma geçmişini özetler ve sliding window ile birleştirir
    /// </summary>
    Task<ChatHistory> GetSummarizedChatHistoryAsync(
        Guid conversationId,
        ChatHistory fullHistory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Token sayısına göre özetleme gerekip gerekmediğini kontrol eder
    /// </summary>
    bool RequiresSummarization(ChatHistory history);

    /// <summary>
    /// Mesaj listesini özetler
    /// </summary>
    Task<string> SummarizeMessagesAsync(
        IEnumerable<ChatMessageContent> messages,
        CancellationToken cancellationToken = default);
}
