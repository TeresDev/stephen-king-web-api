using Investec.SK.Api.Http;
using Investec.SK.Application.Derry;
using Investec.SK.Application.Maine;
using Investec.SK.Application.Responses;

namespace Investec.SK.Api.Endpoints;

public static class VillainEndpoints
{
    public static RouteGroupBuilder MapVillainEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/villains").WithTags("Villains");

        group.MapGet("/maine", async (
                int? page,
                int? pageSize,
                string? sort,
                string? dir,
                int? year,
                GetMaineVillainsQuery query,
                CancellationToken cancellationToken) =>
                ProblemResults.From(
                    await query.ExecuteAsync(page, pageSize, sort, dir, year, cancellationToken)))
            .WithName("GetMaineVillains")
            .WithSummary("Villains resident in Maine, grouped by publish year.")
            .WithDescription(
                "Rows are the unit of sorting and paging; year grouping is applied to each page "
                + "for presentation. Requires both upstream sources.")
            .Produces<ApiResponse<IReadOnlyList<MaineYearGroup>>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/female-derry", async (
                int? page,
                int? pageSize,
                string? sort,
                string? dir,
                GetFemaleDerryQuery query,
                CancellationToken cancellationToken) =>
                ProblemResults.From(
                    await query.ExecuteAsync(page, pageSize, sort, dir, cancellationToken)))
            .WithName("GetFemaleDerryVillains")
            .WithSummary("Female villains resident in Derry, tolerating the 'Dery' spelling.")
            .WithDescription(
                "Requires the villains source only, so it answers in full even while the books "
                + "source is unavailable.")
            .Produces<ApiResponse<IReadOnlyList<DerryVillain>>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return group;
    }
}
