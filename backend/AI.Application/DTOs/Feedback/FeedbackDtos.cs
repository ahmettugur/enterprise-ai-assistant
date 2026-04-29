namespace AI.Application.DTOs.Feedback;

/// <summary>
/// Feedback DTO
/// </summary>
public class FeedbackDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public string Type { get; set; } = "";
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Feedback statistics DTO
/// </summary>
public class FeedbackStatisticsDto
{
    public int TotalFeedbacks { get; set; }
    public int PositiveFeedbacks { get; set; }
    public int NegativeFeedbacks { get; set; }
    public double SatisfactionRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Dashboard statistics DTO with trends
/// </summary>
public class DashboardStatisticsDto
{
    public int TotalFeedbacks { get; set; }
    public int PositiveFeedbacks { get; set; }
    public int NegativeFeedbacks { get; set; }
    public double SatisfactionRate { get; set; }
    public double TrendChange { get; set; }
    public string TrendDirection { get; set; } = "stable";
    public List<DailyStatDto> DailyStats { get; set; } = [];
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Daily statistics DTO for trend chart
/// </summary>
public class DailyStatDto
{
    public string Date { get; set; } = "";
    public int PositiveCount { get; set; }
    public int NegativeCount { get; set; }
    public double SatisfactionRate { get; set; }
}

/// <summary>
/// Negative feedback DTO for review
/// </summary>
public class NegativeFeedbackDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public string UserId { get; set; } = "";
    public string? Comment { get; set; }
    public string? MessageContent { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAnalyzed { get; set; }
    public DateTime? AnalyzedAt { get; set; }
}
