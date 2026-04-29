namespace AI.Application.DTOs.FeedbackAnalysis;

/// <summary>
/// Improvement suggestion based on feedback analysis
/// </summary>
public class ImprovementSuggestion
{
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium"; // High, Medium, Low
    public string PromptModification { get; set; } = string.Empty;
}