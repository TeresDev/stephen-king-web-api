using System.Text.Json.Serialization;

namespace Investec.SK.Infrastructure.Contracts;

/// <summary>
/// The books feed, which uses PascalCase for most fields and lowercase for a few. Every property
/// is nullable: this type describes what upstream might send, not what is valid.
/// </summary>
public sealed record BookContract
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("Year")]
    public int? Year { get; init; }

    [JsonPropertyName("Title")]
    public string? Title { get; init; }

    [JsonPropertyName("Publisher")]
    public string? Publisher { get; init; }

    [JsonPropertyName("ISBN")]
    public string? Isbn { get; init; }

    [JsonPropertyName("Pages")]
    public int? Pages { get; init; }

    [JsonPropertyName("Notes")]
    public IReadOnlyList<string?>? Notes { get; init; }

    [JsonPropertyName("villains")]
    public IReadOnlyList<ResourceRef>? Villains { get; init; }
}
