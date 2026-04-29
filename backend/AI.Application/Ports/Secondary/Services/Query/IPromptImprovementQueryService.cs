using AI.Application.DTOs.PromptImprovement;

namespace AI.Application.Ports.Secondary.Services.Query;

/// <summary>
/// Application-layer query service for prompt improvement statistics.
/// DTO döndürdüğü için Domain'de değil, Application'da kalır.
/// </summary>
public interface IPromptImprovementQueryService
{
    Task<PromptImprovementStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
