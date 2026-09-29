using Investec.SK.Application.Maine;
using Investec.SK.Application.Queries;

namespace Investec.SK.Application.Tests;

public class MaineRowSorterTests
{
    private static readonly MaineRow[] Rows =
    [
        new("It", "isbn-it", 1138, "Pennywise", 1986),
        new("Carrie", "isbn-carrie", 199, "Margaret White", 1974),
        new("The Stand", "isbn-stand", 823, "Randall Flagg", 1978),
    ];

    [Fact]
    public void Sort_ByPagesDescending_PutsLongestBookFirst()
    {
        var sorted = MaineRowSorter.Sort(Rows, SortKey.Pages, SortDirection.Desc);

        Assert.Equal(["It", "The Stand", "Carrie"], sorted.Select(r => r.BookName));
    }

    [Fact]
    public void Sort_ByPagesAscending_PutsShortestBookFirst()
    {
        var sorted = MaineRowSorter.Sort(Rows, SortKey.Pages, SortDirection.Asc);

        Assert.Equal(["Carrie", "The Stand", "It"], sorted.Select(r => r.BookName));
    }

    [Fact]
    public void Sort_ByBookNameAscending_IsAlphabetical()
    {
        var sorted = MaineRowSorter.Sort(Rows, SortKey.BookName, SortDirection.Asc);

        Assert.Equal(["Carrie", "It", "The Stand"], sorted.Select(r => r.BookName));
    }

    [Fact]
    public void Sort_ByVillainNameAscending_IsAlphabetical()
    {
        var sorted = MaineRowSorter.Sort(Rows, SortKey.VillainName, SortDirection.Asc);

        Assert.Equal(["Margaret White", "Pennywise", "Randall Flagg"],
                     sorted.Select(r => r.VillainName));
    }

    [Fact]
    public void Sort_EqualPages_BreaksTieOnBookNameAscending()
    {
        MaineRow[] tied =
        [
            new("Zebra", "z", 500, "Villain A", 1990),
            new("Apple", "a", 500, "Villain B", 1991),
        ];

        var sorted = MaineRowSorter.Sort(tied, SortKey.Pages, SortDirection.Desc);

        Assert.Equal(["Apple", "Zebra"], sorted.Select(r => r.BookName));
    }

    [Fact]
    public void Sort_EqualPagesAndBookName_BreaksTieOnVillainNameAscending()
    {
        MaineRow[] tied =
        [
            new("It", "i", 1138, "Pennywise", 1986),
            new("It", "i", 1138, "Henry Bowers", 1986),
        ];

        var sorted = MaineRowSorter.Sort(tied, SortKey.Pages, SortDirection.Desc);

        Assert.Equal(["Henry Bowers", "Pennywise"], sorted.Select(r => r.VillainName));
    }

    [Fact]
    public void Sort_IsStableAcrossRepeatedCalls()
    {
        var first = MaineRowSorter.Sort(Rows, SortKey.Pages, SortDirection.Desc);
        var second = MaineRowSorter.Sort(Rows, SortKey.Pages, SortDirection.Desc);

        Assert.Equal(first.Select(r => r.BookName), second.Select(r => r.BookName));
    }

    [Fact]
    public void Sort_EmptyInput_ReturnsEmpty() =>
        Assert.Empty(MaineRowSorter.Sort([], SortKey.Pages, SortDirection.Desc));

    [Fact]
    public void Sort_DoesNotMutateTheInput()
    {
        var input = Rows.ToArray();

        MaineRowSorter.Sort(input, SortKey.BookName, SortDirection.Asc);

        Assert.Equal("It", input[0].BookName);
    }
}
