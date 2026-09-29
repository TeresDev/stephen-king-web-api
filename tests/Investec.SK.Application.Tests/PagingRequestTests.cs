using Investec.SK.Application.Queries;

namespace Investec.SK.Application.Tests;

public class PagingRequestTests
{
    private static readonly SortKey[] MaineKeys =
        [SortKey.Pages, SortKey.BookName, SortKey.VillainName];

    private static readonly SortKey[] DerryKeys = [SortKey.VillainName];

    [Fact]
    public void TryCreate_AllDefaults_UsesPageOneSizeTwentyPagesDesc()
    {
        var ok = PagingRequest.TryCreate(null, null, null, null, null, MaineKeys,
                                         out var request, out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
        Assert.Equal(SortKey.Pages, request.Sort);
        Assert.Equal(SortDirection.Desc, request.Direction);
        Assert.Null(request.Year);
    }

    [Fact]
    public void TryCreate_NameSortWithoutDirection_DefaultsToAscending()
    {
        var ok = PagingRequest.TryCreate(null, null, "bookName", null, null, MaineKeys,
                                         out var request, out _);

        Assert.True(ok);
        Assert.Equal(SortKey.BookName, request.Sort);
        Assert.Equal(SortDirection.Asc, request.Direction);
    }

    [Theory]
    [InlineData("PAGES", SortKey.Pages)]
    [InlineData("pages", SortKey.Pages)]
    [InlineData("bookname", SortKey.BookName)]
    [InlineData("VillainName", SortKey.VillainName)]
    public void TryCreate_SortKeyIsCaseInsensitive(string raw, SortKey expected)
    {
        var ok = PagingRequest.TryCreate(null, null, raw, null, null, MaineKeys,
                                         out var request, out _);

        Assert.True(ok);
        Assert.Equal(expected, request.Sort);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryCreate_PageBelowOne_IsRejected(int page)
    {
        var ok = PagingRequest.TryCreate(page, null, null, null, null, MaineKeys, out _, out var errors);

        Assert.False(ok);
        Assert.Contains(errors, e => e.Parameter == "page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void TryCreate_PageSizeOutOfRange_IsRejected(int pageSize)
    {
        var ok = PagingRequest.TryCreate(null, pageSize, null, null, null, MaineKeys, out _, out var errors);

        Assert.False(ok);
        Assert.Contains(errors, e => e.Parameter == "pageSize");
    }

    [Fact]
    public void TryCreate_PageSizeAtMaximum_IsAccepted()
    {
        var ok = PagingRequest.TryCreate(null, 100, null, null, null, MaineKeys, out var request, out _);

        Assert.True(ok);
        Assert.Equal(100, request.PageSize);
    }

    [Fact]
    public void TryCreate_UnknownSortKey_IsRejectedAndListsAllowedKeys()
    {
        var ok = PagingRequest.TryCreate(null, null, "publisher", null, null, MaineKeys, out _, out var errors);

        Assert.False(ok);
        var error = Assert.Single(errors, e => e.Parameter == "sort");
        Assert.Contains("pages", error.Message);
        Assert.Contains("bookName", error.Message);
        Assert.Contains("villainName", error.Message);
    }

    [Fact]
    public void TryCreate_SortKeyNotAllowedForThisEndpoint_IsRejected()
    {
        // bookName is meaningless on the villain-shaped Derry list.
        var ok = PagingRequest.TryCreate(null, null, "bookName", null, null, DerryKeys, out _, out var errors);

        Assert.False(ok);
        Assert.Contains(errors, e => e.Parameter == "sort");
    }

    [Fact]
    public void TryCreate_UnknownDirection_IsRejected()
    {
        var ok = PagingRequest.TryCreate(null, null, null, "sideways", null, MaineKeys, out _, out var errors);

        Assert.False(ok);
        Assert.Contains(errors, e => e.Parameter == "dir");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void TryCreate_YearOutOfRange_IsRejected(int year)
    {
        var ok = PagingRequest.TryCreate(null, null, null, null, year, MaineKeys, out _, out var errors);

        Assert.False(ok);
        Assert.Contains(errors, e => e.Parameter == "year");
    }

    [Fact]
    public void TryCreate_YearInRange_IsAccepted()
    {
        var ok = PagingRequest.TryCreate(null, null, null, null, 1986, MaineKeys, out var request, out _);

        Assert.True(ok);
        Assert.Equal(1986, request.Year);
    }

    [Fact]
    public void TryCreate_SeveralViolations_ReportsAllOfThemNotJustTheFirst()
    {
        var ok = PagingRequest.TryCreate(-1, 500, "publisher", "sideways", 1800, MaineKeys,
                                         out _, out var errors);

        Assert.False(ok);
        Assert.Equal(5, errors.Count);
        Assert.Contains(errors, e => e.Parameter == "page");
        Assert.Contains(errors, e => e.Parameter == "pageSize");
        Assert.Contains(errors, e => e.Parameter == "sort");
        Assert.Contains(errors, e => e.Parameter == "dir");
        Assert.Contains(errors, e => e.Parameter == "year");
    }

    [Fact]
    public void Skip_HugePageOverflowsTheProduct_ClampsToIntMaxValueInsteadOfWrappingNegative()
    {
        // page = 30,000,000 with pageSize = 100 overflows (page-1)*pageSize as a 32-bit int and
        // wraps negative. Enumerable.Skip treats a negative count as zero, so an unclamped Skip
        // would silently return page 1's rows for a page far beyond any possible result.
        var ok = PagingRequest.TryCreate(30_000_000, 100, null, null, null, MaineKeys,
                                         out var request, out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.Equal(int.MaxValue, request.Skip);
    }

    [Fact]
    public void SortName_AndDirectionName_MatchTheWireVocabulary()
    {
        PagingRequest.TryCreate(null, null, "villainName", "desc", null, MaineKeys, out var request, out _);

        Assert.Equal("villainName", request.SortName);
        Assert.Equal("desc", request.DirectionName);
    }
}
