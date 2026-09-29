using System.Text.Json.Serialization;

namespace Investec.SK.Infrastructure.Contracts;

/// <summary>
/// A cross-reference embedded in either feed. Books carry { name, url } for villains; villains
/// carry { title, url } for books. One type covers both, with the unused member left null.
/// </summary>
public sealed record ResourceRef
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
