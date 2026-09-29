using Investec.SK.Application.Queries;

namespace Investec.SK.Api.Http;

/// <summary>
/// The single place a query outcome becomes an HTTP status. Keeping it here is what allows the
/// Application layer to stay free of ASP.NET types.
/// </summary>
public static class ProblemResults
{
    private const string RetryAfterSeconds = "30";

    public static IResult From<T>(QueryResult<T> result) => result.Status switch
    {
        // Response is non-null whenever Status is Success; the `!` keeps
        // TreatWarningsAsErrors from failing the build on the nullable annotation.
        QueryStatus.Success =>
            TypedResults.Ok(result.Response!),

        QueryStatus.InvalidRequest =>
            TypedResults.ValidationProblem(
                result.ValidationErrors
                      .GroupBy(e => e.Parameter)
                      .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()),
                title: "One or more query parameters are invalid."),

        QueryStatus.UpstreamUnavailable =>
            TypedResults.Problem(
                title: "An upstream source required by this endpoint is unavailable.",
                detail: Describe(result),
                statusCode: StatusCodes.Status502BadGateway),

        QueryStatus.AllUpstreamsUnavailable =>
            new RetryAfterProblem(Describe(result)),

        _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError),
    };

    private static string Describe<T>(QueryResult<T> result) =>
        string.Join("; ", result.Failures.Select(f =>
            $"{f.Source} ({f.Kind}{(f.Status is null ? "" : $", HTTP {f.Status}")}): {f.Reason}"));

    /// <summary>503 with a Retry-After header, which TypedResults.Problem cannot set on its own.</summary>
    private sealed class RetryAfterProblem(string detail) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.RetryAfter = RetryAfterSeconds;

            await TypedResults.Problem(
                title: "All upstream sources are unavailable.",
                detail: detail,
                statusCode: StatusCodes.Status503ServiceUnavailable)
                .ExecuteAsync(httpContext);
        }
    }
}
