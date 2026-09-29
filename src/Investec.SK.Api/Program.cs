using Investec.SK.Api.Endpoints;
using Investec.SK.Api.OpenApi;
using Investec.SK.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<ExamplePayloadsTransformer>();
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
app.MapVillainEndpoints();
app.MapHealthEndpoints();

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can boot the real pipeline in tests.</summary>
public partial class Program;
