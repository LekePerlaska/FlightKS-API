namespace FlightKS.Models.Config;

public enum RateLimitStore
{
    InMemory,
    Distributed,
}

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// InMemory (default, single-instance) or Distributed (Redis, for multi-replica deployments).
    /// Flip to Distributed only after Redis is running and RedisConnectionString is set.
    /// See docs/rate-limiting-plan.md Phase 8.
    /// </summary>
    public RateLimitStore Store { get; init; } = RateLimitStore.InMemory;

    /// <summary>
    /// StackExchange.Redis connection string. Only used when Store = Distributed.
    /// Docker: "redis:6379" (container name). Local: "localhost:6379".
    /// </summary>
    public string RedisConnectionString { get; init; } = "localhost:6379";

    public PublicSearchOptions PublicSearch { get; init; } = new();
    public SensitiveWritesOptions SensitiveWrites { get; init; } = new();
    public GlobalOptions Global { get; init; } = new();
}

/// <summary>Token-bucket policy for anonymous, DB-heavy search and autocomplete endpoints.</summary>
public class PublicSearchOptions
{
    /// <summary>Maximum tokens (burst capacity). Accommodates a user typing quickly.</summary>
    public int TokenLimit { get; init; } = 40;

    /// <summary>Tokens added per replenishment period.</summary>
    public int TokensPerPeriod { get; init; } = 20;

    /// <summary>How often tokens are replenished.</summary>
    public TimeSpan ReplenishmentPeriod { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Requests queued when the bucket is empty before rejecting. 0 = reject immediately.</summary>
    public int QueueLimit { get; init; } = 0;
}

/// <summary>
/// Sliding-window policy for money/inventory mutation endpoints.
/// Hard floor: max 9 passengers per booking × (reserve + passenger + baggage calls) must fit in the window.
/// Keep PermitLimit above ~30 to avoid blocking legitimate full-group bookings.
/// </summary>
public class SensitiveWritesOptions
{
    public int PermitLimit { get; init; } = 30;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
    public int SegmentsPerWindow { get; init; } = 6;
}

/// <summary>
/// Sliding-window fallback for all other routes.
/// Must comfortably exceed the heaviest legitimate page fan-out
/// (e.g. booking-confirmation: summary + price-summary + tickets + seat-summary + confirmation = 5 calls).
/// </summary>
public class GlobalOptions
{
    public int PermitLimit { get; init; } = 100;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
    public int SegmentsPerWindow { get; init; } = 6;
}
