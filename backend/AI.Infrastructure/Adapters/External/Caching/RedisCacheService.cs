using AI.Application.DTOs;
using AI.Application.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AI.Application.Common.Constants;
using AI.Application.Ports.Secondary.Services.Cache;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.External.Caching;

/// <summary>
/// Redis-based distributed cache implementation for chat history
/// Uses IDistributedCache abstraction (supports Redis, SQL Server, NCache, etc.)
/// With L1 (Memory) and L2 (Redis) caching layers
/// 
/// İyileştirmeler:
/// - Configuration-based TTL değerleri
/// - Cache stampede koruması (SemaphoreSlim)
/// - Compression desteği (büyük veriler için)
/// - Sliding expiration desteği
/// - Expired key temizleme (memory leak önleme)
/// - Metrics desteği (hit/miss counters)
/// </summary>
public sealed class RedisCacheService : IChatCacheService, IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly CacheSettings _settings;

    // Key tracking for InvalidateAll - thread-safe with TTL tracking
    private readonly ConcurrentDictionary<string, DateTime> _trackedKeys = new();

    // Cache stampede protection - per-key locks
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    // Metrics counters
    private long _l1HitCount;
    private long _l2HitCount;
    private long _missCount;

    // Cleanup timer
    private readonly Timer? _cleanupTimer;
    private bool _disposed;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IMemoryCache memoryCache,
        IOptions<CacheSettings> settings,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? new CacheSettings();

        // Expired key cleanup timer başlat
        _cleanupTimer = new Timer(
            CleanupExpiredKeysCallback,
            null,
            _settings.ExpiredKeyCleanupInterval,
            _settings.ExpiredKeyCleanupInterval);
    }

    #region ChatHistory Caching

    public async Task<ChatHistory?> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = CacheKeys.ChatHistory(conversationId);

            // L1: Check memory cache
            if (_memoryCache.TryGetValue(cacheKey, out ChatHistory? memoryCachedHistory))
            {
                Interlocked.Increment(ref _l1HitCount);
                _logger.LogDebug("ChatHistory found in L1 cache - ConversationId: {ConversationId}", conversationId);
                return memoryCachedHistory;
            }

            // L2: Check distributed cache (Redis)
            var cachedData = await _distributedCache.GetAsync(cacheKey, cancellationToken);

            if (cachedData == null)
            {
                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("ChatHistory not found in cache - ConversationId: {ConversationId}", conversationId);
                return null;
            }

            Interlocked.Increment(ref _l2HitCount);

            // Decompress if needed
            var jsonData = DecompressIfNeeded(cachedData);
            var chatHistory = JsonSerializer.Deserialize<ChatHistory>(jsonData);

            // Update L1 cache
            if (chatHistory != null)
            {
                _memoryCache.Set(cacheKey, chatHistory, _settings.MemoryCacheTtl);
            }

            _logger.LogDebug("ChatHistory found in L2 cache - ConversationId: {ConversationId}", conversationId);
            return chatHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ChatHistory from cache - ConversationId: {ConversationId}", conversationId);
            return null; // Cache miss on error
        }
    }

    public async Task SetChatHistoryAsync(string conversationId, ChatHistory chatHistory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = CacheKeys.ChatHistory(conversationId);
            var serialized = JsonSerializer.Serialize(chatHistory);
            var cacheExpiration = expiration ?? _settings.DefaultRedisTtl;

            // Compress if needed
            var dataToStore = CompressIfNeeded(serialized);

            var options = CreateCacheEntryOptions(cacheExpiration);

            // Set in L2 (Redis)
            await _distributedCache.SetAsync(cacheKey, dataToStore, options, cancellationToken);

            // Set in L1 (Memory)
            _memoryCache.Set(cacheKey, chatHistory, _settings.MemoryCacheTtl);

            // Track the key with expiration time
            TrackKey(cacheKey, cacheExpiration);

            _logger.LogDebug("ChatHistory cached - ConversationId: {ConversationId}, Expiration: {Expiration}, Compressed: {Compressed}",
                conversationId, cacheExpiration, dataToStore.Length < serialized.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting ChatHistory in cache - ConversationId: {ConversationId}", conversationId);
            // Don't throw - cache failures shouldn't break app
        }
    }

    public async Task InvalidateChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = CacheKeys.ChatHistory(conversationId);

            // Remove from L1
            _memoryCache.Remove(cacheKey);

            // Remove from L2
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);

            // Untrack the key
            _trackedKeys.TryRemove(cacheKey, out _);
            _keyLocks.TryRemove(cacheKey, out _);

            _logger.LogDebug("ChatHistory cache invalidated - ConversationId: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating ChatHistory cache - ConversationId: {ConversationId}", conversationId);
        }
    }

    #endregion

    #region Conversation Metadata Caching

    public async Task<ConversationMetadata?> GetConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = CacheKeys.ConversationMetadata(conversationId);

            // L1: Check memory cache
            if (_memoryCache.TryGetValue(cacheKey, out ConversationMetadata? memoryMetadata))
            {
                Interlocked.Increment(ref _l1HitCount);
                return memoryMetadata;
            }

            // L2: Check distributed cache
            var cachedData = await _distributedCache.GetAsync(cacheKey, cancellationToken);

            if (cachedData == null)
            {
                Interlocked.Increment(ref _missCount);
                return null;
            }

            Interlocked.Increment(ref _l2HitCount);

            var jsonData = DecompressIfNeeded(cachedData);
            var metadata = JsonSerializer.Deserialize<ConversationMetadata>(jsonData);

            // Update L1
            if (metadata != null)
            {
                _memoryCache.Set(cacheKey, metadata, _settings.MemoryCacheTtl);
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ConversationMetadata from cache - ConversationId: {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task SetConversationMetadataAsync(string conversationId, ConversationMetadata metadata, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = CacheKeys.ConversationMetadata(conversationId);
            var serialized = JsonSerializer.Serialize(metadata);
            var cacheExpiration = expiration ?? _settings.ConversationMetadataTtl;

            var dataToStore = CompressIfNeeded(serialized);
            var options = CreateCacheEntryOptions(cacheExpiration);

            await _distributedCache.SetAsync(cacheKey, dataToStore, options, cancellationToken);
            _memoryCache.Set(cacheKey, metadata, _settings.MemoryCacheTtl);

            TrackKey(cacheKey, cacheExpiration);

            _logger.LogDebug("ConversationMetadata cached - ConversationId: {ConversationId}, Expiration: {Expiration}",
                conversationId, cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting ConversationMetadata in cache - ConversationId: {ConversationId}", conversationId);
        }
    }

    public async Task InvalidateConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = CacheKeys.ConversationMetadata(conversationId);

            _memoryCache.Remove(cacheKey);
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);

            _trackedKeys.TryRemove(cacheKey, out _);
            _keyLocks.TryRemove(cacheKey, out _);

            _logger.LogDebug("ConversationMetadata cache invalidated - ConversationId: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating ConversationMetadata cache - ConversationId: {ConversationId}", conversationId);
        }
    }

    #endregion

    #region Batch Operations

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keysToRemove = _trackedKeys.Keys.ToList();
            var removedCount = 0;

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key, cancellationToken);
                _trackedKeys.TryRemove(key, out _);
                _keyLocks.TryRemove(key, out _);
                removedCount++;
            }

            _logger.LogInformation("All cache entries cleared from both L1 and L2 - Count: {Count}", removedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all caches");
        }
    }

    #endregion

    #region Generic Cache Operations

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // L1: Check memory cache
            if (_memoryCache.TryGetValue(key, out T? memoryCachedValue))
            {
                Interlocked.Increment(ref _l1HitCount);
                _logger.LogDebug("Value found in L1 cache - Key: {Key}", key);
                return memoryCachedValue;
            }

            // L2: Check distributed cache (Redis)
            var cachedData = await _distributedCache.GetAsync(key, cancellationToken);

            if (cachedData == null)
            {
                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("Value not found in cache - Key: {Key}", key);
                return null;
            }

            Interlocked.Increment(ref _l2HitCount);

            var jsonData = DecompressIfNeeded(cachedData);
            var value = JsonSerializer.Deserialize<T>(jsonData);

            // Update L1 cache
            if (value != null)
            {
                _memoryCache.Set(key, value, _settings.MemoryCacheTtl);
            }

            _logger.LogDebug("Value found in L2 cache - Key: {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache - Key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var cacheExpiration = expiration ?? _settings.DefaultRedisTtl;

            var dataToStore = CompressIfNeeded(serialized);
            var options = CreateCacheEntryOptions(cacheExpiration);

            // Set in L2 (Redis)
            await _distributedCache.SetAsync(key, dataToStore, options, cancellationToken);

            // Set in L1 (Memory)
            _memoryCache.Set(key, value, _settings.MemoryCacheTtl);

            TrackKey(key, cacheExpiration);

            _logger.LogDebug("Value cached - Key: {Key}, Expiration: {Expiration}", key, cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache - Key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _trackedKeys.TryRemove(key, out _);
            _keyLocks.TryRemove(key, out _);

            _logger.LogDebug("Value removed from cache - Key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache - Key: {Key}", key);
        }
    }

    #endregion

    #region Cache Stampede Protection

    /// <summary>
    /// Cache stampede korumalı get-or-create operasyonu
    /// Aynı key için eşzamanlı DB sorgularını önler
    /// </summary>
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // İlk olarak cache'den kontrol et
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
            return cached;

        if (!_settings.EnableStampedeProtection)
        {
            // Stampede koruması kapalıysa direkt factory çağır
            var value = await factory(cancellationToken);
            if (value != null)
                await SetAsync(key, value, expiration, cancellationToken);
            return value;
        }

        // Key-specific lock al
        var keyLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            // Lock timeout ile bekle
            if (!await keyLock.WaitAsync(_settings.StampedeLockTimeout, cancellationToken))
            {
                _logger.LogWarning("Cache stampede lock timeout - Key: {Key}", key);
                // Timeout durumunda yine de factory çağır (degraded mode)
                return await factory(cancellationToken);
            }

            try
            {
                // Double-check: Lock aldıktan sonra tekrar cache kontrol et
                cached = await GetAsync<T>(key, cancellationToken);
                if (cached != null)
                    return cached;

                // Factory'den değer al
                var value = await factory(cancellationToken);
                if (value != null)
                {
                    await SetAsync(key, value, expiration, cancellationToken);
                }
                return value;
            }
            finally
            {
                keyLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrCreateAsync - Key: {Key}", key);
            // Hata durumunda cache'siz devam et
            return await factory(cancellationToken);
        }
    }

    #endregion

    #region Health Check & Metrics

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = "health_check_test";
            var testValue = DateTime.UtcNow.Ticks.ToString();
            var testData = Encoding.UTF8.GetBytes(testValue);

            await _distributedCache.SetAsync(testKey, testData, cancellationToken);
            var retrieved = await _distributedCache.GetAsync(testKey, cancellationToken);
            await _distributedCache.RemoveAsync(testKey, cancellationToken);

            return retrieved != null && Encoding.UTF8.GetString(retrieved) == testValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache health check failed");
            return false;
        }
    }

    /// <summary>
    /// Cache metrics bilgisi döndürür
    /// </summary>
    public CacheMetrics GetMetrics()
    {
        var totalHits = _l1HitCount + _l2HitCount;
        var totalRequests = totalHits + _missCount;

        return new CacheMetrics
        {
            L1HitCount = _l1HitCount,
            L2HitCount = _l2HitCount,
            MissCount = _missCount,
            HitRate = totalRequests > 0 ? (double)totalHits / totalRequests : 0,
            TrackedKeyCount = _trackedKeys.Count,
            ActiveLockCount = _keyLocks.Count
        };
    }

    /// <summary>
    /// Metrics'leri sıfırlar
    /// </summary>
    public void ResetMetrics()
    {
        Interlocked.Exchange(ref _l1HitCount, 0);
        Interlocked.Exchange(ref _l2HitCount, 0);
        Interlocked.Exchange(ref _missCount, 0);
    }

    #endregion

    #region Private Helpers

    private DistributedCacheEntryOptions CreateCacheEntryOptions(TimeSpan expiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        if (_settings.EnableSlidingExpiration)
        {
            // Sliding expiration, absolute'un yarısı veya ayarlanan değer (hangisi küçükse)
            var slidingTime = TimeSpan.FromTicks(Math.Min(
                _settings.SlidingExpirationTtl.Ticks,
                expiration.Ticks / 2));

            options.SlidingExpiration = slidingTime;
        }

        return options;
    }

    private byte[] CompressIfNeeded(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);

        if (!_settings.EnableCompression || bytes.Length < _settings.CompressionThresholdBytes)
        {
            return bytes;
        }

        try
        {
            using var output = new MemoryStream();
            // İlk byte: compression flag (1 = compressed)
            output.WriteByte(1);

            using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            var compressed = output.ToArray();

            // Compression faydalı mı kontrol et
            if (compressed.Length < bytes.Length)
            {
                return compressed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Compression failed, storing uncompressed");
        }

        // Compression yapılmadıysa flag = 0
        var result = new byte[bytes.Length + 1];
        result[0] = 0;
        Buffer.BlockCopy(bytes, 0, result, 1, bytes.Length);
        return result;
    }

    private string DecompressIfNeeded(byte[] data)
    {
        if (data.Length == 0)
            return string.Empty;

        // İlk byte compression flag
        var isCompressed = data[0] == 1;

        if (!isCompressed)
        {
            // Flag olmayan eski format veya uncompressed data
            if (data[0] == 0)
            {
                return Encoding.UTF8.GetString(data, 1, data.Length - 1);
            }
            // Eski format (flag yok) - direkt deserialize
            return Encoding.UTF8.GetString(data);
        }

        try
        {
            using var input = new MemoryStream(data, 1, data.Length - 1);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Decompression failed, trying raw decode");
            return Encoding.UTF8.GetString(data);
        }
    }

    private void TrackKey(string key, TimeSpan expiration)
    {
        var expirationTime = DateTime.UtcNow.Add(expiration);

        // Max key limit kontrolü
        if (_trackedKeys.Count >= _settings.MaxTrackedKeys)
        {
            // En eski key'leri temizle
            CleanupExpiredKeys();

            // Hala limit üstündeyse, en eski %10'u sil
            if (_trackedKeys.Count >= _settings.MaxTrackedKeys)
            {
                var keysToRemove = _trackedKeys
                    .OrderBy(kv => kv.Value)
                    .Take(_settings.MaxTrackedKeys / 10)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var k in keysToRemove)
                {
                    _trackedKeys.TryRemove(k, out _);
                }
            }
        }

        _trackedKeys[key] = expirationTime;
    }

    private void CleanupExpiredKeysCallback(object? state)
    {
        try
        {
            CleanupExpiredKeys();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during expired key cleanup");
        }
    }

    private void CleanupExpiredKeys()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _trackedKeys
            .Where(kv => kv.Value < now)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _trackedKeys.TryRemove(key, out _);
            _keyLocks.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired tracked keys", expiredKeys.Count);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _cleanupTimer?.Dispose();

        // Dispose all locks
        foreach (var lockItem in _keyLocks.Values)
        {
            lockItem.Dispose();
        }
        _keyLocks.Clear();

        _disposed = true;
    }

    #endregion
}

/// <summary>
/// Cache metrics bilgisi
/// </summary>
public class CacheMetrics
{
    public long L1HitCount { get; set; }
    public long L2HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate { get; set; }
    public int TrackedKeyCount { get; set; }
    public int ActiveLockCount { get; set; }
}

/// <summary>
/// No-op cache service for when caching is disabled (InMemory mode)
/// </summary>
public sealed class NullCacheService : IChatCacheService
{
    public Task<ChatHistory?> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
        => Task.FromResult<ChatHistory?>(null);

    public Task SetChatHistoryAsync(string conversationId, ChatHistory chatHistory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<ConversationMetadata?> GetConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationMetadata?>(null);

    public Task SetConversationMetadataAsync(string conversationId, ConversationMetadata metadata, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
