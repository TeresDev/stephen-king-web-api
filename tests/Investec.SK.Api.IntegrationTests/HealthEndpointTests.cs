using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Investec.SK.Api.IntegrationTests;

public class HealthEndpointTests
{
    [Fact]
    public async Task Health_AllSourcesUp_ReportsBothHealthy()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/health/upstream");

        Assert.True(body.GetProperty("allHealthy").GetBoolean());

        var sources = body.GetProperty("sources").EnumerateArray().ToArray();
        Assert.Equal(2, sources.Length);
        Assert.All(sources, s => Assert.True(s.GetProperty("healthy").GetBoolean()));
    }

    [Fact]
    public async Task Health_NamesBothSources()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/health/upstream");

        var names = body.GetProperty("sources").EnumerateArray()
                        .Select(s => s.GetProperty("name").GetString()).ToArray();

        Assert.Contains("books", names);
        Assert.Contains("villains", names);
    }

    [Fact]
    public async Task Health_BooksDown_Returns200ButReportsBooksUnhealthy()
    {
        using var factory = TestApiFactory.BooksDown();
        var response = await factory.CreateClient().GetAsync("/health/upstream");

        // The report succeeded; the body carries the bad news.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("allHealthy").GetBoolean());

        var booksSource = body.GetProperty("sources").EnumerateArray()
                              .Single(s => s.GetProperty("name").GetString() == "books");

        Assert.False(booksSource.GetProperty("healthy").GetBoolean());
        Assert.Equal(503, booksSource.GetProperty("status").GetInt32());
        Assert.Equal("ServerError", booksSource.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Health_BothDown_ReportsBothUnhealthy()
    {
        using var factory = TestApiFactory.BothDown();
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/health/upstream");

        Assert.False(body.GetProperty("allHealthy").GetBoolean());
        Assert.All(body.GetProperty("sources").EnumerateArray(),
                   s => Assert.False(s.GetProperty("healthy").GetBoolean()));
    }

    [Fact]
    public async Task Health_ReportsLatencyForEachSource()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/health/upstream");

        Assert.All(body.GetProperty("sources").EnumerateArray(),
                   s => Assert.True(s.GetProperty("latencyMs").GetInt64() >= 0));
    }
}
