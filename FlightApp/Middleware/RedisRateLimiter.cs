using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace FlightKS.Middleware;

/// <summary>
/// Sliding-window rate limiter backed by Redis, using an atomic Lua script.
/// One instance is created per partition key and cached by PartitionedRateLimiter.
/// Fails open on Redis errors — requests are allowed through rather than hard-blocked,
/// which preserves availability when Redis is temporarily unreachable.
/// </summary>
internal sealed class RedisRateLimiter : RateLimiter
{
    private readonly IDatabase _db;
    private readonly string _key;
    private readonly int _permitLimit;
    private readonly long _windowMs;

    // Atomic sliding-window via a sorted set.
    // Removes stale members, checks count, adds new member if under limit, sets expiry.
    // Returns: { acquired (1=yes/0=no), remaining_permits, retry_after_ms }
    private const string SlidingWindowScript = @"
local key = KEYS[1]
local limit = tonumber(ARGV[1])
local window_ms = tonumber(ARGV[2])
local now_ms = tonumber(ARGV[3])
local member = ARGV[4]
redis.call('ZREMRANGEBYSCORE', key, 0, now_ms - window_ms)
local count = tonumber(redis.call('ZCARD', key))
if count < limit then
    redis.call('ZADD', key, now_ms, member)
    redis.call('PEXPIRE', key, window_ms)
    return {1, limit - count - 1, 0}
end
local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
local retry_ms = window_ms
if #oldest > 1 then
    local age_ms = now_ms - tonumber(oldest[2])
    if age_ms < window_ms then retry_ms = window_ms - age_ms end
end
return {0, 0, retry_ms}
";

    /// <param name="keyPrefix">Short tier identifier (e.g. "ps", "sw", "gl") — namespaces the
    /// Redis key so global, public-search, and sensitive-writes limiters don't share a sorted set
    /// even when they share the same partition key (user sub or IP address).</param>
    public RedisRateLimiter(IConnectionMultiplexer mux, string partitionKey, int permitLimit, TimeSpan window,
        string keyPrefix = "gl")
    {
        _db = mux.GetDatabase();
        _key = $"rl:{keyPrefix}:{partitionKey}";
        _permitLimit = permitLimit;
        _windowMs = (long)window.TotalMilliseconds;
    }

    public override TimeSpan? IdleDuration => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        // Redis requires async I/O — synchronous acquire always fails so the caller
        // retries via WaitAsync, which is what the rate-limiting middleware always uses.
        return RedisLease.Failed(TimeSpan.FromMilliseconds(_windowMs));
    }

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount, CancellationToken cancellationToken)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var member = $"{nowMs}-{Guid.NewGuid():N}";
        try
        {
            var result = (RedisValue[]?)await _db.ScriptEvaluateAsync(
                SlidingWindowScript,
                keys: [(RedisKey)_key],
                values: [(RedisValue)_permitLimit, _windowMs, nowMs, member]);

            if (result is null || result.Length < 3)
                return RedisLease.Failed(TimeSpan.FromMilliseconds(_windowMs));

            return (int)result[0] == 1
                ? RedisLease.Acquired()
                : RedisLease.Failed(TimeSpan.FromMilliseconds((long)result[2]));
        }
        catch
        {
            // Fail open: let the request through rather than blocking all traffic on Redis outage.
            return RedisLease.Acquired();
        }
    }
}

/// <summary>Lease returned by RedisRateLimiter. Exposes RetryAfter metadata on rejection.</summary>
internal sealed class RedisLease : RateLimitLease
{
    private static readonly RedisLease AcquiredLease = new(true, TimeSpan.Zero);
    private readonly TimeSpan _retryAfter;

    private RedisLease(bool isAcquired, TimeSpan retryAfter)
    {
        IsAcquired = isAcquired;
        _retryAfter = retryAfter;
    }

    public static RedisLease Acquired() => AcquiredLease;
    public static RedisLease Failed(TimeSpan retryAfter) => new(false, retryAfter);

    public override bool IsAcquired { get; }

    public override IEnumerable<string> MetadataNames =>
        IsAcquired ? [] : [MetadataName.RetryAfter.Name];

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        if (!IsAcquired && metadataName == MetadataName.RetryAfter.Name)
        {
            metadata = _retryAfter;
            return true;
        }
        metadata = null;
        return false;
    }
}
