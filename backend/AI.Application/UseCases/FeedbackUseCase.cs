using AI.Application.DTOs.Feedback;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Query;
using AI.Domain.Conversations;
using AI.Domain.Feedback;
using AI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases;

/// <summary>
/// Feedback service implementation - Use Case
/// Message feedback CRUD işlemlerini yönetir
/// </summary>
public class FeedbackUseCase : IFeedbackUseCase
{
    private readonly IMessageFeedbackRepository _feedbackRepository;
    private readonly IFeedbackQueryService _feedbackQueryService;
    private readonly IConversationRepository _historyRepository;
    private readonly ILogger<FeedbackUseCase> _logger;

    public FeedbackUseCase(
        IMessageFeedbackRepository feedbackRepository,
        IFeedbackQueryService feedbackQueryService,
        IConversationRepository historyRepository,
        ILogger<FeedbackUseCase> logger)
    {
        _feedbackRepository = feedbackRepository;
        _feedbackQueryService = feedbackQueryService;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<FeedbackDto?> AddFeedbackAsync(
        Guid messageId,
        string userId,
        string feedbackType,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        // Check if message exists
        var message = await _historyRepository.GetMessageAsync(messageId);
        if (message == null)
        {
            _logger.LogWarning("Message not found for feedback: {MessageId}", messageId);
            return null;
        }

        // Parse feedback type
        var type = ParseFeedbackType(feedbackType);
        if (type == null)
        {
            _logger.LogWarning("Invalid feedback type: {FeedbackType}", feedbackType);
            return null;
        }

        // Check if feedback already exists
        var existingFeedback = await _feedbackRepository.GetByMessageAndUserAsync(messageId, userId);

        if (existingFeedback != null)
        {
            // Update existing feedback by deleting and recreating
            await _feedbackRepository.DeleteAsync(existingFeedback.Id);
        }

        // Create new feedback
        var feedback = MessageFeedback.Create(
            messageId,
            message.ConversationId,
            userId,
            type.Value,
            comment
        );

        var created = await _feedbackRepository.AddAsync(feedback);

        _logger.LogInformation(
            "Feedback {Action} - MessageId: {MessageId}, UserId: {UserId}, Type: {Type}",
            existingFeedback != null ? "updated" : "added",
            messageId, userId, type.Value);

        return MapToDto(created);
    }

    public async Task<FeedbackDto?> GetFeedbackForMessageAsync(
        Guid messageId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var feedback = await _feedbackRepository.GetByMessageAndUserAsync(messageId, userId);
        return feedback != null ? MapToDto(feedback) : null;
    }

    public async Task<List<FeedbackDto>> GetFeedbacksForConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = await _feedbackRepository.GetByConversationAsync(conversationId);
        return feedbacks.Select(MapToDto).ToList();
    }

    public async Task<FeedbackStatisticsDto> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var stats = await _feedbackQueryService.GetStatisticsAsync(startDate, endDate, cancellationToken);

        return new FeedbackStatisticsDto
        {
            TotalFeedbacks = stats.TotalFeedbacks,
            PositiveFeedbacks = stats.PositiveFeedbacks,
            NegativeFeedbacks = stats.NegativeFeedbacks,
            SatisfactionRate = stats.SatisfactionRate,
            StartDate = stats.StartDate,
            EndDate = stats.EndDate
        };
    }

    public async Task<bool> DeleteFeedbackAsync(
        Guid messageId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var feedback = await _feedbackRepository.GetByMessageAndUserAsync(messageId, userId);
        if (feedback == null)
        {
            return false;
        }

        await _feedbackRepository.DeleteAsync(feedback.Id);

        _logger.LogInformation("Feedback deleted - MessageId: {MessageId}, UserId: {UserId}", messageId, userId);

        return true;
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-days);

        // Get overall statistics
        var stats = await _feedbackQueryService.GetStatisticsAsync(startDate, endDate, cancellationToken);

        // Get daily trends
        var dailyStats = await _feedbackQueryService.GetDailyStatisticsAsync(startDate, endDate, cancellationToken);

        // Calculate week-over-week comparison
        var lastWeekStart = endDate.AddDays(-7);
        var previousWeekStart = endDate.AddDays(-14);

        var lastWeekStats = await _feedbackQueryService.GetStatisticsAsync(lastWeekStart, endDate, cancellationToken);
        var previousWeekStats = await _feedbackQueryService.GetStatisticsAsync(previousWeekStart, lastWeekStart, cancellationToken);

        var trendChange = previousWeekStats.SatisfactionRate > 0
            ? Math.Round(lastWeekStats.SatisfactionRate - previousWeekStats.SatisfactionRate, 2)
            : 0;

        return new DashboardStatisticsDto
        {
            TotalFeedbacks = stats.TotalFeedbacks,
            PositiveFeedbacks = stats.PositiveFeedbacks,
            NegativeFeedbacks = stats.NegativeFeedbacks,
            SatisfactionRate = stats.SatisfactionRate,
            TrendChange = trendChange,
            TrendDirection = trendChange > 0 ? "up" : trendChange < 0 ? "down" : "stable",
            DailyStats = dailyStats.Select(d => new DailyStatDto
            {
                Date = d.Date.ToString("yyyy-MM-dd"),
                PositiveCount = d.PositiveFeedbacks,
                NegativeCount = d.NegativeFeedbacks,
                SatisfactionRate = d.SatisfactionRate
            }).ToList(),
            StartDate = startDate,
            EndDate = endDate
        };
    }

    public async Task<List<NegativeFeedbackDto>> GetNegativeFeedbacksAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = await _feedbackRepository.GetNegativeFeedbacksWithCommentsAsync(skip, take);

        return feedbacks.Select(f => new NegativeFeedbackDto
        {
            Id = f.Id,
            MessageId = f.MessageId,
            ConversationId = f.ConversationId,
            UserId = f.UserId,
            Comment = f.Comment,
            MessageContent = f.MessageContent,
            CreatedAt = f.CreatedAt,
            IsAnalyzed = f.IsAnalyzed,
            AnalyzedAt = f.AnalyzedAt
        }).ToList();
    }

    private static FeedbackType? ParseFeedbackType(string typeString)
    {
        return typeString?.ToLowerInvariant() switch
        {
            "positive" => FeedbackType.Positive,
            "negative" => FeedbackType.Negative,
            _ => null
        };
    }

    private static FeedbackDto MapToDto(MessageFeedback feedback)
    {
        return new FeedbackDto
        {
            Id = feedback.Id,
            MessageId = feedback.MessageId,
            ConversationId = feedback.ConversationId,
            Type = feedback.Type.ToString().ToLower(),
            Comment = feedback.Comment,
            CreatedAt = feedback.CreatedAt
        };
    }
}
