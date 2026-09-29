namespace Investec.SK.Application.Responses;

/// <summary>Why a record could not be assembled into a response row.</summary>
public enum SkipReason
{
    NoLinkedBooks,
    UnresolvedBookReference,
    InvalidPageCount,
    InvalidIsbn,
    InvalidYear,
    MissingTitle,
    MissingVillainName,

    /// <summary>Upstream supplied a missing or non-positive id, so the record cannot be joined.</summary>
    InvalidIdentifier,
}

/// <summary>
/// A record that was dropped, and why. Nothing disappears silently: every row that does not make
/// it into <c>data</c> is accounted for here.
/// </summary>
public sealed record SkippedRecord(
    string Kind,
    string Identifier,
    SkipReason Reason,
    string Detail);
