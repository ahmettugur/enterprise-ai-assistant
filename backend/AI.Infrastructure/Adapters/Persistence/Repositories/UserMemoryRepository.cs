using AI.Domain.Memory;
using AI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// UserMemory repository implementation using DbContextFactory for thread-safety
/// </summary>
public sealed class UserMemoryRepository : IUserMemoryRepository
{
    private readonly IDbContextFactory<ChatDbContext> _dbContextFactory;
    private readonly ILogger<UserMemoryRepository> _logger;

    public UserMemoryRepository(IDbContextFactory<ChatDbContext> dbContextFactory, ILogger<UserMemoryRepository> logger)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<UserMemory>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.UserMemories
                .AsNoTracking()
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.LastAccessedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memories for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserMemory>> GetByCategoryAsync(string userId, MemoryCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.UserMemories
                .AsNoTracking()
                .Where(m => m.UserId == userId && m.Category == category)
                .OrderByDescending(m => m.Confidence)
                .ThenByDescending(m => m.UsageCount)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memories by category for user: {UserId}, category: {Category}", userId, category);
            throw;
        }
    }

    public async Task<UserMemory?> GetByKeyAsync(string userId, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var normalizedKey = key.ToLowerInvariant().Trim();

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.UserMemories
                .FirstOrDefaultAsync(m => m.UserId == userId && m.Key == normalizedKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory by key for user: {UserId}, key: {Key}", userId, key);
            throw;
        }
    }

    public async Task<UserMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.UserMemories
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory by id: {Id}", id);
            throw;
        }
    }

    public async Task<UserMemory> AddAsync(UserMemory memory, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(memory);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.UserMemories.AddAsync(memory, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Added memory for user: {UserId}, key: {Key}", memory.UserId, memory.Key);
            return memory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding memory for user: {UserId}", memory.UserId);
            throw;
        }
    }

    public async Task UpdateAsync(UserMemory memory, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(memory);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            dbContext.UserMemories.Update(memory);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Updated memory for user: {UserId}, key: {Key}", memory.UserId, memory.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating memory: {Id}", memory.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var memory = await dbContext.UserMemories
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (memory != null)
            {
                memory.MarkAsDeleted();
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Soft deleted memory: {Id}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting memory: {Id}", id);
            throw;
        }
    }

    public async Task DeleteAllByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Soft delete - KVKK uyumluluğu için
            var memories = await dbContext.UserMemories
                .Where(m => m.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (var memory in memories)
            {
                memory.MarkAsDeleted();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Soft deleted all memories for user: {UserId}, count: {Count}", userId, memories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all memories for user: {UserId}", userId);
            throw;
        }
    }

    public async Task AddRangeAsync(IEnumerable<UserMemory> memories, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(memories);

            var memoryList = memories.ToList();
            if (memoryList.Count == 0)
                return;

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.UserMemories.AddRangeAsync(memoryList, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Added {Count} memories", memoryList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding memories in batch");
            throw;
        }
    }

    public async Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            return await dbContext.UserMemories
                .CountAsync(m => m.UserId == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory count for user: {UserId}", userId);
            throw;
        }
    }
}
