namespace AI.Application.Configuration;

/// <summary>
/// Rate limiting configuration settings
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Enable or disable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Fixed window rate limit settings
    /// </summary>
    public FixedWindowSettings FixedWindow { get; set; } = new();

    /// <summary>
    /// Sliding window rate limit settings
    /// </summary>
    public SlidingWindowSettings SlidingWindow { get; set; } = new();

    /// <summary>
    /// Token bucket rate limit settings
    /// </summary>
    public TokenBucketSettings TokenBucket { get; set; } = new();

    /// <summary>
    /// Concurrency limit settings
    /// </summary>
    public ConcurrencySettings Concurrency { get; set; } = new();
}

/// <summary>
/// Fixed window rate limit configuration
/// </summary>
public class FixedWindowSettings
{
    /// <summary>
    /// Maximum number of requests per window
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Queue limit for requests exceeding the limit
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}

/// <summary>
/// Sliding window rate limit configuration
/// </summary>
public class SlidingWindowSettings
{
    /// <summary>
    /// Maximum number of requests per window
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Number of segments per window
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 6;

    /// <summary>
    /// Queue limit for requests exceeding the limit
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}

/// <summary>
/// Token bucket rate limit configuration
/// </summary>
public class TokenBucketSettings
{
    /// <summary>
    /// Maximum tokens in bucket
    /// </summary>
    public int TokenLimit { get; set; } = 100;

    /// <summary>
    /// Tokens added per replenishment period
    /// </summary>
    public int TokensPerPeriod { get; set; } = 20;

    /// <summary>
    /// Replenishment period in seconds
    /// </summary>
    public int ReplenishmentPeriodSeconds { get; set; } = 10;

    /// <summary>
    /// Queue limit for requests exceeding the limit
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}

/// <summary>
/// Concurrency limit configuration
/// </summary>
public class ConcurrencySettings
{
    /// <summary>
    /// Maximum concurrent requests
    /// </summary>
    public int PermitLimit { get; set; } = 50;

    /// <summary>
    /// Queue limit for requests exceeding the limit
    /// </summary>
    public int QueueLimit { get; set; } = 25;
}
