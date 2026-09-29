using Investec.SK.Application.Responses;

namespace Investec.SK.Application.Maine;

/// <summary>
/// One flattened (villain, book) pair. Rows — not year groups — are the unit of sorting and
/// paging; grouping by year happens afterwards, purely for presentation.
/// </summary>
public sealed record MaineRow(
    string BookName,
    string Isbn,
    int Pages,
    string VillainName,
    int Year);

public sealed record MaineRowSet(
    IReadOnlyList<MaineRow> Rows,
    IReadOnlyList<SkippedRecord> Skipped);
