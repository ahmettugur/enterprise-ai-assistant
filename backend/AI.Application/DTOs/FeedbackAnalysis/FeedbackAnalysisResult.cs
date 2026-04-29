namespace AI.Application.DTOs.FeedbackAnalysis;

/// <summary>
/// Result of feedback analysis
/// </summary>
public class FeedbackAnalysisResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public int TotalFeedbacksAnalyzed { get; set; }
    public List<FeedbackCategory> Categories { get; set; } = new();
    public List<ImprovementSuggestion> Suggestions { get; set; } = new();
    public string OverallSummary { get; set; } = string.Empty;
}