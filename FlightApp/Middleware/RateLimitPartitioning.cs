using System.Threading.RateLimiting;
using FlightKS.Models.Config;
using StackExchange.Redis;

namespace FlightKS.Middleware;

/// <summary>
/// Centralizes partition-key derivation and per-tier limiter construction.
/// THIS IS THE SINGLE SEAM FOR PHASE 8 (Redis distributed store).
/// To add Redis: implement the Distributed branch inside each Get*Partition method.
/// No other file needs to change for the switch from in-memory to distributed limiting.
/// </summary>
public static class RateLimitPartitioning
{
    public const string PublicSearchPolicy = "public-search";
    public const string SensitiveWritesPolicy = "sensitive-writes";

    /// <summary>
    /// Returns "user:{sub}" for authenticated requests, "ip:{address}" for anonymous ones.
    /// The prefix prevents a crafted sub claim from colliding with an IP bucket.
    /// RemoteIpAddress is accurate here because UseForwardedHeaders runs first (Phase 2).
    /// Matches the "sub" claim used by CurrentUserAccessor (MapInboundClaims = false).
    /// </summary>
    public static string GetPartitionKey(HttpContext context)
    {
        var sub = context.User.FindFirst("sub")?.Value;
        if (sub is not null)
            return $"user:{sub}";

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    /// <summary>
    /// Token-bucket partition for anonymous search/autocomplete endpoints.
    /// Distributed: uses a Redis sliding-window with TokensPerPeriod/ReplenishmentPeriod — the
    /// token-bucket burst semantics (TokenLimit) don't translate to a distributed store, so the
    /// sustained rate is enforced instead. In-memory retains the full burst/replenishment model.
    /// </summary>
    public static RateLimitPartition<string> GetPublicSearchPartition(
        string key, PublicSearchOptions options, RateLimitStore store, IConnectionMultiplexer? multiplexer = null)
    {
        if (store == RateLimitStore.Distributed && multiplexer is not null)
            return RateLimitPartition.Get(key, k =>
                new RedisRateLimiter(multiplexer, k, options.TokensPerPeriod, options.ReplenishmentPeriod, keyPrefix: "ps"));

        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = options.TokenLimit,
            TokensPerPeriod = options.TokensPerPeriod,
            ReplenishmentPeriod = options.ReplenishmentPeriod,
            QueueLimit = options.QueueLimit,
            AutoReplenishment = true,
        });
    }

    /// <summary>
    /// Sliding-window partition for money/inventory mutation endpoints (bookings, payments,
    /// refunds, seat reservations). PermitLimit must stay above ~27 (9 passengers × 3 calls)
    /// to avoid blocking legitimate full-group bookings.
    /// Distributed: uses RedisRateLimiter — sliding-window maps directly, no translation needed.
    /// </summary>
    public static RateLimitPartition<string> GetSensitiveWritesPartition(
        string key, SensitiveWritesOptions options, RateLimitStore store, IConnectionMultiplexer? multiplexer = null)
    {
        if (store == RateLimitStore.Distributed && multiplexer is not null)
            return RateLimitPartition.Get(key, k =>
                new RedisRateLimiter(multiplexer, k, options.PermitLimit, options.Window, keyPrefix: "sw"));

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = options.Window,
            SegmentsPerWindow = options.SegmentsPerWindow,
            QueueLimit = 0,
        });
    }

    /// <summary>
    /// Sliding-window fallback partition for all other routes.
    /// Must comfortably exceed the heaviest page fan-out (booking-confirmation = 5 calls).
    /// Distributed: uses RedisRateLimiter — sliding-window maps directly, no translation needed.
    /// </summary>
    public static RateLimitPartition<string> GetGlobalPartition(
        string key, GlobalOptions options, RateLimitStore store, IConnectionMultiplexer? multiplexer = null)
    {
        if (store == RateLimitStore.Distributed && multiplexer is not null)
            return RateLimitPartition.Get(key, k =>
                new RedisRateLimiter(multiplexer, k, options.PermitLimit, options.Window, keyPrefix: "gl"));

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = options.Window,
            SegmentsPerWindow = options.SegmentsPerWindow,
            QueueLimit = 0,
        });
    }
}
