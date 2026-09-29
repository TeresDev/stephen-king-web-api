using Investec.SK.Api.Endpoints;
using Investec.SK.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapVillainEndpoints();
app.MapHealthEndpoints();

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can boot the real pipeline in tests.</summary>
public partial class Program;
