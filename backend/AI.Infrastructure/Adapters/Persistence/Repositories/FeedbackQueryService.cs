using AI.Application.DTOs.MessageFeedback;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// Dedicated query service for feedback statistics.
/// CQRS separation: Query responsibility ayrıştırıldı — Command ops IMessageFeedbackRepository'de kalır.
/// </summary>
public class FeedbackQueryService : IFeedbackQueryService
{
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<FeedbackQueryService> _logger;

    public FeedbackQueryService(
        ChatDbContext dbContext,
        ILogger<FeedbackQueryService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeedbackStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var feedbacks = await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            return new FeedbackStatistics
            {
                TotalFeedbacks = feedbacks.Count,
                PositiveFeedbacks = feedbacks.Count(f => f.Type == FeedbackType.Positive),
                NegativeFeedbacks = feedbacks.Count(f => f.Type == FeedbackType.Negative),
                StartDate = startDate,
                EndDate = endDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback statistics");
            throw;
        }
    }

    public async Task<FeedbackStatistics> GetStatisticsByUserAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var feedbacks = await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Where(f => f.UserId == userId && f.CreatedAt >= startDate && f.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            return new FeedbackStatistics
            {
                TotalFeedbacks = feedbacks.Count,
                PositiveFeedbacks = feedbacks.Count(f => f.Type == FeedbackType.Positive),
                NegativeFeedbacks = feedbacks.Count(f => f.Type == FeedbackType.Negative),
                StartDate = startDate,
                EndDate = endDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback statistics for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<DailyFeedbackStatistics>> GetDailyStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var feedbacks = await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            var dailyStats = feedbacks
                .GroupBy(f => f.CreatedAt.Date)
                .Select(g => new DailyFeedbackStatistics
                {
                    Date = g.Key,
                    PositiveFeedbacks = g.Count(f => f.Type == FeedbackType.Positive),
                    NegativeFeedbacks = g.Count(f => f.Type == FeedbackType.Negative)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return dailyStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily feedback statistics");
            throw;
        }
    }
}
