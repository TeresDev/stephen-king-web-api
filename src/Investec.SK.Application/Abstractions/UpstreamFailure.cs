namespace Investec.SK.Application.Abstractions;

/// <summary>Why an upstream source could not be read. Surfaced verbatim in the response's errors[].</summary>
public enum UpstreamFailureKind
{
    Timeout,
    CircuitOpen,
    ServerError,
    NotFound,
    BadPayload,
    Transport,
}

/// <summary>
/// A named, machine-readable upstream failure. <paramref name="Status"/> is null when the call
/// never produced an HTTP response (transport error, timeout, open circuit).
/// </summary>
public sealed record UpstreamFailure(
    string Source,
    string Resource,
    int? Status,
    UpstreamFailureKind Kind,
    string Reason);

public static class UpstreamSources
{
    public const string Books = "books";
    public const string Villains = "villains";
}
