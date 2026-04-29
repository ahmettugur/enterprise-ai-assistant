namespace AI.Application.DTOs.FeedbackAnalysis;

/// <summary>
/// Request to add feedback
/// </summary>
public class AddFeedbackRequest
{
    /// <summary>
    /// Feedback type: "positive" or "negative"
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// Optional comment explaining the feedback
    /// </summary>
    public string? Comment { get; set; }
}

/// <summary>
/// Feedback analysis response DTO
/// </summary>
public class FeedbackAnalysisResponseDto
{
    public Guid Id { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public int TotalFeedbacksAnalyzed { get; set; }
    public string OverallSummary { get; set; } = string.Empty;
    public List<CategoryResponseDto> Categories { get; set; } = new();
    public List<SuggestionResponseDto> Suggestions { get; set; } = new();
}

/// <summary>
/// Feedback category response
/// </summary>
public class CategoryResponseDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public List<string> ExampleComments { get; set; } = new();
}

/// <summary>
/// Improvement suggestion response
/// </summary>
public class SuggestionResponseDto
{
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string PromptModification { get; set; } = string.Empty;
}
