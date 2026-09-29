namespace Investec.SK.Domain;

/// <summary>
/// A villain that passed validation. <see cref="BookRefs"/> may legitimately be empty — 2 of the
/// 38 Maine residents in the live feed appear in no book, and that is reported, not hidden.
/// </summary>
public sealed record Villain(
    VillainId Id,
    VillainName Name,
    Gender Gender,
    string? Status,
    ResidenceNotes Notes,
    IReadOnlyList<BookLink> BookRefs);
