using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Maine;
using Investec.SK.Application.Queries;
using Investec.SK.Application.Responses;

namespace Investec.SK.Application.Summary;

/// <summary>Row counts per publish year plus the longest books, over the same Maine row set.</summary>
public sealed class GetMaineSummaryQuery(
    IVillainRepository villains,
    IBookRepository books,
    TimeProvider timeProvider)
{
    public const int DefaultTop = 5;
    public const int MaxTop = 50;

    public async Task<QueryResult<MaineSummary>> ExecuteAsync(int? top, CancellationToken cancellationToken)
    {
        var resolvedTop = top ?? DefaultTop;
        if (resolvedTop < 1 || resolvedTop > MaxTop)
        {
            return QueryResult<MaineSummary>.Invalid(
                [new ValidationError("top", $"must be between 1 and {MaxTop}")]);
        }

        var villainTask = villains.GetAllAsync(cancellationToken);
        var bookTask = books.GetAllAsync(cancellationToken);
        await Task.WhenAll(villainTask, bookTask);

        var villainResult = await villainTask;
        var bookResult = await bookTask;

        var failures = new List<UpstreamFailure>();
        if (!villainResult.IsSuccess) failures.Add(villainResult.Failure!);
        if (!bookResult.IsSuccess) failures.Add(bookResult.Failure!);

        if (failures.Count == 2) return QueryResult<MaineSummary>.AllUnavailable(failures);
        if (failures.Count == 1) return QueryResult<MaineSummary>.Unavailable(failures);

        var rowSet = MaineRowBuilder.Build(villainResult.Value!.Items, bookResult.Value!.Items);

        var skipped = villainResult.Value.Skipped
            .Concat(bookResult.Value.Skipped)
            .Concat(rowSet.Skipped)
            .ToArray();

        var countsByYear = rowSet.Rows
            .GroupBy(r => r.Year)
            .Select(g => new YearCount(g.Key, g.Count()))
            .OrderByDescending(c => c.Year)
            .ToArray();

        var topByPages = rowSet.Rows
            .GroupBy(r => r.Isbn, StringComparer.Ordinal)
            .Select(g => new TopBook(
                g.First().BookName,
                g.Key,
                g.First().Pages,
                g.First().Year,
                g.Select(r => r.VillainName).Distinct(StringComparer.OrdinalIgnoreCase).Count()))
            .OrderByDescending(b => b.Pages)
            .ThenBy(b => b.BookName, StringComparer.OrdinalIgnoreCase)
            .Take(resolvedTop)
            .ToArray();

        var summary = new MaineSummary(rowSet.Rows.Count, countsByYear, topByPages);

        var meta = ResponseMeta.Create(
            page: 1,
            pageSize: resolvedTop,
            totalItems: rowSet.Rows.Count,
            itemsOnPage: topByPages.Length,
            sort: "pages",
            direction: "desc",
            degraded: skipped.Length > 0,
            timeProvider.GetUtcNow());

        return QueryResult<MaineSummary>.Ok(
            new ApiResponse<MaineSummary>(summary, meta, [], skipped));
    }
}
