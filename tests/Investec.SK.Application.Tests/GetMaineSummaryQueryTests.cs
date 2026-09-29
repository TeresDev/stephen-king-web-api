using Investec.SK.Application.Queries;
using Investec.SK.Application.Summary;

namespace Investec.SK.Application.Tests;

public class GetMaineSummaryQueryTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 29, 11, 16, 0, TimeSpan.Zero);

    private static GetMaineSummaryQuery Handler(
        FakeVillainRepository villains, FakeBookRepository books) =>
        new(villains, books, new FakeTimeProvider(At));

    private static (FakeVillainRepository Villains, FakeBookRepository Books) Cast()
    {
        var books = FakeBookRepository.Returning(
            TestData.Book(1, "It", 1986, 1138),
            TestData.Book(2, "Misery", 1986, 310),
            TestData.Book(3, "The Stand", 1978, 823));

        var villains = FakeVillainRepository.Returning(
            TestData.MaineVillain(10, "Pennywise", TestData.Link(1, "It")),
            TestData.MaineVillain(11, "Annie Wilkes", TestData.Link(2, "Misery")),
            TestData.MaineVillain(12, "Randall Flagg", TestData.Link(3, "The Stand")));

        return (villains, books);
    }

    [Fact]
    public async Task ExecuteAsync_CountsRowsPerYearDescending()
    {
        var (villains, books) = Cast();

        var result = await Handler(villains, books).ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(QueryStatus.Success, result.Status);
        Assert.Equal([1986, 1978], result.Response!.Data.CountsByYear.Select(c => c.Year));
        Assert.Equal(2, result.Response.Data.CountsByYear[0].Count);
        Assert.Equal(1, result.Response.Data.CountsByYear[1].Count);
        Assert.Equal(3, result.Response.Data.TotalRows);
    }

    [Fact]
    public async Task ExecuteAsync_TopByPages_IsOrderedDescendingAndDefaultsToFive()
    {
        var (villains, books) = Cast();

        var result = await Handler(villains, books).ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(["It", "The Stand", "Misery"],
                     result.Response!.Data.TopByPages.Select(r => r.BookName));
    }

    [Fact]
    public async Task ExecuteAsync_TopParameter_LimitsTheList()
    {
        var (villains, books) = Cast();

        var result = await Handler(villains, books).ExecuteAsync(2, CancellationToken.None);

        Assert.Equal(2, result.Response!.Data.TopByPages.Count);
        Assert.Equal("It", result.Response.Data.TopByPages[0].BookName);
    }

    [Fact]
    public async Task ExecuteAsync_BookWithSeveralVillains_AppearsOnceWithAVillainCount()
    {
        var books = FakeBookRepository.Returning(
            TestData.Book(1, "It", 1986, 1138),
            TestData.Book(2, "Misery", 1987, 310));

        var villains = FakeVillainRepository.Returning(
            TestData.MaineVillain(10, "Pennywise", TestData.Link(1, "It")),
            TestData.MaineVillain(11, "Henry Bowers", TestData.Link(1, "It")),
            TestData.MaineVillain(12, "Patrick Hockstetter", TestData.Link(1, "It")),
            TestData.MaineVillain(13, "Annie Wilkes", TestData.Link(2, "Misery")));

        var result = await Handler(villains, books).ExecuteAsync(null, CancellationToken.None);

        var topByPages = result.Response!.Data.TopByPages;

        Assert.Equal(2, topByPages.Count);
        Assert.Single(topByPages, b => b.BookName == "It");
        var it = topByPages.Single(b => b.BookName == "It");
        Assert.Equal(3, it.VillainCount);
        Assert.Equal(1138, it.Pages);

        // Ordering is still pages-descending across distinct books.
        Assert.Equal(["It", "Misery"], topByPages.Select(b => b.BookName));

        // The top limit counts BOOKS, not rows: 4 rows collapse to 2 distinct books.
        Assert.Equal(4, result.Response.Data.TotalRows);
    }

    [Fact]
    public async Task ExecuteAsync_TopParameter_LimitsBooksNotRows()
    {
        var books = FakeBookRepository.Returning(
            TestData.Book(1, "It", 1986, 1138),
            TestData.Book(2, "Misery", 1987, 310));

        var villains = FakeVillainRepository.Returning(
            TestData.MaineVillain(10, "Pennywise", TestData.Link(1, "It")),
            TestData.MaineVillain(11, "Henry Bowers", TestData.Link(1, "It")),
            TestData.MaineVillain(12, "Patrick Hockstetter", TestData.Link(1, "It")),
            TestData.MaineVillain(13, "Annie Wilkes", TestData.Link(2, "Misery")));

        var result = await Handler(villains, books).ExecuteAsync(1, CancellationToken.None);

        Assert.Single(result.Response!.Data.TopByPages);
        Assert.Equal("It", result.Response.Data.TopByPages[0].BookName);
        Assert.Equal(3, result.Response.Data.TopByPages[0].VillainCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    public async Task ExecuteAsync_TopOutOfRange_IsInvalidRequest(int top)
    {
        var (villains, books) = Cast();

        var result = await Handler(villains, books).ExecuteAsync(top, CancellationToken.None);

        Assert.Equal(QueryStatus.InvalidRequest, result.Status);
        Assert.Contains(result.ValidationErrors, e => e.Parameter == "top");
    }

    [Fact]
    public async Task ExecuteAsync_BooksSourceDown_IsUpstreamUnavailable()
    {
        var result = await Handler(FakeVillainRepository.Returning(), FakeBookRepository.Failing())
            .ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(QueryStatus.UpstreamUnavailable, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_BothSourcesDown_IsAllUpstreamsUnavailable()
    {
        var result = await Handler(FakeVillainRepository.Failing(), FakeBookRepository.Failing())
            .ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(QueryStatus.AllUpstreamsUnavailable, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SkippedVillains_AreStillReported()
    {
        var books = FakeBookRepository.Returning(TestData.Book(1, "It", 1986, 1138));
        var villains = FakeVillainRepository.Returning(
            TestData.MaineVillain(10, "Pennywise", TestData.Link(1, "It")),
            TestData.MaineVillain(11, "Bob Anderson"));

        var result = await Handler(villains, books).ExecuteAsync(null, CancellationToken.None);

        Assert.Single(result.Response!.Skipped);
        Assert.True(result.Response.Meta.Degraded);
    }
}
