using AI.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// Zamanlanmış raporlar için repository implementasyonu
/// </summary>
public sealed class ScheduledReportRepository : IScheduledReportRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ScheduledReportRepository> _logger;

    public ScheduledReportRepository(ChatDbContext context, ILogger<ScheduledReportRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region ScheduledReport Operations

    /// <inheritdoc />
    public async Task<ScheduledReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScheduledReport?> GetByIdWithLogsAsync(Guid id, int logLimit = 10, CancellationToken cancellationToken = default)
    {
        var report = await _context.ScheduledReports
            .Include(r => r.Logs!.OrderByDescending(l => l.StartedAt).Take(logLimit))
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return report;
    }

    /// <inheritdoc />
    public async Task<List<ScheduledReport>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ScheduledReport>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ScheduledReport>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.NextRunAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ScheduledReport>> GetDueReportsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _context.ScheduledReports
            .Where(r => r.IsActive && r.NextRunAt != null && r.NextRunAt <= now)
            .OrderBy(r => r.NextRunAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScheduledReport> CreateAsync(ScheduledReport report, CancellationToken cancellationToken = default)
    {
        await _context.ScheduledReports.AddAsync(report, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Zamanlanmış rapor oluşturuldu - Id: {Id}, UserId: {UserId}, Name: {Name}",
            report.Id, report.UserId, report.Name);
        
        return report;
    }

    /// <inheritdoc />
    public async Task<ScheduledReport> UpdateAsync(ScheduledReport report, CancellationToken cancellationToken = default)
    {
        _context.ScheduledReports.Update(report);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Zamanlanmış rapor güncellendi - Id: {Id}", report.Id);
        
        return report;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _context.ScheduledReports.FindAsync([id], cancellationToken);
        
        if (report == null)
        {
            _logger.LogWarning("Silinecek zamanlanmış rapor bulunamadı - Id: {Id}", id);
            return false;
        }

        _context.ScheduledReports.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Zamanlanmış rapor silindi - Id: {Id}, Name: {Name}", id, report.Name);
        
        return true;
    }

    /// <inheritdoc />
    public async Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReports
            .CountAsync(r => r.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsNameUniqueAsync(string userId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ScheduledReports
            .Where(r => r.UserId == userId && r.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    #endregion

    #region ScheduledReportLog Operations

    /// <inheritdoc />
    public async Task<ScheduledReportLog> CreateLogAsync(ScheduledReportLog log, CancellationToken cancellationToken = default)
    {
        await _context.ScheduledReportLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Zamanlanmış rapor log kaydı oluşturuldu - Id: {Id}, ReportId: {ReportId}",
            log.Id, log.ScheduledReportId);
        
        return log;
    }

    /// <inheritdoc />
    public async Task<ScheduledReportLog> UpdateLogAsync(ScheduledReportLog log, CancellationToken cancellationToken = default)
    {
        _context.ScheduledReportLogs.Update(log);
        await _context.SaveChangesAsync(cancellationToken);
        
        return log;
    }

    /// <inheritdoc />
    public async Task<List<ScheduledReportLog>> GetLogsByReportIdAsync(Guid reportId, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReportLogs
            .AsNoTracking()
            .Where(l => l.ScheduledReportId == reportId)
            .OrderByDescending(l => l.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScheduledReportLog?> GetLatestLogByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledReportLogs
            .AsNoTracking()
            .Where(l => l.ScheduledReportId == reportId)
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldLogsAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var deletedCount = await _context.ScheduledReportLogs
            .Where(l => l.StartedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Eski log kayıtları temizlendi - Silinen: {Count}, Kesim tarihi: {CutoffDate}",
                deletedCount, cutoffDate);
        }

        return deletedCount;
    }

    #endregion
}
