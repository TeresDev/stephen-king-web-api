using System.Text.Json.Serialization;

namespace Investec.SK.Infrastructure.Contracts;

/// <summary>Both feeds wrap their payload in { "data": [ ... ] }.</summary>
public sealed record UpstreamEnvelope<T>
{
    [JsonPropertyName("data")]
    public IReadOnlyList<T>? Data { get; init; }
}
