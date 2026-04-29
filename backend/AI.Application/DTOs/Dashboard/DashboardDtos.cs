namespace AI.Application.DTOs.Dashboard;

/// <summary>
/// Dashboard overview response DTO
/// </summary>
public class DashboardOverviewDto
{
    public PeriodInfoDto Period { get; set; } = null!;
    public FeedbackStatsDto FeedbackStats { get; set; } = null!;
    public ImprovementStatsDto ImprovementStats { get; set; } = null!;
    public LastAnalysisDto? LastAnalysis { get; set; }
}

/// <summary>
/// Period information DTO
/// </summary>
public class PeriodInfoDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Feedback statistics DTO
/// </summary>
public class FeedbackStatsDto
{
    public int TotalFeedbacks { get; set; }
    public int PositiveFeedbacks { get; set; }
    public int NegativeFeedbacks { get; set; }
    public double SatisfactionRate { get; set; }
    public double SatisfactionTrend { get; set; }
    public double FeedbackTrend { get; set; }
}

/// <summary>
/// Improvement statistics DTO
/// </summary>
public class ImprovementStatsDto
{
    public int TotalImprovements { get; set; }
    public int PendingCount { get; set; }
    public int AppliedCount { get; set; }
    public int RejectedCount { get; set; }
    public int HighPriorityPending { get; set; }
}

/// <summary>
/// Last analysis summary DTO
/// </summary>
public class LastAnalysisDto
{
    public Guid Id { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public int FeedbacksAnalyzed { get; set; }
    public string Summary { get; set; } = "";
}

/// <summary>
/// Feedback trends response DTO
/// </summary>
public class FeedbackTrendsDto
{
    public PeriodInfoDto Period { get; set; } = null!;
    public List<DailyFeedbackDataDto> DailyData { get; set; } = [];
}

/// <summary>
/// Daily feedback data DTO
/// </summary>
public class DailyFeedbackDataDto
{
    public DateTime Date { get; set; }
    public int Positive { get; set; }
    public int Negative { get; set; }
    public int Total { get; set; }
    public double SatisfactionRate { get; set; }
}

/// <summary>
/// Category breakdown item DTO
/// </summary>
public class CategoryBreakdownItemDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Prompt improvements response DTO
/// </summary>
public class PromptImprovementsResponseDto
{
    public ImprovementStatsDto Statistics { get; set; } = null!;
    public List<PromptImprovementDto> Improvements { get; set; } = [];
}

/// <summary>
/// Prompt improvement DTO
/// </summary>
public class PromptImprovementDto
{
    public Guid Id { get; set; }
    public Guid AnalysisReportId { get; set; }
    public string Category { get; set; } = "";
    public string Issue { get; set; } = "";
    public string Suggestion { get; set; } = "";
    public string Priority { get; set; } = "";
    public string PromptModification { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ReviewNotes { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Analysis report summary DTO
/// </summary>
public class AnalysisReportSummaryDto
{
    public Guid Id { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public int TotalFeedbacksAnalyzed { get; set; }
    public int HighPriorityCount { get; set; }
    public int MediumPriorityCount { get; set; }
    public int LowPriorityCount { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

/// <summary>
/// Analysis report detail DTO
/// </summary>
public class AnalysisReportDetailDto : AnalysisReportSummaryDto
{
    public string OverallSummary { get; set; } = "";
    public List<CategoryBreakdownItemDto> Categories { get; set; } = [];
    public List<SuggestionDetailDto> Suggestions { get; set; } = [];
    public List<PromptImprovementDto> TrackedImprovements { get; set; } = [];
}

/// <summary>
/// Suggestion detail DTO
/// </summary>
public class SuggestionDetailDto
{
    public string Category { get; set; } = "";
    public string Issue { get; set; } = "";
    public string Suggestion { get; set; } = "";
    public string Priority { get; set; } = "";
    public string PromptModification { get; set; } = "";
}

/// <summary>
/// Internal DTO for category parsing
/// </summary>
internal class CategoryParseDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Internal DTO for suggestion parsing
/// </summary>
internal class SuggestionParseDto
{
    public string? Category { get; set; }
    public string? Issue { get; set; }
    public string? Suggestion { get; set; }
    public string? Priority { get; set; }
    public string? PromptModification { get; set; }
}
