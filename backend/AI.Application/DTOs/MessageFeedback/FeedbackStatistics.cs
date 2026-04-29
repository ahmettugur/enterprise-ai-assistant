namespace AI.Application.DTOs.MessageFeedback;

/// <summary>
/// Feedback statistics model
/// </summary>
public class FeedbackStatistics
{
    public int TotalFeedbacks { get; set; }
    public int PositiveFeedbacks { get; set; }
    public int NegativeFeedbacks { get; set; }
    public double SatisfactionRate => TotalFeedbacks > 0 
        ? Math.Round((double)PositiveFeedbacks / TotalFeedbacks * 100, 2) 
        : 0;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}