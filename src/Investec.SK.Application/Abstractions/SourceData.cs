using Investec.SK.Application.Responses;

namespace Investec.SK.Application.Abstractions;

/// <summary>
/// Everything a source-level mapping pass produced: the records that mapped cleanly, and the
/// records that did not (with the reason). Repositories cache and return the whole thing so a
/// cache hit never re-loses the skips that a fresh fetch would have reported.
/// </summary>
public sealed record SourceData<T>(IReadOnlyList<T> Items, IReadOnlyList<SkippedRecord> Skipped);
