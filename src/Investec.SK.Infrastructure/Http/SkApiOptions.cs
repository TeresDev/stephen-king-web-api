namespace Investec.SK.Infrastructure.Http;

/// <summary>
/// Upstream is a free Render instance that cold-starts, so timeouts, retry and caching are
/// load-bearing rather than decorative. Bound from the "StephenKingApi" configuration section.
/// </summary>
public sealed class SkApiOptions
{
    public const string SectionName = "StephenKingApi";

    public Uri BaseAddress { get; set; } = new("https://stephen-king-api.onrender.com/api/");

    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(15);

    public int RetryCount { get; set; } = 3;

    /// <summary>How long a good payload is reused. Expired plus upstream down means 502/503 —
    /// stale data is never served.</summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);
}
