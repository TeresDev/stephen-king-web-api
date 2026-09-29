using Investec.SK.Domain;

namespace Investec.SK.Domain.Tests;

public class PublishYearTests
{
    [Theory]
    [InlineData(1900)]
    [InlineData(1974)]
    [InlineData(2100)]
    public void TryCreate_YearInRange_Succeeds(int raw)
    {
        Assert.True(PublishYear.TryCreate(raw, out var year));
        Assert.Equal(raw, year.Value);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(null)]
    public void TryCreate_YearOutOfRangeOrNull_Fails(int? raw)
    {
        Assert.False(PublishYear.TryCreate(raw, out var year));
        Assert.Equal(default, year);
    }
}

public class PageCountTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(199)]
    [InlineData(1138)]
    public void TryCreate_PositiveCount_Succeeds(int raw)
    {
        Assert.True(PageCount.TryCreate(raw, out var pages));
        Assert.Equal(raw, pages.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(null)]
    public void TryCreate_NonPositiveOrNull_Fails(int? raw)
    {
        Assert.False(PageCount.TryCreate(raw, out var pages));
        Assert.Equal(default, pages);
    }
}

public class IsbnTests
{
    [Fact]
    public void TryCreate_WellFormed_SucceedsAndPreservesValue()
    {
        Assert.True(Isbn.TryCreate("978-0-385-08695-0", out var isbn));
        Assert.Equal("978-0-385-08695-0", isbn.Value);
    }

    [Fact]
    public void TryCreate_SurroundingWhitespace_IsTrimmed()
    {
        Assert.True(Isbn.TryCreate("  978-0-385-08695-0  ", out var isbn));
        Assert.Equal("978-0-385-08695-0", isbn.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryCreate_NullOrBlank_Fails(string? raw)
    {
        Assert.False(Isbn.TryCreate(raw, out var isbn));
        Assert.Null(isbn);
    }
}

public class BookTitleTests
{
    [Fact]
    public void TryCreate_NonBlank_SucceedsAndTrims()
    {
        Assert.True(BookTitle.TryCreate("  It  ", out var title));
        Assert.Equal("It", title.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void TryCreate_NullOrBlank_Fails(string? raw)
    {
        Assert.False(BookTitle.TryCreate(raw, out var title));
        Assert.Null(title);
    }
}

public class VillainNameTests
{
    [Fact]
    public void TryCreate_NonBlank_SucceedsAndTrims()
    {
        Assert.True(VillainName.TryCreate(" Pennywise ", out var name));
        Assert.Equal("Pennywise", name.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void TryCreate_NullOrBlank_Fails(string? raw)
    {
        Assert.False(VillainName.TryCreate(raw, out var name));
        Assert.Null(name);
    }
}

public class IdentifierTests
{
    [Fact]
    public void BookId_WithSameValue_IsEqual() =>
        Assert.Equal(new BookId(2), new BookId(2));

    [Fact]
    public void VillainId_WithDifferentValue_IsNotEqual() =>
        Assert.NotEqual(new VillainId(2), new VillainId(3));
}
