using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Investec.SK.Api.OpenApi;

/// <summary>
/// Adds a realistic example payload to the 200 response of the two main endpoints. The brief's
/// stretch goal asks for OpenAPI "with example payloads"; without this the generated document
/// describes shapes but shows nobody what a real response looks like. Values are drawn from the
/// live data captured in the design spec (year 1986, "It", Pennywise), and the Maine example
/// includes a populated <c>skipped[]</c> entry so the diagnostics channel is visible to a reader
/// who only ever looks at the schema.
/// </summary>
public sealed class ExamplePayloadsTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var example = context.Description.RelativePath switch
        {
            "villains/maine" => MaineExample(),
            "villains/female-derry" => FemaleDerryExample(),
            _ => null,
        };

        if (example is not null)
        {
            ApplyToOkResponse(operation, example);
        }

        return Task.CompletedTask;
    }

    private static void ApplyToOkResponse(OpenApiOperation operation, JsonNode example)
    {
        if (operation.Responses is null || !operation.Responses.TryGetValue("200", out var response)
            || response?.Content is null)
        {
            return;
        }

        foreach (var mediaType in response.Content.Values)
        {
            mediaType.Example = example.DeepClone();
        }
    }

    private static JsonNode MaineExample() => new JsonObject
    {
        ["data"] = new JsonArray
        {
            new JsonObject
            {
                ["year"] = 1986,
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["bookName"] = "It",
                        ["isbn"] = "978-0-670-81302-5",
                        ["pages"] = 1138,
                        ["villainName"] = "Pennywise",
                    },
                },
            },
        },
        ["meta"] = CommonMeta("pages", "desc", totalItems: 36, itemsOnPage: 20, degraded: true),
        ["errors"] = new JsonArray(),
        ["skipped"] = new JsonArray
        {
            new JsonObject
            {
                ["kind"] = "Villain",
                ["identifier"] = "Bob Anderson",
                ["reason"] = "NoLinkedBooks",
                ["detail"] = "Villain is a Maine resident but appears in no book",
            },
        },
    };

    private static JsonNode FemaleDerryExample() => new JsonObject
    {
        ["data"] = new JsonArray
        {
            new JsonObject
            {
                ["villainName"] = "Greta Bowie",
                ["gender"] = "Female",
                ["status"] = "Deceased",
                ["residenceNote"] = "Derry residents",
                ["books"] = new JsonArray { "It" },
            },
        },
        ["meta"] = CommonMeta("villainName", "asc", totalItems: 1, itemsOnPage: 1, degraded: false),
        ["errors"] = new JsonArray(),
        ["skipped"] = new JsonArray(),
    };

    private static JsonObject CommonMeta(string sort, string direction, int totalItems, int itemsOnPage, bool degraded) =>
        new()
        {
            ["page"] = 1,
            ["pageSize"] = 20,
            ["totalItems"] = totalItems,
            ["totalPages"] = (int)Math.Ceiling(totalItems / 20.0),
            ["itemsOnPage"] = itemsOnPage,
            ["sort"] = sort,
            ["direction"] = direction,
            ["degraded"] = degraded,
            ["retrievedAt"] = "2026-09-29T11:16:00Z",
        };
}
