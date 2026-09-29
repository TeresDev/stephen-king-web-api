using Investec.SK.Api.Endpoints;
using Investec.SK.Api.OpenApi;
using Investec.SK.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<ExamplePayloadsTransformer>();
    options.AddSchemaTransformer<ApiResponseSchemaTransformer>();
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Stephen King Web API"));
app.MapVillainEndpoints();
app.MapHealthEndpoints();

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can boot the real pipeline in tests.</summary>
public partial class Program;
