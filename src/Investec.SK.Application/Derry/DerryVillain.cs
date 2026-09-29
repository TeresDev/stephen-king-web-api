namespace Investec.SK.Application.Derry;

/// <summary>
/// A female Derry resident. <paramref name="ResidenceNote"/> echoes the note that matched so the
/// caller can see which spelling ("Derry residents" or the tolerated "Dery") triggered it.
/// <paramref name="Books"/> comes from the villain feed's own embedded titles.
/// </summary>
public sealed record DerryVillain(
    string VillainName,
    string Gender,
    string? Status,
    string? ResidenceNote,
    IReadOnlyList<string> Books);
