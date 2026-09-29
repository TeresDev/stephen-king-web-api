using Investec.SK.Application.Queries;

namespace Investec.SK.Application.Maine;

/// <summary>
/// Orders rows before paging. Tie-breaks are always ascending and always total, so the same
/// request never returns a differently shuffled page.
/// </summary>
public static class MaineRowSorter
{
    private static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;

    public static IReadOnlyList<MaineRow> Sort(
        IReadOnlyList<MaineRow> rows,
        SortKey key,
        SortDirection direction)
    {
        var descending = direction == SortDirection.Desc;

        IOrderedEnumerable<MaineRow> ordered = key switch
        {
            SortKey.Pages => descending
                ? rows.OrderByDescending(r => r.Pages)
                : rows.OrderBy(r => r.Pages),

            SortKey.BookName => descending
                ? rows.OrderByDescending(r => r.BookName, Names)
                : rows.OrderBy(r => r.BookName, Names),

            SortKey.VillainName => descending
                ? rows.OrderByDescending(r => r.VillainName, Names)
                : rows.OrderBy(r => r.VillainName, Names),

            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unhandled sort key"),
        };

        ordered = key switch
        {
            SortKey.Pages => ordered.ThenBy(r => r.BookName, Names).ThenBy(r => r.VillainName, Names),
            SortKey.BookName => ordered.ThenBy(r => r.VillainName, Names),
            SortKey.VillainName => ordered.ThenBy(r => r.BookName, Names),
            _ => ordered,
        };

        return ordered.ToArray();
    }
}
