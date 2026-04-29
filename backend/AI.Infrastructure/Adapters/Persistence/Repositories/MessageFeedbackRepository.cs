using AI.Application.DTOs.MessageFeedback;
using AI.Domain.Feedback;
using AI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// PostgreSQL implementation of IMessageFeedbackRepository (Command side).
/// Query responsibility → FeedbackQueryService (CQRS separation).
/// </summary>
public class MessageFeedbackRepository : IMessageFeedbackRepository
{
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<MessageFeedbackRepository> _logger;

    public MessageFeedbackRepository(
        ChatDbContext dbContext,
        ILogger<MessageFeedbackRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MessageFeedback> AddAsync(MessageFeedback feedback, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.MessageFeedbacks.AddAsync(feedback, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Feedback added - MessageId: {MessageId}, UserId: {UserId}, Type: {Type}",
                feedback.MessageId, feedback.UserId, feedback.Type);

            return feedback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding feedback for message: {MessageId}", feedback.MessageId);
            throw;
        }
    }

    public async Task<MessageFeedback?> GetByMessageAndUserAsync(Guid messageId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.MessageId == messageId && f.UserId == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback - MessageId: {MessageId}, UserId: {UserId}", messageId, userId);
            throw;
        }
    }

    public async Task<List<MessageFeedback>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Where(f => f.ConversationId == conversationId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks for conversation: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<List<MessageFeedback>> GetByUserAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<MessageFeedback> UpdateAsync(MessageFeedback feedback, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.MessageFeedbacks.Update(feedback);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Feedback updated - Id: {Id}, MessageId: {MessageId}, Type: {Type}",
                feedback.Id, feedback.MessageId, feedback.Type);

            return feedback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feedback: {FeedbackId}", feedback.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        try
        {
            var feedback = await _dbContext.MessageFeedbacks.FindAsync([feedbackId], cancellationToken);
            if (feedback != null)
            {
                _dbContext.MessageFeedbacks.Remove(feedback);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Feedback deleted - Id: {Id}", feedbackId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feedback: {FeedbackId}", feedbackId);
            throw;
        }
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

    public async Task<List<MessageFeedback>> GetNegativeFeedbacksWithCommentsAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var feedbacks = await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Include(f => f.Message)
                .Where(f => f.Type == FeedbackType.Negative && !string.IsNullOrEmpty(f.Comment))
                .OrderByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            // Populate denormalized MessageContent for Application/Scheduler access
            foreach (var f in feedbacks)
                f.MessageContent = f.Message?.Content;

            return feedbacks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting negative feedbacks with comments");
            throw;
        }
    }

    public async Task<List<MessageFeedback>> GetFeedbacksPendingAnalysisAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var feedbacks = await _dbContext.MessageFeedbacks
                .AsNoTracking()
                .Include(f => f.Message)
                .Where(f => f.Type == FeedbackType.Negative && !f.IsAnalyzed)
                .OrderBy(f => f.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);

            // Populate denormalized MessageContent for Application/Scheduler access
            foreach (var f in feedbacks)
                f.MessageContent = f.Message?.Content;

            return feedbacks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks pending analysis");
            throw;
        }
    }

    public async Task MarkAsAnalyzedAsync(IEnumerable<Guid> feedbackIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var feedbacks = await _dbContext.MessageFeedbacks
                .Where(f => feedbackIds.Contains(f.Id))
                .ToListAsync(cancellationToken);

            foreach (var feedback in feedbacks)
            {
                feedback.MarkAsAnalyzed();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Marked {Count} feedbacks as analyzed", feedbacks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking feedbacks as analyzed");
            throw;
        }
    }
}
