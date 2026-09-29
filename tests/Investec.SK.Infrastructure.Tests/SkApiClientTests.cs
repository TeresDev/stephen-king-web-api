using System.Net;
using Investec.SK.Application.Abstractions;
using Investec.SK.Infrastructure.Contracts;
using Investec.SK.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Investec.SK.Infrastructure.Tests;

public class SkApiClientTests
{
    private static SkApiClient Client(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://stephen-king-api.onrender.com/api/"),
        },
        NullLogger<SkApiClient>.Instance);

    private static Task<UpstreamResult<IReadOnlyList<BookContract>>> GetBooks(
        StubHttpMessageHandler handler) =>
        Client(handler).GetListAsync<BookContract>(
            UpstreamSources.Books, "books", CancellationToken.None);

    [Fact]
    public async Task GetListAsync_SuccessfulResponse_ReturnsDeserialisedItems()
    {
        var result = await GetBooks(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.OK, Fixtures.Books));

        Assert.True(result.IsSuccess);
        Assert.Equal(64, result.Value!.Count);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task GetListAsync_ServerError_IsClassifiedAsServerErrorWithTheStatus()
    {
        var result = await GetBooks(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.InternalServerError, "boom"));

        Assert.False(result.IsSuccess);
        Assert.Equal(UpstreamFailureKind.ServerError, result.Failure!.Kind);
        Assert.Equal(500, result.Failure.Status);
        Assert.Equal("books", result.Failure.Source);
        Assert.Contains("books", result.Failure.Resource);
    }

    [Fact]
    public async Task GetListAsync_NotFound_IsClassifiedAsNotFound()
    {
        var result = await GetBooks(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.NotFound, "missing"));

        Assert.Equal(UpstreamFailureKind.NotFound, result.Failure!.Kind);
        Assert.Equal(404, result.Failure.Status);
    }

    [Fact]
    public async Task GetListAsync_MalformedJson_IsClassifiedAsBadPayload()
    {
        var result = await GetBooks(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.OK, "{ this is not json"));

        Assert.False(result.IsSuccess);
        Assert.Equal(UpstreamFailureKind.BadPayload, result.Failure!.Kind);
    }

    [Fact]
    public async Task GetListAsync_JsonWithoutADataProperty_IsClassifiedAsBadPayload()
    {
        var result = await GetBooks(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.OK, """{ "unexpected": true }"""));

        Assert.False(result.IsSuccess);
        Assert.Equal(UpstreamFailureKind.BadPayload, result.Failure!.Kind);
    }

    [Fact]
    public async Task GetListAsync_TransportFailure_IsClassifiedAsTransport()
    {
        var result = await GetBooks(StubHttpMessageHandler.Throwing(
            new HttpRequestException("Name or service not known")));

        Assert.False(result.IsSuccess);
        Assert.Equal(UpstreamFailureKind.Transport, result.Failure!.Kind);
        Assert.Null(result.Failure.Status);
    }

    [Fact]
    public async Task GetListAsync_Timeout_IsClassifiedAsTimeoutWithNoStatus()
    {
        var handler = StubHttpMessageHandler.NeverCompleting();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://stephen-king-api.onrender.com/api/"),
            Timeout = TimeSpan.FromMilliseconds(100),
        };

        var result = await new SkApiClient(httpClient, NullLogger<SkApiClient>.Instance)
            .GetListAsync<BookContract>(UpstreamSources.Books, "books", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpstreamFailureKind.Timeout, result.Failure!.Kind);
        Assert.Null(result.Failure.Status);
    }

    [Fact]
    public async Task GetListAsync_CallerCancellation_PropagatesRatherThanBecomingAFailure()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handler = StubHttpMessageHandler.NeverCompleting();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Client(handler).GetListAsync<BookContract>(UpstreamSources.Books, "books", cts.Token));
    }

    [Fact]
    public async Task GetListAsync_NeverThrowsForUpstreamProblems()
    {
        // The whole point of the boundary: no exception reaches Application.
        var exception = await Record.ExceptionAsync(() =>
            GetBooks(StubHttpMessageHandler.Throwing(new HttpRequestException("down"))));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetListAsync_EmptyDataArray_IsASuccessfulEmptyResult()
    {
        var result = await GetBooks(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.OK, """{ "data": [] }"""));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }
}
