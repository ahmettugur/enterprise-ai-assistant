using AI.Application.DTOs.FeedbackAnalysis;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Use Case for analyzing negative feedbacks and generating improvement suggestions
/// </summary>
public interface IFeedbackAnalysisUseCase
{
    /// <summary>
    /// Analyze pending negative feedbacks and generate improvement suggestions
    /// </summary>
    Task<FeedbackAnalysisResult> AnalyzePendingFeedbacksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest analysis report
    /// </summary>
    Task<FeedbackAnalysisResult?> GetLatestAnalysisAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get analysis history (returns DTOs, not domain entities)
    /// </summary>
    Task<List<FeedbackAnalysisResult>> GetAnalysisHistoryAsync(int limit = 10, CancellationToken cancellationToken = default);
}
