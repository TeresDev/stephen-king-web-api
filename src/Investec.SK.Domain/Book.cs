namespace Investec.SK.Domain;

/// <summary>
/// A book that passed every validation rule. An instance cannot represent an invalid book —
/// upstream records that fail validation are reported as skipped instead of being constructed.
/// </summary>
public sealed record Book(
    BookId Id,
    BookTitle Title,
    PublishYear Year,
    Isbn Isbn,
    PageCount Pages,
    string? Publisher,
    IReadOnlyList<VillainId> VillainRefs);
