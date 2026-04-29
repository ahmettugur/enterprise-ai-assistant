

namespace AI.Domain.Scheduling;

/// <summary>
/// Zamanlanmış raporlar için repository interface'i
/// </summary>
public interface IScheduledReportRepository
{
    #region ScheduledReport Operations

    Task<ScheduledReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ScheduledReport?> GetByIdWithLogsAsync(Guid id, int logLimit = 10, CancellationToken cancellationToken = default);
    Task<List<ScheduledReport>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<ScheduledReport>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<ScheduledReport>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<List<ScheduledReport>> GetDueReportsAsync(CancellationToken cancellationToken = default);
    Task<ScheduledReport> CreateAsync(ScheduledReport report, CancellationToken cancellationToken = default);
    Task<ScheduledReport> UpdateAsync(ScheduledReport report, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsNameUniqueAsync(string userId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    #endregion

    #region ScheduledReportLog Operations

    Task<ScheduledReportLog> CreateLogAsync(ScheduledReportLog log, CancellationToken cancellationToken = default);
    Task<ScheduledReportLog> UpdateLogAsync(ScheduledReportLog log, CancellationToken cancellationToken = default);
    Task<List<ScheduledReportLog>> GetLogsByReportIdAsync(Guid reportId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<ScheduledReportLog?> GetLatestLogByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<int> DeleteOldLogsAsync(int retentionDays = 30, CancellationToken cancellationToken = default);

    #endregion
}
