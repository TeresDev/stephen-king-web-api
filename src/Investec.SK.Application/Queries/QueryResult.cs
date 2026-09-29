using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Responses;

namespace Investec.SK.Application.Queries;

/// <summary>
/// The outcome of a query, independent of HTTP. The Api layer is the only place that turns these
/// into status codes, so the Application layer never references ASP.NET.
/// </summary>
public enum QueryStatus
{
    Success,
    InvalidRequest,
    UpstreamUnavailable,
    AllUpstreamsUnavailable,
}

public sealed record QueryResult<T>(
    QueryStatus Status,
    ApiResponse<T>? Response,
    IReadOnlyList<ValidationError> ValidationErrors,
    IReadOnlyList<UpstreamFailure> Failures)
{
    public static QueryResult<T> Ok(ApiResponse<T> response) =>
        new(QueryStatus.Success, response, [], []);

    public static QueryResult<T> Invalid(IReadOnlyList<ValidationError> errors) =>
        new(QueryStatus.InvalidRequest, null, errors, []);

    public static QueryResult<T> Unavailable(IReadOnlyList<UpstreamFailure> failures) =>
        new(QueryStatus.UpstreamUnavailable, null, [], failures);

    public static QueryResult<T> AllUnavailable(IReadOnlyList<UpstreamFailure> failures) =>
        new(QueryStatus.AllUpstreamsUnavailable, null, [], failures);
}
