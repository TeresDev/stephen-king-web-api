namespace Investec.SK.Domain;

/// <summary>
/// The residence-bearing view of a villain's note list. <see cref="MatchedDerryNote"/> keeps the
/// original spelling so a response can show which variant matched.
/// </summary>
public sealed record ResidenceNotes
{
    private ResidenceNotes(
        IReadOnlyList<string> raw,
        bool isResidentOfMaine,
        bool isResidentOfDerry,
        string? matchedDerryNote)
    {
        Raw = raw;
        IsResidentOfMaine = isResidentOfMaine;
        IsResidentOfDerry = isResidentOfDerry;
        MatchedDerryNote = matchedDerryNote;
    }

    public IReadOnlyList<string> Raw { get; }

    public bool IsResidentOfMaine { get; }

    public bool IsResidentOfDerry { get; }

    public string? MatchedDerryNote { get; }

    public static ResidenceNotes Empty { get; } = new([], false, false, null);

    public static ResidenceNotes From(IEnumerable<string?>? notes)
    {
        if (notes is null)
        {
            return Empty;
        }

        var kept = notes.Where(n => !string.IsNullOrWhiteSpace(n))
                        .Select(n => n!.Trim())
                        .ToArray();

        if (kept.Length == 0)
        {
            return Empty;
        }

        var maine = kept.Any(ResidenceMatcher.IsMaine);
        var derryNote = kept.FirstOrDefault(ResidenceMatcher.IsDerry);

        return new ResidenceNotes(kept, maine, derryNote is not null, derryNote);
    }
}
