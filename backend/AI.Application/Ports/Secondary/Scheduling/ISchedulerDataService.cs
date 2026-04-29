using AI.Domain.Scheduling;
using AI.Domain.Feedback;

namespace AI.Application.Ports.Secondary.Scheduling;

/// <summary>
/// Data service interface for background jobs - isolates scheduler from infrastructure
/// </summary>
public interface ISchedulerDataService
{
    /// <summary>
    /// Gets feedback entries pending analysis
    /// </summary>
    Task<List<MessageFeedback>> GetFeedbacksPendingAnalysisAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks feedback entries as analyzed
    /// </summary>
    Task MarkFeedbacksAsAnalyzedAsync(IEnumerable<Guid> feedbackIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a feedback analysis report
    /// </summary>
    Task<FeedbackAnalysisReport> SaveAnalysisReportAsync(FeedbackAnalysisReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled reports due for execution
    /// </summary>
    Task<List<ScheduledReport>> GetDueReportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last run timestamp of a scheduled report
    /// </summary>
    Task UpdateScheduledReportLastRunAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active scheduled reports
    /// </summary>
    Task<List<ScheduledReport>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
