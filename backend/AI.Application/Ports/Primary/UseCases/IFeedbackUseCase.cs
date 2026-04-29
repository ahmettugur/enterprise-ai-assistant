using AI.Application.DTOs.Feedback;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Feedback Use Case interface - Primary Port
/// API'den doğrudan çağrılır (FeedbackEndpoints.cs)
/// Message feedback CRUD işlemlerini yönetir
/// </summary>
public interface IFeedbackUseCase
{
    /// <summary>
    /// Mesaja feedback ekler
    /// </summary>
    Task<FeedbackDto?> AddFeedbackAsync(
        Guid messageId, 
        string userId, 
        string feedbackType, 
        string? comment = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mesaj için feedback getirir
    /// </summary>
    Task<FeedbackDto?> GetFeedbackForMessageAsync(
        Guid messageId, 
        string userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Conversation için tüm feedbackleri getirir
    /// </summary>
    Task<List<FeedbackDto>> GetFeedbacksForConversationAsync(
        Guid conversationId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tarih aralığına göre istatistikleri getirir
    /// </summary>
    Task<FeedbackStatisticsDto> GetStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Feedback siler
    /// </summary>
    Task<bool> DeleteFeedbackAsync(
        Guid messageId, 
        string userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dashboard istatistiklerini getirir
    /// </summary>
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
        int days = 30, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Negatif feedbackleri inceleme için getirir
    /// </summary>
    Task<List<NegativeFeedbackDto>> GetNegativeFeedbacksAsync(
        int skip = 0, 
        int take = 50, 
        CancellationToken cancellationToken = default);
}
