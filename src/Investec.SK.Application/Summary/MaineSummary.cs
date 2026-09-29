using Investec.SK.Application.Maine;

namespace Investec.SK.Application.Summary;

public sealed record YearCount(int Year, int Count);

/// <summary>
/// One of the longest books among Maine residents' appearances. Distinct per book: a book with
/// several Maine-resident villains appears ONCE here, with <paramref name="VillainCount"/>
/// recording how many. Rows remain the unit elsewhere; this list is deliberately book-shaped.
/// </summary>
public sealed record TopBook(
    string BookName,
    string Isbn,
    int Pages,
    int Year,
    int VillainCount);

public sealed record MaineSummary(
    int TotalRows,
    IReadOnlyList<YearCount> CountsByYear,
    IReadOnlyList<TopBook> TopByPages);
