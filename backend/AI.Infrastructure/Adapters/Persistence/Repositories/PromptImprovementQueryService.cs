using AI.Application.DTOs.PromptImprovement;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// Dedicated query service for prompt improvement statistics.
/// CQRS separation: Query responsibility ayrıştırıldı — Command ops IFeedbackAnalysisReportRepository'de kalır.
/// </summary>
public class PromptImprovementQueryService : IPromptImprovementQueryService
{
    private readonly ChatDbContext _dbContext;

    public PromptImprovementQueryService(ChatDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PromptImprovementStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var improvements = await _dbContext.PromptImprovements
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PromptImprovementStatistics
        {
            TotalCount = improvements.Count,
            PendingCount = improvements.Count(p => p.Status == PromptImprovementStatus.Pending),
            UnderReviewCount = improvements.Count(p => p.Status == PromptImprovementStatus.UnderReview),
            AppliedCount = improvements.Count(p => p.Status == PromptImprovementStatus.Applied),
            RejectedCount = improvements.Count(p => p.Status == PromptImprovementStatus.Rejected),
            HighPriorityPendingCount = improvements.Count(p => p.Status == PromptImprovementStatus.Pending && p.Priority == "High"),
            MediumPriorityPendingCount = improvements.Count(p => p.Status == PromptImprovementStatus.Pending && p.Priority == "Medium"),
            LowPriorityPendingCount = improvements.Count(p => p.Status == PromptImprovementStatus.Pending && p.Priority == "Low")
        };
    }
}
