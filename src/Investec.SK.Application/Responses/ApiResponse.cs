using Investec.SK.Application.Abstractions;

namespace Investec.SK.Application.Responses;

/// <summary>Paging and provenance for a response. Paging is measured in rows, never year-groups.</summary>
public sealed record ResponseMeta(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    int ItemsOnPage,
    string Sort,
    string Direction,
    bool Degraded,
    DateTimeOffset RetrievedAt)
{
    public static ResponseMeta Create(
        int page,
        int pageSize,
        int totalItems,
        int itemsOnPage,
        string sort,
        string direction,
        bool degraded,
        DateTimeOffset retrievedAt)
    {
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new ResponseMeta(
            page, pageSize, totalItems, totalPages, itemsOnPage,
            sort, direction, degraded, retrievedAt);
    }
}

/// <summary>
/// The single envelope every endpoint returns. <c>Errors</c> names upstream sources that failed;
/// <c>Skipped</c> names records that could not be assembled.
/// </summary>
public sealed record ApiResponse<T>(
    T Data,
    ResponseMeta Meta,
    IReadOnlyList<UpstreamFailure> Errors,
    IReadOnlyList<SkippedRecord> Skipped);
