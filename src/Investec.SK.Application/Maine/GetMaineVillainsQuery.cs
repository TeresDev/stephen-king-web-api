using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Queries;
using Investec.SK.Application.Responses;

namespace Investec.SK.Application.Maine;

/// <summary>
/// Maine residents joined to their books, sorted and paged by row, then grouped by year.
/// Needs BOTH upstream sources, so either one failing is fatal to this endpoint.
/// </summary>
public sealed class GetMaineVillainsQuery(
    IVillainRepository villains,
    IBookRepository books,
    TimeProvider timeProvider)
{
    public static IReadOnlyList<SortKey> AllowedSortKeys { get; } =
        [SortKey.Pages, SortKey.BookName, SortKey.VillainName];

    public async Task<QueryResult<IReadOnlyList<MaineYearGroup>>> ExecuteAsync(
        int? page,
        int? pageSize,
        string? sort,
        string? dir,
        int? year,
        CancellationToken cancellationToken)
    {
        if (!PagingRequest.TryCreate(page, pageSize, sort, dir, year, AllowedSortKeys,
                                     out var request, out var validationErrors))
        {
            return QueryResult<IReadOnlyList<MaineYearGroup>>.Invalid(validationErrors);
        }

        var villainTask = villains.GetAllAsync(cancellationToken);
        var bookTask = books.GetAllAsync(cancellationToken);
        await Task.WhenAll(villainTask, bookTask);

        var villainResult = await villainTask;
        var bookResult = await bookTask;

        var failures = new List<UpstreamFailure>();
        if (!villainResult.IsSuccess) failures.Add(villainResult.Failure!);
        if (!bookResult.IsSuccess) failures.Add(bookResult.Failure!);

        if (failures.Count == 2)
        {
            return QueryResult<IReadOnlyList<MaineYearGroup>>.AllUnavailable(failures);
        }

        if (failures.Count == 1)
        {
            return QueryResult<IReadOnlyList<MaineYearGroup>>.Unavailable(failures);
        }

        var rowSet = MaineRowBuilder.Build(villainResult.Value!.Items, bookResult.Value!.Items);

        var skipped = villainResult.Value.Skipped
            .Concat(bookResult.Value.Skipped)
            .Concat(rowSet.Skipped)
            .ToArray();

        // Declared explicitly: a `var` here fails to infer a common type between
        // IReadOnlyList<MaineRow> and MaineRow[].
        IReadOnlyList<MaineRow> filtered = request.Year is null
            ? rowSet.Rows
            : rowSet.Rows.Where(r => r.Year == request.Year).ToArray();

        var sorted = MaineRowSorter.Sort(filtered, request.Sort, request.Direction);
        var pageRows = MainePager.TakePage(sorted, request.Page, request.PageSize);
        var groups = MainePager.GroupByYear(pageRows);

        var meta = ResponseMeta.Create(
            request.Page,
            request.PageSize,
            filtered.Count,
            pageRows.Count,
            request.SortName,
            request.DirectionName,
            degraded: skipped.Length > 0,
            timeProvider.GetUtcNow());

        return QueryResult<IReadOnlyList<MaineYearGroup>>.Ok(
            new ApiResponse<IReadOnlyList<MaineYearGroup>>(groups, meta, [], skipped));
    }
}
