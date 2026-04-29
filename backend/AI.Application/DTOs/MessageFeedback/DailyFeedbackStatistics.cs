namespace AI.Application.DTOs.MessageFeedback;

/// <summary>
/// Daily feedback statistics for trend charts
/// </summary>
public class DailyFeedbackStatistics
{
    public DateTime Date { get; set; }
    public int PositiveFeedbacks { get; set; }
    public int NegativeFeedbacks { get; set; }
    public int TotalFeedbacks => PositiveFeedbacks + NegativeFeedbacks;
    public double SatisfactionRate => TotalFeedbacks > 0 
        ? Math.Round((double)PositiveFeedbacks / TotalFeedbacks * 100, 2) 
        : 0;
}