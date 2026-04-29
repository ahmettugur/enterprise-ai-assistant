

using AI.Domain.Enums;

namespace AI.Domain.Feedback;

/// <summary>
/// Repository interface for FeedbackAnalysisReport aggregate root.
/// PromptImprovement (child entity) erişimi de bu repository üzerinden yapılır.
/// </summary>
public interface IFeedbackAnalysisReportRepository
{
    // ── Report operations ──
    Task AddAsync(FeedbackAnalysisReport report, CancellationToken cancellationToken = default);
    Task<FeedbackAnalysisReport?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<List<FeedbackAnalysisReport>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<FeedbackAnalysisReport>> GetAllAsync(int? limit = null, CancellationToken cancellationToken = default);

    // ── PromptImprovement (child entity) operations ──
    Task AddImprovementAsync(PromptImprovement improvement, CancellationToken cancellationToken = default);
    Task AddImprovementsAsync(IEnumerable<PromptImprovement> improvements, CancellationToken cancellationToken = default);
    Task<PromptImprovement?> GetImprovementByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PromptImprovement>> GetAllImprovementsAsync(PromptImprovementStatus? status = null, string? priority = null, int? limit = null, CancellationToken cancellationToken = default);
    Task<List<PromptImprovement>> GetImprovementsByReportIdAsync(Guid analysisReportId, CancellationToken cancellationToken = default);
    Task<List<PromptImprovement>> GetPendingImprovementsByPriorityAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task UpdateImprovementAsync(PromptImprovement improvement, CancellationToken cancellationToken = default);
}
