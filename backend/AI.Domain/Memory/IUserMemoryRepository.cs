
using AI.Domain.Enums;

namespace AI.Domain.Memory;

/// <summary>
/// UserMemory repository interface
/// </summary>
public interface IUserMemoryRepository
{
    Task<List<UserMemory>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<UserMemory>> GetByCategoryAsync(string userId, MemoryCategory category, CancellationToken cancellationToken = default);
    Task<UserMemory?> GetByKeyAsync(string userId, string key, CancellationToken cancellationToken = default);
    Task<UserMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserMemory> AddAsync(UserMemory memory, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserMemory memory, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<UserMemory> memories, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
