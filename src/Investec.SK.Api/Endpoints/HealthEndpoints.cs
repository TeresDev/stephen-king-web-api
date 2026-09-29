using System.Diagnostics;
using Investec.SK.Application.Abstractions;

namespace Investec.SK.Api.Endpoints;

public sealed record UpstreamSourceHealth(
    string Name,
    bool Healthy,
    int? Status,
    string? Kind,
    long LatencyMs);

public sealed record UpstreamHealthReport(bool AllHealthy, IReadOnlyList<UpstreamSourceHealth> Sources);

/// <summary>
/// Answers "which upstream sources are broken" without provoking a failure on a data endpoint.
/// Always 200: the report itself succeeded, and the body carries the verdict.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/upstream", async (
                IBookRepository books,
                IVillainRepository villains,
                CancellationToken cancellationToken) =>
            {
                var bookProbe = Probe(UpstreamSources.Books,
                    () => books.GetAllAsync(cancellationToken), r => r.IsSuccess, r => r.Failure);

                var villainProbe = Probe(UpstreamSources.Villains,
                    () => villains.GetAllAsync(cancellationToken), r => r.IsSuccess, r => r.Failure);

                var results = await Task.WhenAll(bookProbe, villainProbe);

                return TypedResults.Ok(
                    new UpstreamHealthReport(results.All(r => r.Healthy), results));
            })
            .WithName("GetUpstreamHealth")
            .WithTags("Health")
            .WithSummary("Per-source reachability of the two upstream feeds.")
            .Produces<UpstreamHealthReport>();

        return app;
    }

    private static async Task<UpstreamSourceHealth> Probe<TResult>(
        string name,
        Func<Task<TResult>> call,
        Func<TResult, bool> isSuccess,
        Func<TResult, UpstreamFailure?> failureOf)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await call();
        stopwatch.Stop();

        if (isSuccess(result))
        {
            return new UpstreamSourceHealth(name, true, null, null, stopwatch.ElapsedMilliseconds);
        }

        var failure = failureOf(result);
        return new UpstreamSourceHealth(
            name, false, failure?.Status, failure?.Kind.ToString(), stopwatch.ElapsedMilliseconds);
    }
}
