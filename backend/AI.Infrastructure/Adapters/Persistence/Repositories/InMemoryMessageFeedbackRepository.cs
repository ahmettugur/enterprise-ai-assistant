using System.Collections.Concurrent;
using AI.Application.DTOs.MessageFeedback;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Feedback;
using AI.Domain.Enums;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// In-memory implementation of IMessageFeedbackRepository for testing and development
/// Thread-safe using ConcurrentDictionary
/// </summary>
public sealed class InMemoryMessageFeedbackRepository : IMessageFeedbackRepository, IFeedbackQueryService
{
    private readonly ConcurrentDictionary<Guid, MessageFeedback> _feedbacks = new();

    public Task<MessageFeedback> AddAsync(MessageFeedback feedback, CancellationToken cancellationToken = default)
    {
        _feedbacks[feedback.Id] = feedback;
        return Task.FromResult(feedback);
    }

    public Task<MessageFeedback?> GetByMessageAndUserAsync(Guid messageId, string userId, CancellationToken cancellationToken = default)
    {
        var feedback = _feedbacks.Values.FirstOrDefault(f =>
            f.MessageId == messageId && f.UserId == userId);
        return Task.FromResult(feedback);
    }

    public Task<List<MessageFeedback>> GetByConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var feedbacks = _feedbacks.Values
            .Where(f => f.ConversationId == conversationId)
            .OrderByDescending(f => f.CreatedAt)
            .ToList();
        return Task.FromResult(feedbacks);
    }

    public Task<List<MessageFeedback>> GetByUserAsync(string userId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        var feedbacks = _feedbacks.Values
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult(feedbacks);
    }

    public Task<MessageFeedback> UpdateAsync(MessageFeedback feedback, CancellationToken cancellationToken = default)
    {
        _feedbacks[feedback.Id] = feedback;
        return Task.FromResult(feedback);
    }

    public Task DeleteAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        _feedbacks.TryRemove(feedbackId, out _);
        return Task.CompletedTask;
    }

    public Task<FeedbackStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var feedbacksInRange = _feedbacks.Values
            .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
            .ToList();

        var stats = CalculateStatistics(feedbacksInRange, startDate, endDate);
        return Task.FromResult(stats);
    }

    public Task<FeedbackStatistics> GetStatisticsByUserAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var feedbacksInRange = _feedbacks.Values
            .Where(f => f.UserId == userId && f.CreatedAt >= startDate && f.CreatedAt <= endDate)
            .ToList();

        var stats = CalculateStatistics(feedbacksInRange, startDate, endDate);
        return Task.FromResult(stats);
    }

    public Task<List<DailyFeedbackStatistics>> GetDailyStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var dailyStats = _feedbacks.Values
            .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
            .GroupBy(f => f.CreatedAt.Date)
            .Select(g => new DailyFeedbackStatistics
            {
                Date = g.Key,
                PositiveFeedbacks = g.Count(f => f.Type == FeedbackType.Positive),
                NegativeFeedbacks = g.Count(f => f.Type == FeedbackType.Negative)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Task.FromResult(dailyStats);
    }

    public Task<List<MessageFeedback>> GetNegativeFeedbacksWithCommentsAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        var feedbacks = _feedbacks.Values
            .Where(f => f.Type == FeedbackType.Negative && !string.IsNullOrWhiteSpace(f.Comment))
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult(feedbacks);
    }

    public Task<List<MessageFeedback>> GetFeedbacksPendingAnalysisAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var feedbacks = _feedbacks.Values
            .Where(f => f.Type == FeedbackType.Negative && !f.IsAnalyzed)
            .OrderBy(f => f.CreatedAt)
            .Take(take)
            .ToList();

        return Task.FromResult(feedbacks);
    }

    public Task MarkAsAnalyzedAsync(IEnumerable<Guid> feedbackIds, CancellationToken cancellationToken = default)
    {
        foreach (var id in feedbackIds)
        {
            if (_feedbacks.TryGetValue(id, out var feedback))
            {
                feedback.MarkAsAnalyzed();
            }
        }
        return Task.CompletedTask;
    }

    #region Helper Methods

    private static FeedbackStatistics CalculateStatistics(List<MessageFeedback> feedbacks, DateTime startDate, DateTime endDate)
    {
        return new FeedbackStatistics
        {
            TotalFeedbacks = feedbacks.Count,
            PositiveFeedbacks = feedbacks.Count(f => f.Type == FeedbackType.Positive),
            NegativeFeedbacks = feedbacks.Count(f => f.Type == FeedbackType.Negative),
            StartDate = startDate,
            EndDate = endDate
        };
    }

    /// <summary>
    /// Clear all data - useful for testing
    /// </summary>
    public void Clear()
    {
        _feedbacks.Clear();
    }

    /// <summary>
    /// Seed test data - useful for testing
    /// </summary>
    public void SeedTestData()
    {
        var conversationId = Guid.NewGuid();
        var userId = "test-user-1";

        // Add some test feedbacks
        var positiveFeedback = MessageFeedback.Create(
            Guid.NewGuid(),
            conversationId,
            userId,
            FeedbackType.Positive,
            "Great response!"
        );
        _feedbacks[positiveFeedback.Id] = positiveFeedback;

        var negativeFeedback = MessageFeedback.Create(
            Guid.NewGuid(),
            conversationId,
            userId,
            FeedbackType.Negative,
            "Response was not accurate"
        );
        _feedbacks[negativeFeedback.Id] = negativeFeedback;

        var anotherPositive = MessageFeedback.Create(
            Guid.NewGuid(),
            conversationId,
            userId,
            FeedbackType.Positive
        );
        _feedbacks[anotherPositive.Id] = anotherPositive;
    }

    /// <summary>
    /// Get count of feedbacks - useful for testing assertions
    /// </summary>
    public int Count => _feedbacks.Count;

    #endregion
}
