using Investec.SK.Application.Maine;

namespace Investec.SK.Application.Tests;

public class MainePagerTakePageTests
{
    private static MaineRow[] Rows(int count) =>
        Enumerable.Range(1, count)
                  .Select(i => new MaineRow($"Book {i:D2}", $"isbn-{i}", 1000 - i, $"Villain {i:D2}", 1980))
                  .ToArray();

    [Fact]
    public void TakePage_FirstPage_ReturnsFirstPageSizeRows()
    {
        var page = MainePager.TakePage(Rows(36), page: 1, pageSize: 20);

        Assert.Equal(20, page.Count);
        Assert.Equal("Book 01", page[0].BookName);
        Assert.Equal("Book 20", page[19].BookName);
    }

    [Fact]
    public void TakePage_LastPartialPage_ReturnsTheRemainder()
    {
        var page = MainePager.TakePage(Rows(36), page: 2, pageSize: 20);

        Assert.Equal(16, page.Count);
        Assert.Equal("Book 21", page[0].BookName);
        Assert.Equal("Book 36", page[15].BookName);
    }

    [Fact]
    public void TakePage_BeyondLastPage_ReturnsEmptyRatherThanThrowing()
    {
        var page = MainePager.TakePage(Rows(36), page: 999, pageSize: 20);

        Assert.Empty(page);
    }

    [Fact]
    public void TakePage_PageSizeLargerThanResultSet_ReturnsEverything()
    {
        var page = MainePager.TakePage(Rows(36), page: 1, pageSize: 100);

        Assert.Equal(36, page.Count);
    }

    [Fact]
    public void TakePage_EmptyInput_ReturnsEmpty() =>
        Assert.Empty(MainePager.TakePage([], page: 1, pageSize: 20));

    [Fact]
    public void TakePage_HugePageOverflowsTheSkipCalculation_ReturnsEmptyRatherThanPageOne()
    {
        // (page - 1) * pageSize overflows a 32-bit int here and wraps negative; Enumerable.Skip
        // treats a negative count as zero, which would silently return page 1's rows.
        var page = MainePager.TakePage(Rows(36), page: int.MaxValue, pageSize: 100);

        Assert.Empty(page);
    }
}

public class MainePagerGroupByYearTests
{
    [Fact]
    public void GroupByYear_RowsOfOneYear_ProduceOneGroup()
    {
        MaineRow[] rows =
        [
            new("It", "i", 1138, "Pennywise", 1986),
            new("It", "i", 1138, "Henry Bowers", 1986),
        ];

        var groups = MainePager.GroupByYear(rows);

        var group = Assert.Single(groups);
        Assert.Equal(1986, group.Year);
        Assert.Equal(2, group.Items.Count);
    }

    [Fact]
    public void GroupByYear_EmitsGroupsInTheOrderTheirFirstRowAppears()
    {
        // Sorted by pages desc: 1986, 1978, 1974 — NOT ascending by year.
        MaineRow[] rows =
        [
            new("It", "i", 1138, "Pennywise", 1986),
            new("The Stand", "s", 823, "Randall Flagg", 1978),
            new("Carrie", "c", 199, "Margaret White", 1974),
        ];

        var groups = MainePager.GroupByYear(rows);

        Assert.Equal([1986, 1978, 1974], groups.Select(g => g.Year));
    }

    [Fact]
    public void GroupByYear_NonContiguousRowsOfSameYear_CollapseIntoOneGroupAtItsFirstPosition()
    {
        MaineRow[] rows =
        [
            new("It", "i", 1138, "Pennywise", 1986),
            new("The Stand", "s", 823, "Randall Flagg", 1978),
            new("Misery", "m", 310, "Annie Wilkes", 1986),
        ];

        var groups = MainePager.GroupByYear(rows);

        Assert.Equal(2, groups.Count);
        Assert.Equal(1986, groups[0].Year);
        Assert.Equal(2, groups[0].Items.Count);
        Assert.Equal(["It", "Misery"], groups[0].Items.Select(i => i.BookName));
        Assert.Equal(1978, groups[1].Year);
    }

    [Fact]
    public void GroupByYear_DropsTheYearFromEachItemBecauseTheGroupCarriesIt()
    {
        MaineRow[] rows = [new("It", "isbn-it", 1138, "Pennywise", 1986)];

        var item = Assert.Single(MainePager.GroupByYear(rows)[0].Items);

        Assert.Equal("It", item.BookName);
        Assert.Equal("isbn-it", item.Isbn);
        Assert.Equal(1138, item.Pages);
        Assert.Equal("Pennywise", item.VillainName);
    }

    [Fact]
    public void GroupByYear_EmptyPage_ReturnsNoGroups() =>
        Assert.Empty(MainePager.GroupByYear([]));

    [Fact]
    public void PagingThenGrouping_LetsAYearStraddleAPageBoundary()
    {
        MaineRow[] sorted =
        [
            new("It", "i", 1138, "Pennywise", 1986),
            new("Misery", "m", 310, "Annie Wilkes", 1986),
            new("Carrie", "c", 199, "Margaret White", 1974),
        ];

        var firstPage = MainePager.GroupByYear(MainePager.TakePage(sorted, 1, 1));
        var secondPage = MainePager.GroupByYear(MainePager.TakePage(sorted, 2, 1));

        // 1986 legitimately appears on both pages: rows, not years, are the paging unit.
        Assert.Equal(1986, Assert.Single(firstPage).Year);
        Assert.Equal(1986, Assert.Single(secondPage).Year);
        Assert.Equal("It", firstPage[0].Items[0].BookName);
        Assert.Equal("Misery", secondPage[0].Items[0].BookName);
    }
}
