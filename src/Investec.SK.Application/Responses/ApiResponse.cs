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
/// <param name="Data">The requested rows, or an empty collection when none matched.</param>
/// <param name="Meta">Paging and provenance for this response.</param>
/// <param name="Errors">
/// Upstream source failures that did not prevent the request from succeeding.
///
/// This is always empty in the current design: every upstream failure prevents the endpoint
/// from serving its data at all, so it is reported as an RFC 9457 <c>ProblemDetails</c> response
/// (502, or 503 when every source this endpoint needs is unavailable) instead of a partial 200.
/// That <c>ProblemDetails</c>' <c>detail</c> already names the failing source, its
/// <see cref="UpstreamFailureKind"/>, the HTTP status and the reason, so there is nothing left
/// for this field to carry today.
///
/// It is kept, not deleted, because it is the designated slot for partial-failure reporting the
/// day a partial-failure mode exists (a third upstream source, per-resource fan-out, or serving
/// stale-with-warning). Consumers should treat an empty array as "no partial failures occurred",
/// not as "this will never be populated" — do not code against it always being empty.
///
/// Contrast with <see cref="Skipped"/>, which IS populated today and carries records that could
/// not be assembled even though the request otherwise succeeded.
/// </param>
/// <param name="Skipped">Records that could not be assembled, and why. Populated today.</param>
public sealed record ApiResponse<T>(
    T Data,
    ResponseMeta Meta,
    IReadOnlyList<UpstreamFailure> Errors,
    IReadOnlyList<SkippedRecord> Skipped);
