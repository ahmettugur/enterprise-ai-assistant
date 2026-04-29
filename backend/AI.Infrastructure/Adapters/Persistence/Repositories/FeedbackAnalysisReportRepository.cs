using AI.Domain.Feedback;
using AI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// Repository implementation for FeedbackAnalysisReport entity
/// </summary>
public class FeedbackAnalysisReportRepository : IFeedbackAnalysisReportRepository
{
    private readonly ChatDbContext _context;

    public FeedbackAnalysisReportRepository(ChatDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FeedbackAnalysisReport report, CancellationToken cancellationToken = default)
    {
        await _context.FeedbackAnalysisReports.AddAsync(report, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeedbackAnalysisReport?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FeedbackAnalysisReports
            .OrderByDescending(r => r.AnalyzedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<FeedbackAnalysisReport>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeedbackAnalysisReports
            .Where(r => r.AnalyzedAt >= startDate && r.AnalyzedAt <= endDate)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FeedbackAnalysisReport>> GetAllAsync(
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FeedbackAnalysisReports
            .OrderByDescending(r => r.AnalyzedAt);

        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync(cancellationToken);
        }

        return await query.ToListAsync(cancellationToken);
    }

    // ── PromptImprovement (child entity) operations ──

    public async Task AddImprovementAsync(PromptImprovement improvement, CancellationToken cancellationToken = default)
    {
        await _context.PromptImprovements.AddAsync(improvement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddImprovementsAsync(IEnumerable<PromptImprovement> improvements, CancellationToken cancellationToken = default)
    {
        await _context.PromptImprovements.AddRangeAsync(improvements, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PromptImprovement?> GetImprovementByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PromptImprovements
            .Include(p => p.AnalysisReport)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<PromptImprovement>> GetAllImprovementsAsync(
        PromptImprovementStatus? status = null,
        string? priority = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PromptImprovements
            .Include(p => p.AnalysisReport)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrEmpty(priority))
            query = query.Where(p => p.Priority == priority);

        query = query.OrderByDescending(p => p.CreatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<PromptImprovement>> GetImprovementsByReportIdAsync(
        Guid analysisReportId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PromptImprovements
            .Where(p => p.AnalysisReportId == analysisReportId)
            .OrderBy(p => p.Priority == "High" ? 0 : p.Priority == "Medium" ? 1 : 2)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PromptImprovement>> GetPendingImprovementsByPriorityAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _context.PromptImprovements
            .Where(p => p.Status == PromptImprovementStatus.Pending)
            .OrderBy(p => p.Priority == "High" ? 0 : p.Priority == "Medium" ? 1 : 2)
            .ThenByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateImprovementAsync(PromptImprovement improvement, CancellationToken cancellationToken = default)
    {
        _context.PromptImprovements.Update(improvement);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
