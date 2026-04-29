using AI.Application.Ports.Secondary.Scheduling;
using AI.Domain.Scheduling;
using AI.Domain.Feedback;
using AI.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.External.Scheduling;

/// <summary>
/// Implementation of ISchedulerDataService using EF Core
/// </summary>
public sealed class SchedulerDataService : ISchedulerDataService
{
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<SchedulerDataService> _logger;

    public SchedulerDataService(
        ChatDbContext dbContext,
        ILogger<SchedulerDataService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<MessageFeedback>> GetFeedbacksPendingAnalysisAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting {Count} feedbacks pending analysis", count);

            var feedbacks = await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Include(f => f.Message)
                .Where(f => !f.IsAnalyzed)
                .OrderBy(f => f.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            // Populate denormalized MessageContent for Scheduler access
            foreach (var f in feedbacks)
                f.MessageContent = f.Message?.Content;

            _logger.LogDebug("Found {Count} feedbacks pending analysis", feedbacks.Count);
            return feedbacks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks pending analysis");
            throw;
        }
    }

    public async Task MarkFeedbacksAsAnalyzedAsync(
        IEnumerable<Guid> feedbackIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Marking {Count} feedbacks as analyzed", feedbackIds.Count());

            var ids = feedbackIds.ToList();
            var feedbacks = await _dbContext.MessageFeedbacks
                .Where(f => ids.Contains(f.Id))
                .ToListAsync(cancellationToken);

            foreach (var feedback in feedbacks)
            {
                feedback.MarkAsAnalyzed();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully marked {Count} feedbacks as analyzed", feedbacks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking feedbacks as analyzed");
            throw;
        }
    }

    public async Task<FeedbackAnalysisReport> SaveAnalysisReportAsync(
        FeedbackAnalysisReport report,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving analysis report");

            await _dbContext.FeedbackAnalysisReports.AddAsync(report, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully saved analysis report: {ReportId}", report.Id);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving analysis report");
            throw;
        }
    }

    public async Task<List<ScheduledReport>> GetDueReportsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            _logger.LogDebug("Getting reports due before: {Now}", now);

            var reports = await _dbContext.ScheduledReports
                .AsNoTracking()
                .Where(r => r.NextRunAt <= now && r.IsActive)
                .OrderBy(r => r.NextRunAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} reports due for execution", reports.Count);
            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting due reports");
            throw;
        }
    }

    public async Task UpdateScheduledReportLastRunAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating last run for report: {ReportId}", reportId);

            var report = await _dbContext.ScheduledReports
                .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

            if (report == null)
            {
                _logger.LogWarning("Scheduled report not found: {ReportId}", reportId);
                throw new ArgumentException($"Scheduled report not found: {reportId}", nameof(reportId));
            }

            // Calculate next run based on schedule
            var nextRunAt = CalculateNextRunAt(report.CronExpression);
            report.RecordSuccessfulRun(nextRunAt);

            _logger.LogDebug(
                "Updated report {ReportId}: LastRun={LastRun}, NextRun={NextRun}, RunCount={Count}",
                reportId,
                report.LastRunAt,
                report.NextRunAt,
                report.RunCount);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scheduled report last run: {ReportId}", reportId);
            throw;
        }
    }

    public async Task<List<ScheduledReport>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all active scheduled reports");

            var reports = await _dbContext.ScheduledReports
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} active scheduled reports", reports.Count);
            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active scheduled reports");
            throw;
        }
    }

    private DateTime CalculateNextRunAt(string cronExpression)
    {
        try
        {
            // Use Cronos library to calculate next run
            var cron = Cronos.CronExpression.Parse(cronExpression);
            var next = cron.GetNextOccurrence(DateTime.UtcNow);

            _logger.LogDebug("Calculated next run from cron '{Cron}': {NextRun}", cronExpression, next);

            return next ?? DateTime.UtcNow.AddDays(1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating next run from cron: {Cron}", cronExpression);
            // Fallback: run tomorrow
            return DateTime.UtcNow.AddDays(1);
        }
    }
}
