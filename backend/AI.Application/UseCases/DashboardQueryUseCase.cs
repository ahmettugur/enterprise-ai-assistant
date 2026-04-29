using System.Text.Json;
using AI.Application.DTOs.Dashboard;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Feedback;
using AI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases;

/// <summary>
/// Dashboard query service implementation - Use Case
/// Feedback analytics ve prompt improvement işlemlerini yönetir
/// </summary>
public class DashboardQueryUseCase : IDashboardQueryUseCase
{
    private readonly IMessageFeedbackRepository _feedbackRepository;
    private readonly IFeedbackAnalysisReportRepository _reportRepository;
    private readonly IFeedbackQueryService _feedbackQueryService;
    private readonly IPromptImprovementQueryService _improvementQueryService;
    private readonly ILogger<DashboardQueryUseCase> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DashboardQueryUseCase(
        IMessageFeedbackRepository feedbackRepository,
        IFeedbackAnalysisReportRepository reportRepository,
        IFeedbackQueryService feedbackQueryService,
        IPromptImprovementQueryService improvementQueryService,
        ILogger<DashboardQueryUseCase> logger)
    {
        _feedbackRepository = feedbackRepository;
        _reportRepository = reportRepository;
        _feedbackQueryService = feedbackQueryService;
        _improvementQueryService = improvementQueryService;
        _logger = logger;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        // Get last 30 days statistics
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);

        var feedbackStats = await _feedbackQueryService.GetStatisticsAsync(startDate, endDate, cancellationToken);
        var improvementStats = await _improvementQueryService.GetStatisticsAsync(cancellationToken);
        var latestReport = await _reportRepository.GetLatestAsync(cancellationToken);

        // Get previous period for comparison
        var prevEndDate = startDate;
        var prevStartDate = prevEndDate.AddDays(-30);
        var prevFeedbackStats = await _feedbackQueryService.GetStatisticsAsync(prevStartDate, prevEndDate, cancellationToken);

        // Calculate trends
        var satisfactionTrend = prevFeedbackStats.SatisfactionRate > 0
            ? ((feedbackStats.SatisfactionRate - prevFeedbackStats.SatisfactionRate) / prevFeedbackStats.SatisfactionRate) * 100
            : 0;

        var feedbackTrend = prevFeedbackStats.TotalFeedbacks > 0
            ? ((double)(feedbackStats.TotalFeedbacks - prevFeedbackStats.TotalFeedbacks) / prevFeedbackStats.TotalFeedbacks) * 100
            : 0;

        return new DashboardOverviewDto
        {
            Period = new PeriodInfoDto { StartDate = startDate, EndDate = endDate },
            FeedbackStats = new FeedbackStatsDto
            {
                TotalFeedbacks = feedbackStats.TotalFeedbacks,
                PositiveFeedbacks = feedbackStats.PositiveFeedbacks,
                NegativeFeedbacks = feedbackStats.NegativeFeedbacks,
                SatisfactionRate = feedbackStats.SatisfactionRate,
                SatisfactionTrend = satisfactionTrend,
                FeedbackTrend = feedbackTrend
            },
            ImprovementStats = new ImprovementStatsDto
            {
                TotalImprovements = improvementStats.TotalCount,
                PendingCount = improvementStats.PendingCount,
                AppliedCount = improvementStats.AppliedCount,
                RejectedCount = improvementStats.RejectedCount,
                HighPriorityPending = improvementStats.HighPriorityPendingCount
            },
            LastAnalysis = latestReport != null ? new LastAnalysisDto
            {
                Id = latestReport.Id,
                AnalyzedAt = latestReport.AnalyzedAt,
                FeedbacksAnalyzed = latestReport.TotalFeedbacksAnalyzed,
                Summary = latestReport.OverallSummary.Length > 200
                    ? latestReport.OverallSummary[..200] + "..."
                    : latestReport.OverallSummary
            } : null
        };
    }

    public async Task<FeedbackTrendsDto> GetFeedbackTrendsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);

        var dailyStats = await _feedbackQueryService.GetDailyStatisticsAsync(startDate, endDate, cancellationToken);

        return new FeedbackTrendsDto
        {
            Period = new PeriodInfoDto { StartDate = startDate, EndDate = endDate },
            DailyData = dailyStats.Select(d => new DailyFeedbackDataDto
            {
                Date = d.Date,
                Positive = d.PositiveFeedbacks,
                Negative = d.NegativeFeedbacks,
                Total = d.TotalFeedbacks,
                SatisfactionRate = d.TotalFeedbacks > 0
                    ? (double)d.PositiveFeedbacks / d.TotalFeedbacks * 100
                    : 0
            }).ToList()
        };
    }

    public async Task<List<CategoryBreakdownItemDto>> GetCategoryBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.GetAllAsync(limit: 5, cancellationToken);

        // Aggregate categories from recent reports
        var categoryAggregation = new Dictionary<string, CategoryBreakdownItemDto>();

        foreach (var report in reports)
        {
            try
            {
                var categories = JsonSerializer.Deserialize<List<CategoryParseDto>>(
                    report.CategoriesJson, JsonOptions);

                if (categories != null)
                {
                    foreach (var cat in categories)
                    {
                        if (categoryAggregation.TryGetValue(cat.Name ?? "", out var existing))
                        {
                            existing.Count += cat.Count;
                        }
                        else
                        {
                            categoryAggregation[cat.Name ?? ""] = new CategoryBreakdownItemDto
                            {
                                Name = cat.Name ?? "",
                                Description = cat.Description ?? "",
                                Count = cat.Count
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse categories from report {ReportId}", report.Id);
            }
        }

        var total = categoryAggregation.Values.Sum(c => c.Count);
        foreach (var item in categoryAggregation.Values)
        {
            item.Percentage = total > 0 ? (double)item.Count / total * 100 : 0;
        }

        return categoryAggregation.Values
            .OrderByDescending(c => c.Count)
            .ToList();
    }

    public async Task<PromptImprovementsResponseDto> GetImprovementsAsync(
        string? status = null,
        string? priority = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        PromptImprovementStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PromptImprovementStatus>(status, true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var improvements = await _reportRepository.GetAllImprovementsAsync(
            statusFilter, priority, limit, cancellationToken);

        var stats = await _improvementQueryService.GetStatisticsAsync(cancellationToken);

        return new PromptImprovementsResponseDto
        {
            Statistics = new ImprovementStatsDto
            {
                TotalImprovements = stats.TotalCount,
                PendingCount = stats.PendingCount,
                AppliedCount = stats.AppliedCount,
                RejectedCount = stats.RejectedCount,
                HighPriorityPending = stats.HighPriorityPendingCount
            },
            Improvements = improvements.Select(MapToDto).ToList()
        };
    }

    public async Task<PromptImprovementDto?> UpdateImprovementStatusAsync(
        Guid id,
        string status,
        string userId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var improvement = await _reportRepository.GetImprovementByIdAsync(id, cancellationToken);
        if (improvement == null)
            return null;

        switch (status.ToLower())
        {
            case "applied":
                improvement.Apply(userId, notes);
                break;
            case "rejected":
                improvement.Reject(userId, notes);
                break;
            case "underreview":
                improvement.StartReview(userId);
                break;
            default:
                throw new ArgumentException("Invalid status. Use: Applied, Rejected, or UnderReview");
        }

        await _reportRepository.UpdateImprovementAsync(improvement, cancellationToken);

        _logger.LogInformation("Prompt improvement {Id} status updated to {Status} by {User}",
            id, status, userId);

        return MapToDto(improvement);
    }

    public async Task<List<AnalysisReportSummaryDto>> GetAnalysisReportsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.GetAllAsync(limit, cancellationToken);

        return reports.Select(r => new AnalysisReportSummaryDto
        {
            Id = r.Id,
            AnalyzedAt = r.AnalyzedAt,
            TotalFeedbacksAnalyzed = r.TotalFeedbacksAnalyzed,
            HighPriorityCount = r.HighPriorityCount,
            MediumPriorityCount = r.MediumPriorityCount,
            LowPriorityCount = r.LowPriorityCount,
            PeriodStart = r.PeriodStart,
            PeriodEnd = r.PeriodEnd
        }).ToList();
    }

    public async Task<AnalysisReportDetailDto?> GetAnalysisReportDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.GetAllAsync(100, cancellationToken);
        var report = reports.FirstOrDefault(r => r.Id == id);

        if (report == null)
            return null;

        var improvements = await _reportRepository.GetImprovementsByReportIdAsync(id, cancellationToken);

        List<CategoryParseDto>? categories = null;
        List<SuggestionParseDto>? suggestions = null;

        try
        {
            categories = JsonSerializer.Deserialize<List<CategoryParseDto>>(
                report.CategoriesJson, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse categories from report {ReportId}", id);
        }

        try
        {
            suggestions = JsonSerializer.Deserialize<List<SuggestionParseDto>>(
                report.SuggestionsJson, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse suggestions from report {ReportId}", id);
        }

        return new AnalysisReportDetailDto
        {
            Id = report.Id,
            AnalyzedAt = report.AnalyzedAt,
            TotalFeedbacksAnalyzed = report.TotalFeedbacksAnalyzed,
            OverallSummary = report.OverallSummary,
            HighPriorityCount = report.HighPriorityCount,
            MediumPriorityCount = report.MediumPriorityCount,
            LowPriorityCount = report.LowPriorityCount,
            PeriodStart = report.PeriodStart,
            PeriodEnd = report.PeriodEnd,
            Categories = categories?.Select(c => new CategoryBreakdownItemDto
            {
                Name = c.Name ?? "",
                Description = c.Description ?? "",
                Count = c.Count,
                Percentage = c.Percentage
            }).ToList() ?? [],
            Suggestions = suggestions?.Select(s => new SuggestionDetailDto
            {
                Category = s.Category ?? "",
                Issue = s.Issue ?? "",
                Suggestion = s.Suggestion ?? "",
                Priority = s.Priority ?? "Medium",
                PromptModification = s.PromptModification ?? ""
            }).ToList() ?? [],
            TrackedImprovements = improvements.Select(MapToDto).ToList()
        };
    }

    private static PromptImprovementDto MapToDto(PromptImprovement improvement)
    {
        return new PromptImprovementDto
        {
            Id = improvement.Id,
            AnalysisReportId = improvement.AnalysisReportId,
            Category = improvement.Category,
            Issue = improvement.Issue,
            Suggestion = improvement.Suggestion,
            Priority = improvement.Priority,
            PromptModification = improvement.PromptModification,
            Status = improvement.Status.ToString(),
            ReviewNotes = improvement.ReviewNotes,
            ReviewedBy = improvement.ReviewedBy,
            ReviewedAt = improvement.ReviewedAt,
            CreatedAt = improvement.CreatedAt
        };
    }
}
