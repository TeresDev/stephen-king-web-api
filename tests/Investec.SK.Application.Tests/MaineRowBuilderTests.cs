using Investec.SK.Application.Maine;
using Investec.SK.Application.Responses;
using Investec.SK.Domain;

namespace Investec.SK.Application.Tests;

public class MaineRowBuilderTests
{
    [Fact]
    public void Build_MaineVillainWithOneBook_ProducesOneRow()
    {
        var books = new[] { TestData.Book(1, "It", 1986, 1138, "978-0-670-81302-5") };
        var villains = new[] { TestData.MaineVillain(10, "Pennywise", TestData.Link(1, "It")) };

        var result = MaineRowBuilder.Build(villains, books);

        var row = Assert.Single(result.Rows);
        Assert.Equal("It", row.BookName);
        Assert.Equal("978-0-670-81302-5", row.Isbn);
        Assert.Equal(1138, row.Pages);
        Assert.Equal("Pennywise", row.VillainName);
        Assert.Equal(1986, row.Year);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Build_MaineVillainInTwoBooks_ProducesOneRowPerBook()
    {
        var books = new[]
        {
            TestData.Book(1, "The Waste Lands", 1991, 422),
            TestData.Book(2, "Wizard and Glass", 1997, 787),
        };
        var villains = new[]
        {
            TestData.MaineVillain(10, "Blaine the Mono",
                TestData.Link(1, "The Waste Lands"), TestData.Link(2, "Wizard and Glass")),
        };

        var result = MaineRowBuilder.Build(villains, books);

        Assert.Equal(2, result.Rows.Count);
        Assert.Contains(result.Rows, r => r.BookName == "The Waste Lands");
        Assert.Contains(result.Rows, r => r.BookName == "Wizard and Glass");
    }

    [Fact]
    public void Build_NonMaineVillain_IsExcludedAndNotReportedAsSkipped()
    {
        var books = new[] { TestData.Book(1, "It", 1986, 1138) };
        var villains = new[]
        {
            TestData.Villain(10, "Henry Bowers", Gender.Male, ["Derry residents"], TestData.Link(1, "It")),
        };

        var result = MaineRowBuilder.Build(villains, books);

        Assert.Empty(result.Rows);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Build_MaineVillainWithNoBooks_IsReportedAsSkippedNotDropped()
    {
        var villains = new[] { TestData.MaineVillain(1, "Bob Anderson") };

        var result = MaineRowBuilder.Build(villains, []);

        Assert.Empty(result.Rows);
        var skipped = Assert.Single(result.Skipped);
        Assert.Equal("Villain", skipped.Kind);
        Assert.Equal("Bob Anderson", skipped.Identifier);
        Assert.Equal(SkipReason.NoLinkedBooks, skipped.Reason);
    }

    [Fact]
    public void Build_BookReferenceWithNoMatchingBook_IsReportedAsSkipped()
    {
        var books = new[] { TestData.Book(1, "It", 1986, 1138) };
        var villains = new[]
        {
            TestData.MaineVillain(10, "Pennywise",
                TestData.Link(1, "It"), TestData.Link(99, "A Book Absent From The Books Feed")),
        };

        var result = MaineRowBuilder.Build(villains, books);

        Assert.Single(result.Rows);
        var skipped = Assert.Single(result.Skipped);
        Assert.Equal("BookReference", skipped.Kind);
        Assert.Equal(SkipReason.UnresolvedBookReference, skipped.Reason);
        Assert.Contains("Pennywise", skipped.Identifier);
        Assert.Contains("99", skipped.Identifier);
    }

    [Fact]
    public void Build_NoVillainsAtAll_ProducesNothing()
    {
        var result = MaineRowBuilder.Build([], []);

        Assert.Empty(result.Rows);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void Build_MirrorsTheLiveFeedShape_ThirtySixRowsAndTwoSkips()
    {
        // The live feed has 38 Maine residents, 2 of which appear in no book.
        var books = Enumerable.Range(1, 36)
                              .Select(i => TestData.Book(i, $"Book {i}", 1980 + (i % 10), 100 + i))
                              .ToArray();

        var withBooks = Enumerable.Range(1, 36)
                                  .Select(i => TestData.MaineVillain(
                                      100 + i, $"Villain {i}", TestData.Link(i, $"Book {i}")));

        var withoutBooks = new[]
        {
            TestData.MaineVillain(1, "Bob Anderson"),
            TestData.MaineVillain(2, "Ace Merrill"),
        };

        var result = MaineRowBuilder.Build([.. withBooks, .. withoutBooks], books);

        Assert.Equal(36, result.Rows.Count);
        Assert.Equal(2, result.Skipped.Count);
        Assert.All(result.Skipped, s => Assert.Equal(SkipReason.NoLinkedBooks, s.Reason));
    }
}
