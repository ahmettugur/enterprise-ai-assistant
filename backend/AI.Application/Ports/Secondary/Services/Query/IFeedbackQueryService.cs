using AI.Application.DTOs.MessageFeedback;

namespace AI.Application.Ports.Secondary.Services.Query;

/// <summary>
/// Application-layer query service for feedback statistics.
/// DTO döndürdüğü için Domain'de değil, Application'da kalır.
/// </summary>
public interface IFeedbackQueryService
{
    Task<FeedbackStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<FeedbackStatistics> GetStatisticsByUserAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<DailyFeedbackStatistics>> GetDailyStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
