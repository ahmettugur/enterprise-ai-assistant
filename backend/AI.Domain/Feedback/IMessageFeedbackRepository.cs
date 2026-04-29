


namespace AI.Domain.Feedback;

/// <summary>
/// Repository interface for message feedback CRUD operations — Domain sözleşmesi.
/// İstatistik sorguları Application katmanında IFeedbackQueryService'de kalır.
/// </summary>
public interface IMessageFeedbackRepository
{
    Task<MessageFeedback> AddAsync(MessageFeedback feedback, CancellationToken cancellationToken = default);
    Task<MessageFeedback?> GetByMessageAndUserAsync(Guid messageId, string userId, CancellationToken cancellationToken = default);
    Task<List<MessageFeedback>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<List<MessageFeedback>> GetByUserAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<MessageFeedback> UpdateAsync(MessageFeedback feedback, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid feedbackId, CancellationToken cancellationToken = default);
    Task<List<MessageFeedback>> GetNegativeFeedbacksWithCommentsAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<List<MessageFeedback>> GetFeedbacksPendingAnalysisAsync(int take = 50, CancellationToken cancellationToken = default);
    Task MarkAsAnalyzedAsync(IEnumerable<Guid> feedbackIds, CancellationToken cancellationToken = default);
}
