namespace Investec.SK.Application.Maine;

/// <summary>
/// Rows are the source of truth. <see cref="TakePage"/> slices rows; <see cref="GroupByYear"/>
/// then groups only that page, purely for presentation. A year whose rows straddle a boundary
/// therefore appears on both pages — intended, and asserted in tests.
/// </summary>
public static class MainePager
{
    public static IReadOnlyList<MaineRow> TakePage(
        IReadOnlyList<MaineRow> sortedRows,
        int page,
        int pageSize)
    {
        // Computed in long and clamped for the same reason as PagingRequest.Skip: (page-1)*pageSize
        // can overflow a 32-bit int and wrap negative, and Enumerable.Skip treats a negative count
        // as zero, silently handing back page 1's rows for an out-of-range page.
        var skipLong = (long)(page - 1) * pageSize;
        var skip = skipLong > int.MaxValue ? int.MaxValue : (int)skipLong;

        return sortedRows.Skip(skip).Take(pageSize).ToArray();
    }

    /// <summary>
    /// Groups a page's rows by year, emitting groups in the order their first row appears so the
    /// global sort order survives grouping rather than being reset within each year.
    /// </summary>
    public static IReadOnlyList<MaineYearGroup> GroupByYear(IReadOnlyList<MaineRow> pageRows)
    {
        var order = new List<int>();
        var byYear = new Dictionary<int, List<MaineItem>>();

        foreach (var row in pageRows)
        {
            if (!byYear.TryGetValue(row.Year, out var items))
            {
                items = [];
                byYear[row.Year] = items;
                order.Add(row.Year);
            }

            items.Add(new MaineItem(row.BookName, row.Isbn, row.Pages, row.VillainName));
        }

        return order.Select(year => new MaineYearGroup(year, byYear[year])).ToArray();
    }
}
