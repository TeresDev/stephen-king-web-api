using System.Net;
using Investec.SK.Application.Abstractions;
using Investec.SK.Application.Responses;
using Investec.SK.Infrastructure.Http;
using Investec.SK.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Investec.SK.Infrastructure.Tests;

public class BookRepositoryTests
{
    private static (BookRepository Repository, StubHttpMessageHandler Handler) Build(
        StubHttpMessageHandler handler,
        TimeSpan? ttl = null)
    {
        var client = new SkApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://stephen-king-api.onrender.com/api/") },
            NullLogger<SkApiClient>.Instance);

        var options = Options.Create(new SkApiOptions
        {
            CacheTtl = ttl ?? TimeSpan.FromMinutes(5),
        });

        var cache = new MemoryCache(new MemoryCacheOptions());

        return (new BookRepository(client, cache, options), handler);
    }

    [Fact]
    public async Task GetAllAsync_HealthyUpstream_ReturnsMappedDomainBooks()
    {
        var (repository, _) = Build(StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, Fixtures.Books));

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(64, result.Value!.Items.Count);
        Assert.Contains(result.Value.Items, b => b.Title.Value == "Carrie");
    }

    [Fact]
    public async Task GetAllAsync_BookWithBlankIsbn_ReportsInvalidIsbnInsteadOfSilentlyResurfacingAsUnresolvedBookReference()
    {
        const string payload = """
            { "data": [
                { "id": 1, "Year": 1986, "Title": "It", "handle": "it",
                  "Publisher": "Viking", "ISBN": "", "Pages": 1138,
                  "Notes": [""], "created_at": "2021-01-01T00:00:00.000Z", "villains": [] }
            ] }
            """;

        var (repository, _) = Build(StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, payload));

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        var skipped = Assert.Single(result.Value.Skipped);
        Assert.Equal(SkipReason.InvalidIsbn, skipped.Reason);
        Assert.Equal("It", skipped.Identifier);
    }

    [Fact]
    public async Task GetAllAsync_CalledTwiceWithinTtl_HitsUpstreamOnlyOnce()
    {
        var (repository, handler) = Build(StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, Fixtures.Books));

        await repository.GetAllAsync(CancellationToken.None);
        await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetAllAsync_UpstreamDown_ReturnsFailure()
    {
        var (repository, _) = Build(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.ServiceUnavailable, "down"));

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpstreamFailureKind.ServerError, result.Failure!.Kind);
        Assert.Equal("books", result.Failure.Source);
    }

    [Fact]
    public async Task GetAllAsync_FailureIsNeverCached_SoARecoveryIsSeenImmediately()
    {
        var responses = new Queue<HttpResponseMessage>(
        [
            new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("down") },
            new(HttpStatusCode.OK) { Content = new StringContent(Fixtures.Books) },
        ]);

        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(responses.Dequeue()));
        var (repository, _) = Build(handler);

        var first = await repository.GetAllAsync(CancellationToken.None);
        var second = await repository.GetAllAsync(CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetAllAsync_MalformedPayload_ReportsBadPayloadAndCachesNothing()
    {
        var handler = StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, "{ broken");
        var (repository, _) = Build(handler);

        var first = await repository.GetAllAsync(CancellationToken.None);
        await repository.GetAllAsync(CancellationToken.None);

        Assert.Equal(UpstreamFailureKind.BadPayload, first.Failure!.Kind);
        Assert.Equal(2, handler.CallCount);
    }
}

public class VillainRepositoryTests
{
    private static VillainRepository Build(StubHttpMessageHandler handler)
    {
        var client = new SkApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://stephen-king-api.onrender.com/api/") },
            NullLogger<SkApiClient>.Instance);

        return new VillainRepository(
            client,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new SkApiOptions()));
    }

    [Fact]
    public async Task GetAllAsync_HealthyUpstream_ReturnsMappedDomainVillains()
    {
        var result = await Build(StubHttpMessageHandler.RespondingWith(HttpStatusCode.OK, Fixtures.Villains))
            .GetAllAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(115, result.Value!.Items.Count);
        Assert.Equal(38, result.Value.Items.Count(v => v.Notes.IsResidentOfMaine));
    }

    [Fact]
    public async Task GetAllAsync_UpstreamDown_ReportsTheVillainsSource()
    {
        var result = await Build(StubHttpMessageHandler.Throwing(new HttpRequestException("unreachable")))
            .GetAllAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("villains", result.Failure!.Source);
        Assert.Equal(UpstreamFailureKind.Transport, result.Failure.Kind);
    }
}
