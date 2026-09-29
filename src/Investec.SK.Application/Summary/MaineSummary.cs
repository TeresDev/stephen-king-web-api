using Investec.SK.Application.Maine;

namespace Investec.SK.Application.Summary;

public sealed record YearCount(int Year, int Count);

public sealed record MaineSummary(
    int TotalRows,
    IReadOnlyList<YearCount> CountsByYear,
    IReadOnlyList<MaineRow> TopByPages);
