using AI.Application.DTOs.Dashboard;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Dashboard query Use Case interface - Primary Port
/// API'den doğrudan çağrılır (DashboardEndpoints.cs)
/// Feedback analytics ve prompt improvement işlemlerini yönetir
/// </summary>
public interface IDashboardQueryUseCase
{
    /// <summary>
    /// Dashboard genel bakış istatistiklerini getirir
    /// </summary>
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Feedback trendlerini getirir
    /// </summary>
    Task<FeedbackTrendsDto> GetFeedbackTrendsAsync(int days = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kategori bazlı dağılımı getirir
    /// </summary>
    Task<List<CategoryBreakdownItemDto>> GetCategoryBreakdownAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Prompt iyileştirme listesini getirir
    /// </summary>
    Task<PromptImprovementsResponseDto> GetImprovementsAsync(
        string? status = null, 
        string? priority = null, 
        int limit = 50, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Prompt iyileştirme durumunu günceller
    /// </summary>
    Task<PromptImprovementDto?> UpdateImprovementStatusAsync(
        Guid id, 
        string status, 
        string userId,
        string? notes = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analiz raporları listesini getirir
    /// </summary>
    Task<List<AnalysisReportSummaryDto>> GetAnalysisReportsAsync(int limit = 20, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tekil analiz raporu detayını getirir
    /// </summary>
    Task<AnalysisReportDetailDto?> GetAnalysisReportDetailAsync(Guid id, CancellationToken cancellationToken = default);
}
