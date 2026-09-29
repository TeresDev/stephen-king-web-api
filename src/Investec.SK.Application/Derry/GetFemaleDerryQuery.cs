using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Queries;
using Investec.SK.Application.Responses;
using Investec.SK.Domain;

namespace Investec.SK.Application.Derry;

/// <summary>
/// Female villains resident in Derry. Deliberately depends on the villains source ONLY, so it
/// still answers in full while the books source is unavailable.
/// </summary>
public sealed class GetFemaleDerryQuery(IVillainRepository villains, TimeProvider timeProvider)
{
    public static IReadOnlyList<SortKey> AllowedSortKeys { get; } = [SortKey.VillainName];

    public async Task<QueryResult<IReadOnlyList<DerryVillain>>> ExecuteAsync(
        int? page,
        int? pageSize,
        string? sort,
        string? dir,
        CancellationToken cancellationToken)
    {
        if (!PagingRequest.TryCreate(page, pageSize, sort, dir, null, AllowedSortKeys,
                                     out var request, out var validationErrors))
        {
            return QueryResult<IReadOnlyList<DerryVillain>>.Invalid(validationErrors);
        }

        var result = await villains.GetAllAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return QueryResult<IReadOnlyList<DerryVillain>>.Unavailable([result.Failure!]);
        }

        var matched = result.Value!
            .Where(v => v.Gender == Gender.Female && v.Notes.IsResidentOfDerry)
            .Select(v => new DerryVillain(
                v.Name.Value,
                v.Gender.ToString(),
                v.Status,
                v.Notes.MatchedDerryNote,
                v.BookRefs.Select(b => b.Title).ToArray()))
            .ToArray();

        var ordered = request.Direction == SortDirection.Desc
            ? matched.OrderByDescending(v => v.VillainName, StringComparer.OrdinalIgnoreCase).ToArray()
            : matched.OrderBy(v => v.VillainName, StringComparer.OrdinalIgnoreCase).ToArray();

        var pageItems = ordered.Skip(request.Skip).Take(request.PageSize).ToArray();

        var meta = ResponseMeta.Create(
            request.Page,
            request.PageSize,
            ordered.Length,
            pageItems.Length,
            request.SortName,
            request.DirectionName,
            degraded: false,
            timeProvider.GetUtcNow());

        return QueryResult<IReadOnlyList<DerryVillain>>.Ok(
            new ApiResponse<IReadOnlyList<DerryVillain>>(pageItems, meta, [], []));
    }
}
