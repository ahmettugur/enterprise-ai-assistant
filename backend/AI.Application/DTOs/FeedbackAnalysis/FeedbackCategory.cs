namespace AI.Application.DTOs.FeedbackAnalysis;

/// <summary>
/// Category of feedback issues
/// </summary>
public class FeedbackCategory
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public List<string> ExampleComments { get; set; } = new();
}