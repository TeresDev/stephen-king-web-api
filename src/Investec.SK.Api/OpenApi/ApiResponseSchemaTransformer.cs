using Investec.SK.Application.Responses;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Investec.SK.Api.OpenApi;

/// <summary>
/// Documents <see cref="ApiResponse{T}.Errors"/> in the generated OpenAPI schema. The XML doc
/// comment on the record explains why the field is always empty today (see there for the full
/// reasoning); this transformer copies the short version onto the <c>errors</c> property so a
/// reader who only ever looks at <c>/openapi/v1.json</c>, or a client generated from it, sees the
/// same explanation without opening the source.
/// </summary>
public sealed class ApiResponseSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string ErrorsDescription =
        "Upstream source failures that did not prevent the request from succeeding. Always " +
        "empty in the current design: any upstream failure prevents the endpoint from serving " +
        "its data at all, so it is reported as an RFC 9457 ProblemDetails response (502, or 503 " +
        "when every source this endpoint needs is unavailable) instead of a partial 200. That " +
        "ProblemDetails names the failing source, its kind, the HTTP status and the reason. This " +
        "field is reserved for a future partial-failure mode; treat an empty array as \"no " +
        "partial failures\", not as \"this will never be populated\". Contrast with skipped, " +
        "which is populated today.";

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(ApiResponse<>)
            && schema.Properties is not null
            && schema.Properties.TryGetValue("errors", out var errorsSchema)
            && errorsSchema is not null)
        {
            errorsSchema.Description = ErrorsDescription;
        }

        return Task.CompletedTask;
    }
}
