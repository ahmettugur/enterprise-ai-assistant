namespace AI.Application.DTOs.PromptImprovement;

/// <summary>
/// Statistics for prompt improvements
/// </summary>
public class PromptImprovementStatistics
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int AppliedCount { get; set; }
    public int RejectedCount { get; set; }
    public int HighPriorityPendingCount { get; set; }
    public int MediumPriorityPendingCount { get; set; }
    public int LowPriorityPendingCount { get; set; }
}