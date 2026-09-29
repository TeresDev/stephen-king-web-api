using Investec.SK.Application.Maine;
using Investec.SK.Application.Queries;
using Investec.SK.Application.Responses;
using Investec.SK.Domain;

namespace Investec.SK.Application.Tests;

public class GetMaineVillainsQueryTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 29, 11, 16, 0, TimeSpan.Zero);

    private static GetMaineVillainsQuery Handler(
        FakeVillainRepository villains,
        FakeBookRepository books) =>
        new(villains, books, new FakeTimeProvider(At));

    private static (FakeVillainRepository Villains, FakeBookRepository Books) ThreeMaineRows()
    {
        var books = FakeBookRepository.Returning(
            TestData.Book(1, "It", 1986, 1138, "978-0-670-81302-5"),
            TestData.Book(2, "The Stand", 1978, 823),
            TestData.Book(3, "Carrie", 1974, 199));

        var villains = FakeVillainRepository.Returning(
            TestData.MaineVillain(10, "Pennywise", TestData.Link(1, "It")),
            TestData.MaineVillain(11, "Randall Flagg", TestData.Link(2, "The Stand")),
            TestData.MaineVillain(12, "Margaret White", TestData.Link(3, "Carrie")));

        return (villains, books);
    }

    [Fact]
    public async Task ExecuteAsync_BothSourcesHealthy_ReturnsSuccessGroupedByYearOrderedByPagesDesc()
    {
        var (villains, books) = ThreeMaineRows();

        var result = await Handler(villains, books)
            .ExecuteAsync(null, null, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.Success, result.Status);
        Assert.NotNull(result.Response);
        Assert.Equal([1986, 1978, 1974], result.Response.Data.Select(g => g.Year));
        Assert.Equal("It", result.Response.Data[0].Items[0].BookName);
        Assert.Equal(1138, result.Response.Data[0].Items[0].Pages);
    }

    [Fact]
    public async Task ExecuteAsync_Success_ReportsRowCountsNotGroupCountsInMeta()
    {
        var (villains, books) = ThreeMaineRows();

        var result = await Handler(villains, books)
            .ExecuteAsync(null, null, null, null, null, CancellationToken.None);

        Assert.Equal(3, result.Response!.Meta.TotalItems);
        Assert.Equal(3, result.Response.Meta.ItemsOnPage);
        Assert.Equal(1, result.Response.Meta.TotalPages);
        Assert.Equal("pages", result.Response.Meta.Sort);
        Assert.Equal("desc", result.Response.Meta.Direction);
        Assert.Equal(At, result.Response.Meta.RetrievedAt);
        Assert.False(result.Response.Meta.Degraded);
    }

    [Fact]
    public async Task ExecuteAsync_BooksSourceDown_IsUpstreamUnavailable()
    {
        var result = await Handler(FakeVillainRepository.Returning(), FakeBookRepository.Failing())
            .ExecuteAsync(null, null, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.UpstreamUnavailable, result.Status);
        Assert.Null(result.Response);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("books", failure.Source);
    }

    [Fact]
    public async Task ExecuteAsync_VillainsSourceDown_IsUpstreamUnavailable()
    {
        var result = await Handler(FakeVillainRepository.Failing(), FakeBookRepository.Returning())
            .ExecuteAsync(null, null, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.UpstreamUnavailable, result.Status);
        Assert.Equal("villains", Assert.Single(result.Failures).Source);
    }

    [Fact]
    public async Task ExecuteAsync_BothSourcesDown_IsAllUpstreamsUnavailableAndListsBoth()
    {
        var result = await Handler(FakeVillainRepository.Failing(), FakeBookRepository.Failing())
            .ExecuteAsync(null, null, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.AllUpstreamsUnavailable, result.Status);
        Assert.Equal(2, result.Failures.Count);
        Assert.Contains(result.Failures, f => f.Source == "books");
        Assert.Contains(result.Failures, f => f.Source == "villains");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidPaging_IsInvalidRequestAndNeverTouchesUpstream()
    {
        var villains = FakeVillainRepository.Returning();
        var books = FakeBookRepository.Returning();

        var result = await Handler(villains, books)
            .ExecuteAsync(-1, 500, "publisher", null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.InvalidRequest, result.Status);
        Assert.Equal(3, result.ValidationErrors.Count);
        Assert.Equal(0, villains.CallCount);
        Assert.Equal(0, books.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_MaineVillainWithNoBooks_IsReportedAsSkippedAndMarksDegraded()
    {
        var books = FakeBookRepository.Returning(TestData.Book(1, "It", 1986, 1138));
        var villains = FakeVillainRepository.Returning(
            TestData.MaineVillain(10, "Pennywise", TestData.Link(1, "It")),
            TestData.MaineVillain(11, "Bob Anderson"));

        var result = await Handler(villains, books)
            .ExecuteAsync(null, null, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.Success, result.Status);
        Assert.True(result.Response!.Meta.Degraded);
        var skipped = Assert.Single(result.Response.Skipped);
        Assert.Equal("Bob Anderson", skipped.Identifier);
        Assert.Equal(SkipReason.NoLinkedBooks, skipped.Reason);
        Assert.Empty(result.Response.Errors);
    }

    [Fact]
    public async Task ExecuteAsync_YearFilter_KeepsOnlyThatYear()
    {
        var (villains, books) = ThreeMaineRows();

        var result = await Handler(villains, books)
            .ExecuteAsync(null, null, null, null, 1986, CancellationToken.None);

        var group = Assert.Single(result.Response!.Data);
        Assert.Equal(1986, group.Year);
        Assert.Equal(1, result.Response.Meta.TotalItems);
    }

    [Fact]
    public async Task ExecuteAsync_YearFilterMatchingNothing_SucceedsWithEmptyData()
    {
        var (villains, books) = ThreeMaineRows();

        var result = await Handler(villains, books)
            .ExecuteAsync(null, null, null, null, 1999, CancellationToken.None);

        Assert.Equal(QueryStatus.Success, result.Status);
        Assert.Empty(result.Response!.Data);
        Assert.Equal(0, result.Response.Meta.TotalItems);
        Assert.Equal(0, result.Response.Meta.TotalPages);
    }

    [Fact]
    public async Task ExecuteAsync_SortByBookName_OrdersAlphabeticallyAscendingByDefault()
    {
        var (villains, books) = ThreeMaineRows();

        var result = await Handler(villains, books)
            .ExecuteAsync(null, null, "bookName", null, null, CancellationToken.None);

        var names = result.Response!.Data.SelectMany(g => g.Items).Select(i => i.BookName);
        Assert.Equal(["Carrie", "It", "The Stand"], names);
        Assert.Equal("asc", result.Response.Meta.Direction);
    }

    [Fact]
    public async Task ExecuteAsync_SecondPage_ReturnsTheRemainingRows()
    {
        var (villains, books) = ThreeMaineRows();

        var result = await Handler(villains, books)
            .ExecuteAsync(2, 2, null, null, null, CancellationToken.None);

        Assert.Equal(3, result.Response!.Meta.TotalItems);
        Assert.Equal(2, result.Response.Meta.TotalPages);
        Assert.Equal(1, result.Response.Meta.ItemsOnPage);
        Assert.Equal("Carrie", result.Response.Data[0].Items[0].BookName);
    }

    [Fact]
    public async Task ExecuteAsync_PageBeyondLast_SucceedsWithEmptyDataAndHonestMeta()
    {
        var (villains, books) = ThreeMaineRows();

        var result = await Handler(villains, books)
            .ExecuteAsync(999, 20, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.Success, result.Status);
        Assert.Empty(result.Response!.Data);
        Assert.Equal(999, result.Response.Meta.Page);
        Assert.Equal(3, result.Response.Meta.TotalItems);
        Assert.Equal(0, result.Response.Meta.ItemsOnPage);
    }
}
