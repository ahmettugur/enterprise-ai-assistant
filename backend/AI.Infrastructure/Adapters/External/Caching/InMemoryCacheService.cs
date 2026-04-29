using AI.Application.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;
using AI.Application.Ports.Secondary.Services.Cache;

namespace AI.Infrastructure.Adapters.External.Caching;

/// <summary>
/// In-memory implementation of chat cache service
/// With key tracking for proper InvalidateAll support
/// </summary>
public sealed class InMemoryCacheService : IChatCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    // Key tracking for InvalidateAll - thread-safe
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    public InMemoryCacheService(
        IMemoryCache cache,
        ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _defaultExpiration = TimeSpan.FromHours(1); // Default 1 hour expiration
    }

    #region ChatHistory Caching

    public Task<ChatHistory?> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"chat:history:{conversationId}";
            var value = _cache.Get<ChatHistory>(key);
            _logger.LogDebug("Cache GET for ChatHistory key: {Key}, Found: {Found}", key, value != null);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ChatHistory from cache for conversationId: {ConversationId}", conversationId);
            return Task.FromResult<ChatHistory?>(null);
        }
    }

    public Task SetChatHistoryAsync(string conversationId, ChatHistory chatHistory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"chat:history:{conversationId}";
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };

            // Register callback to remove from tracked keys when evicted
            options.RegisterPostEvictionCallback((evictedKey, value, reason, state) =>
            {
                _trackedKeys.TryRemove(evictedKey.ToString()!, out _);
            });

            _cache.Set(key, chatHistory, options);
            _trackedKeys.TryAdd(key, 0); // Track the key
            _logger.LogDebug("Cache SET for ChatHistory key: {Key}, Expiration: {Expiration}", key, expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting ChatHistory in cache for conversationId: {ConversationId}", conversationId);
        }

        return Task.CompletedTask;
    }

    public Task InvalidateChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"chat:history:{conversationId}";
            _cache.Remove(key);
            _trackedKeys.TryRemove(key, out _); // Untrack the key
            _logger.LogDebug("Cache REMOVE for ChatHistory key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating ChatHistory cache for conversationId: {ConversationId}", conversationId);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Conversation Metadata Caching

    public Task<ConversationMetadata?> GetConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"chat:metadata:{conversationId}";
            var value = _cache.Get<ConversationMetadata>(key);
            _logger.LogDebug("Cache GET for ConversationMetadata key: {Key}, Found: {Found}", key, value != null);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ConversationMetadata from cache for conversationId: {ConversationId}", conversationId);
            return Task.FromResult<ConversationMetadata?>(null);
        }
    }

    public Task SetConversationMetadataAsync(string conversationId, ConversationMetadata metadata, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"chat:metadata:{conversationId}";
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };

            options.RegisterPostEvictionCallback((evictedKey, value, reason, state) =>
            {
                _trackedKeys.TryRemove(evictedKey.ToString()!, out _);
            });

            _cache.Set(key, metadata, options);
            _trackedKeys.TryAdd(key, 0);
            _logger.LogDebug("Cache SET for ConversationMetadata key: {Key}, Expiration: {Expiration}", key, expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting ConversationMetadata in cache for conversationId: {ConversationId}", conversationId);
        }

        return Task.CompletedTask;
    }

    public Task InvalidateConversationMetadataAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"chat:metadata:{conversationId}";
            _cache.Remove(key);
            _trackedKeys.TryRemove(key, out _);
            _logger.LogDebug("Cache REMOVE for ConversationMetadata key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating ConversationMetadata cache for conversationId: {ConversationId}", conversationId);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Batch Operations

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Tüm tracked key'leri sil
            var keysToRemove = _trackedKeys.Keys.ToList();
            var removedCount = 0;

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _trackedKeys.TryRemove(key, out _);
                removedCount++;
            }

            _logger.LogInformation("All cache entries cleared - Count: {Count}", removedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache values");
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Generic Cache Operations

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var value = _cache.Get<T>(key);
            _logger.LogDebug("Cache GET for key: {Key}, Found: {Found}", key, value != null);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };

            options.RegisterPostEvictionCallback((evictedKey, val, reason, state) =>
            {
                _trackedKeys.TryRemove(evictedKey.ToString()!, out _);
            });

            _cache.Set(key, value, options);
            _trackedKeys.TryAdd(key, 0);
            _logger.LogDebug("Cache SET for key: {Key}, Expiration: {Expiration}", key, expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _cache.Remove(key);
            _trackedKeys.TryRemove(key, out _);
            _logger.LogDebug("Cache REMOVE for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Health Check

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // For in-memory cache, we can always consider it healthy
            // since it's part of the application process
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking InMemoryCache health");
            return Task.FromResult(false);
        }
    }

    #endregion
}