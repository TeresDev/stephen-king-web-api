using System.Text.Json.Serialization;

namespace Investec.SK.Infrastructure.Contracts;

/// <summary>The villains feed, which uses lowercase field names throughout.</summary>
public sealed record VillainContract
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("notes")]
    public IReadOnlyList<string?>? Notes { get; init; }

    [JsonPropertyName("books")]
    public IReadOnlyList<ResourceRef>? Books { get; init; }
}
