using Investec.SK.Application.Responses;

namespace Investec.SK.Infrastructure.Mapping;

/// <summary>
/// The result of mapping one upstream record: either a domain object (possibly with skips for
/// parts of it) or no object and the reason it was rejected.
/// </summary>
public sealed record MappingOutcome<T>(T? Value, IReadOnlyList<SkippedRecord> Skipped)
{
    public static MappingOutcome<T> Mapped(T value, IReadOnlyList<SkippedRecord>? skipped = null) =>
        new(value, skipped ?? []);

    public static MappingOutcome<T> Rejected(SkippedRecord skipped) => new(default, [skipped]);
}

public sealed record MappedSet<T>(IReadOnlyList<T> Items, IReadOnlyList<SkippedRecord> Skipped);
