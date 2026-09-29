namespace Investec.SK.Domain;

/// <summary>
/// Upstream notes are a free-text tag list ("Residents of Maine", "Derry residents", "Derry").
/// One normalisation followed by one alias lookup keeps every spelling rule in a single place.
/// </summary>
public static class ResidenceMatcher
{
    private const string LeadingPrefix = "residents of ";

    private static readonly string[] TrailingSuffixes = [" residents", " resident"];

    private static readonly HashSet<string> MaineAliases =
        new(StringComparer.Ordinal) { "maine" };

    private static readonly HashSet<string> DerryAliases =
        new(StringComparer.Ordinal) { "derry", "dery" };

    /// <summary>
    /// Trims, collapses internal whitespace, lowercases, then strips a leading
    /// "residents of " or a trailing " residents"/" resident".
    /// </summary>
    public static string Normalise(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var collapsed = string.Join(' ', raw.Split(' ', StringSplitOptions.RemoveEmptyEntries
                                                      | StringSplitOptions.TrimEntries))
                              .ToLowerInvariant();

        if (collapsed.StartsWith(LeadingPrefix, StringComparison.Ordinal))
        {
            collapsed = collapsed[LeadingPrefix.Length..];
        }

        foreach (var suffix in TrailingSuffixes)
        {
            if (collapsed.EndsWith(suffix, StringComparison.Ordinal))
            {
                collapsed = collapsed[..^suffix.Length];
                break;
            }
        }

        return collapsed.Trim();
    }

    public static bool IsMaine(string? raw) => MaineAliases.Contains(Normalise(raw));

    public static bool IsDerry(string? raw) => DerryAliases.Contains(Normalise(raw));
}
