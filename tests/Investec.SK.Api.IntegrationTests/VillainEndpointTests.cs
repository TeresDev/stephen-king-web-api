using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Investec.SK.Api.IntegrationTests;

public class MaineEndpointTests
{
    private static async Task<JsonElement> GetJson(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Maine_HealthyUpstream_ReturnsTheDocumentedEnvelopeShape()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await GetJson(factory.CreateClient(), "/villains/maine");

        Assert.True(body.TryGetProperty("data", out _));
        Assert.True(body.TryGetProperty("meta", out _));
        Assert.True(body.TryGetProperty("errors", out _));
        Assert.True(body.TryGetProperty("skipped", out _));
    }

    [Fact]
    public async Task Maine_GroupsByYearAndOrdersByPagesDescendingByDefault()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await GetJson(factory.CreateClient(), "/villains/maine");

        var years = body.GetProperty("data").EnumerateArray()
                        .Select(g => g.GetProperty("year").GetInt32()).ToArray();

        Assert.Equal([1986, 1978, 1974], years);

        var first = body.GetProperty("data")[0].GetProperty("items")[0];
        Assert.Equal("It", first.GetProperty("bookName").GetString());
        Assert.Equal(1138, first.GetProperty("pages").GetInt32());
        Assert.Equal("Pennywise", first.GetProperty("villainName").GetString());
        Assert.Equal("978-0-670-81302-5", first.GetProperty("isbn").GetString());
    }

    [Fact]
    public async Task Maine_ReportsTheVillainWithNoBooksInSkippedAndMarksDegraded()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await GetJson(factory.CreateClient(), "/villains/maine");

        var skipped = body.GetProperty("skipped").EnumerateArray().ToArray();
        Assert.Single(skipped);
        Assert.Equal("Bob Anderson", skipped[0].GetProperty("identifier").GetString());
        Assert.True(body.GetProperty("meta").GetProperty("degraded").GetBoolean());
        Assert.Empty(body.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task Maine_MetaCountsRowsNotYearGroups()
    {
        using var factory = TestApiFactory.Healthy();
        var meta = (await GetJson(factory.CreateClient(), "/villains/maine")).GetProperty("meta");

        Assert.Equal(3, meta.GetProperty("totalItems").GetInt32());
        Assert.Equal(20, meta.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, meta.GetProperty("totalPages").GetInt32());
        Assert.Equal("pages", meta.GetProperty("sort").GetString());
        Assert.Equal("desc", meta.GetProperty("direction").GetString());
    }

    [Fact]
    public async Task Maine_PageSizeOfOne_SplitsRowsAcrossPages()
    {
        using var factory = TestApiFactory.Healthy();
        var client = factory.CreateClient();

        var first = await GetJson(client, "/villains/maine?page=1&pageSize=1");
        var second = await GetJson(client, "/villains/maine?page=2&pageSize=1");

        Assert.Equal(3, first.GetProperty("meta").GetProperty("totalPages").GetInt32());
        Assert.Equal("It", first.GetProperty("data")[0].GetProperty("items")[0].GetProperty("bookName").GetString());
        Assert.Equal("The Stand", second.GetProperty("data")[0].GetProperty("items")[0].GetProperty("bookName").GetString());
    }

    [Fact]
    public async Task Maine_YearFilter_NarrowsToThatYear()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await GetJson(factory.CreateClient(), "/villains/maine?year=1986");

        var group = Assert.Single(body.GetProperty("data").EnumerateArray());
        Assert.Equal(1986, group.GetProperty("year").GetInt32());
    }

    [Fact]
    public async Task Maine_PageBeyondLast_Returns200WithEmptyData()
    {
        using var factory = TestApiFactory.Healthy();
        var response = await factory.CreateClient().GetAsync("/villains/maine?page=999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(body.GetProperty("data").EnumerateArray());
        Assert.Equal(3, body.GetProperty("meta").GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task Maine_HugePageOverflowsTheSkipCalculation_Returns200WithEmptyDataNotPageOne()
    {
        using var factory = TestApiFactory.Healthy();
        var response = await factory.CreateClient().GetAsync("/villains/maine?page=30000000&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(body.GetProperty("data").EnumerateArray());
        Assert.Equal(30000000, body.GetProperty("meta").GetProperty("page").GetInt32());
        Assert.Equal(3, body.GetProperty("meta").GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("/villains/maine?page=0")]
    [InlineData("/villains/maine?page=-1")]
    [InlineData("/villains/maine?pageSize=0")]
    [InlineData("/villains/maine?pageSize=101")]
    [InlineData("/villains/maine?sort=publisher")]
    [InlineData("/villains/maine?dir=sideways")]
    [InlineData("/villains/maine?year=1500")]
    public async Task Maine_InvalidQuery_Returns400ProblemDetails(string url)
    {
        using var factory = TestApiFactory.Healthy();
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Maine_SkippedReason_SerialisesAsTheEnumName()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await GetJson(factory.CreateClient(), "/villains/maine");

        var skipped = Assert.Single(body.GetProperty("skipped").EnumerateArray());
        Assert.Equal("NoLinkedBooks", skipped.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Maine_BooksSourceDown_Returns502()
    {
        using var factory = TestApiFactory.BooksDown();
        var response = await factory.CreateClient().GetAsync("/villains/maine");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(502, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Maine_BothSourcesDown_Returns503WithRetryAfter()
    {
        using var factory = TestApiFactory.BothDown();
        var response = await factory.CreateClient().GetAsync("/villains/maine");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
    }
}

public class FemaleDerryEndpointTests
{
    [Fact]
    public async Task FemaleDerry_ReturnsOnlyFemaleDerryResidents()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/villains/female-derry");

        var names = body.GetProperty("data").EnumerateArray()
                        .Select(v => v.GetProperty("villainName").GetString()!).ToArray();

        Assert.Equal(["Greta Bowie", "Marcia Fadden"], names);
    }

    [Fact]
    public async Task FemaleDerry_EchoesTheMatchedSpellingIncludingTheToleratedOne()
    {
        using var factory = TestApiFactory.Healthy();
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/villains/female-derry");

        var marcia = body.GetProperty("data").EnumerateArray()
                         .Single(v => v.GetProperty("villainName").GetString() == "Marcia Fadden");

        Assert.Equal("Dery residents", marcia.GetProperty("residenceNote").GetString());
    }

    [Fact]
    public async Task FemaleDerry_BooksSourceDown_StillReturnsAFull200()
    {
        // The single most important behaviour of the failure contract: this endpoint does not
        // need the books source, so a books outage must not degrade it at all.
        using var factory = TestApiFactory.BooksDown();
        var response = await factory.CreateClient().GetAsync("/villains/female-derry");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("data").GetArrayLength());
        Assert.Empty(body.GetProperty("errors").EnumerateArray());
        Assert.False(body.GetProperty("meta").GetProperty("degraded").GetBoolean());
    }

    [Fact]
    public async Task FemaleDerry_ListsBookTitlesFromTheVillainFeed()
    {
        using var factory = TestApiFactory.BooksDown();
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/villains/female-derry");

        var greta = body.GetProperty("data").EnumerateArray()
                        .Single(v => v.GetProperty("villainName").GetString() == "Greta Bowie");

        Assert.Equal("It", greta.GetProperty("books")[0].GetString());
    }

    [Fact]
    public async Task FemaleDerry_VillainsSourceDown_Returns502()
    {
        using var factory = TestApiFactory.VillainsDown();
        var response = await factory.CreateClient().GetAsync("/villains/female-derry");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task FemaleDerry_BookNameSortKey_Returns400()
    {
        using var factory = TestApiFactory.Healthy();
        var response = await factory.CreateClient().GetAsync("/villains/female-derry?sort=bookName");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class OpenApiTests
{
    [Fact]
    public async Task OpenApiDocument_IsGeneratedAndDescribesBothEndpoints()
    {
        using var factory = TestApiFactory.Healthy();
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var paths = document.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/villains/maine", out _));
        Assert.True(paths.TryGetProperty("/villains/female-derry", out _));
    }
}
