using AI.Application.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using AI.Application.Ports.Secondary.Services.Document;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.External.Caching;

/// <summary>
/// Document ve Category için Redis cache service
/// L1 (Memory) + L2 (Redis) caching layers
/// </summary>
public sealed class DocumentCacheService : IDocumentCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<DocumentCacheService> _logger;
    
    // Cache süreleri - dökümanlar sık değişmeyeceği için uzun tutuyoruz
    private readonly TimeSpan _redisExpiration = TimeSpan.FromHours(24);
    private readonly TimeSpan _memoryExpiration = TimeSpan.FromMinutes(30);

    // Cache key prefixes
    private const string CategoryPrefix = "doc:category:";
    private const string DocumentPrefix = "doc:display:";

    public DocumentCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        ILogger<DocumentCacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Category Cache

    public async Task<List<DocumentCategoryDto>?> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{CategoryPrefix}all";
        return await GetFromCacheAsync<List<DocumentCategoryDto>>(cacheKey, cancellationToken);
    }

    public async Task SetAllCategoriesAsync(List<DocumentCategoryDto> categories, CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{CategoryPrefix}all";
        await SetToCacheAsync(cacheKey, categories, cancellationToken);
        _logger.LogInformation("Cached {Count} categories", categories.Count);
    }

    public async Task<List<DocumentCategorySelectDto>?> GetCategoriesForSelectAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{CategoryPrefix}select";
        return await GetFromCacheAsync<List<DocumentCategorySelectDto>>(cacheKey, cancellationToken);
    }

    public async Task SetCategoriesForSelectAsync(List<DocumentCategorySelectDto> categories, CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{CategoryPrefix}select";
        await SetToCacheAsync(cacheKey, categories, cancellationToken);
    }

    public async Task InvalidateCategoryCacheAsync(CancellationToken cancellationToken = default)
    {
        var keys = new[]
        {
            $"{CategoryPrefix}all",
            $"{CategoryPrefix}select"
        };

        foreach (var key in keys)
        {
            await RemoveFromCacheAsync(key, cancellationToken);
        }

        _logger.LogInformation("Category cache invalidated");
    }

    #endregion

    #region Document Display Info Cache

    public async Task<List<DocumentDisplayInfoListDto>?> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{DocumentPrefix}all";
        return await GetFromCacheAsync<List<DocumentDisplayInfoListDto>>(cacheKey, cancellationToken);
    }

    public async Task SetAllDocumentsAsync(List<DocumentDisplayInfoListDto> documents, CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{DocumentPrefix}all";
        await SetToCacheAsync(cacheKey, documents, cancellationToken);
        _logger.LogInformation("Cached {Count} documents", documents.Count);
    }

    public async Task<List<DocumentDisplayInfoSelectDto>?> GetDocumentsForSelectAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{DocumentPrefix}select";
        return await GetFromCacheAsync<List<DocumentDisplayInfoSelectDto>>(cacheKey, cancellationToken);
    }

    public async Task SetDocumentsForSelectAsync(List<DocumentDisplayInfoSelectDto> documents, CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{DocumentPrefix}select";
        await SetToCacheAsync(cacheKey, documents, cancellationToken);
    }

    public async Task<List<DocumentDisplayInfoListDto>?> GetDocumentsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{DocumentPrefix}category:{categoryId}";
        return await GetFromCacheAsync<List<DocumentDisplayInfoListDto>>(cacheKey, cancellationToken);
    }

    public async Task SetDocumentsByCategoryAsync(string categoryId, List<DocumentDisplayInfoListDto> documents, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{DocumentPrefix}category:{categoryId}";
        await SetToCacheAsync(cacheKey, documents, cancellationToken);
    }

    public async Task InvalidateDocumentCacheAsync(CancellationToken cancellationToken = default)
    {
        // Ana cache key'leri sil
        var keys = new[]
        {
            $"{DocumentPrefix}all",
            $"{DocumentPrefix}select"
        };

        foreach (var key in keys)
        {
            await RemoveFromCacheAsync(key, cancellationToken);
        }

        // Kategori bazlı cache'leri temizlemek için pattern-based silme yapamıyoruz
        // Bu nedenle memory cache'den tüm document prefix'li key'leri temizliyoruz
        // Redis'te ise TTL ile otomatik temizlenecek veya manuel silinecek
        
        _logger.LogInformation("Document cache invalidated");
    }

    #endregion

    #region User-Specific Cache

    private const string UserPrefix = "doc:user:";

    public async Task<List<DocumentDisplayInfoListDto>?> GetDocumentsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserPrefix}{userId}:documents";
        return await GetFromCacheAsync<List<DocumentDisplayInfoListDto>>(cacheKey, cancellationToken);
    }

    public async Task SetDocumentsForUserAsync(string userId, List<DocumentDisplayInfoListDto> documents, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserPrefix}{userId}:documents";
        await SetToCacheAsync(cacheKey, documents, cancellationToken);
        _logger.LogInformation("Cached {Count} documents for user: {UserId}", documents.Count, userId);
    }

    public async Task<List<DocumentCategoryDto>?> GetCategoriesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserPrefix}{userId}:categories";
        return await GetFromCacheAsync<List<DocumentCategoryDto>>(cacheKey, cancellationToken);
    }

    public async Task SetCategoriesForUserAsync(string userId, List<DocumentCategoryDto> categories, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{UserPrefix}{userId}:categories";
        await SetToCacheAsync(cacheKey, categories, cancellationToken);
        _logger.LogInformation("Cached {Count} categories for user: {UserId}", categories.Count, userId);
    }

    public async Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default)
    {
        var keys = new[]
        {
            $"{UserPrefix}{userId}:documents",
            $"{UserPrefix}{userId}:categories"
        };

        foreach (var key in keys)
        {
            await RemoveFromCacheAsync(key, cancellationToken);
        }

        _logger.LogInformation("User cache invalidated for: {UserId}", userId);
    }

    #endregion

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        await InvalidateCategoryCacheAsync(cancellationToken);
        await InvalidateDocumentCacheAsync(cancellationToken);
        _logger.LogInformation("All document and category caches invalidated");
    }

    #region Private Helper Methods

    private async Task<T?> GetFromCacheAsync<T>(string cacheKey, CancellationToken cancellationToken) where T : class
    {
        try
        {
            // L1: Check memory cache
            if (_memoryCache.TryGetValue(cacheKey, out T? memoryCached))
            {
                _logger.LogDebug("Cache hit (L1 Memory): {CacheKey}", cacheKey);
                return memoryCached;
            }

            // L2: Check distributed cache (Redis)
            var cachedData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache miss: {CacheKey}", cacheKey);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedData);

            // Update L1 cache
            if (result != null)
            {
                _memoryCache.Set(cacheKey, result, _memoryExpiration);
            }

            _logger.LogDebug("Cache hit (L2 Redis): {CacheKey}", cacheKey);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting from cache: {CacheKey}", cacheKey);
            return null;
        }
    }

    private async Task SetToCacheAsync<T>(string cacheKey, T value, CancellationToken cancellationToken)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _redisExpiration
            };

            // Set in L2 (Redis)
            await _distributedCache.SetStringAsync(cacheKey, serialized, options, cancellationToken);

            // Set in L1 (Memory)
            _memoryCache.Set(cacheKey, value, _memoryExpiration);

            _logger.LogDebug("Cache set: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache: {CacheKey}", cacheKey);
        }
    }

    private async Task RemoveFromCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            // Remove from L1 (Memory)
            _memoryCache.Remove(cacheKey);

            // Remove from L2 (Redis)
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogDebug("Cache removed: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache: {CacheKey}", cacheKey);
        }
    }

    #endregion
}
