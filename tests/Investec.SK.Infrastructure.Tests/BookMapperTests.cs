using System.Text.Json;
using Investec.SK.Application.Responses;
using Investec.SK.Infrastructure.Contracts;
using Investec.SK.Infrastructure.Mapping;

namespace Investec.SK.Infrastructure.Tests;

public class BookMapperTests
{
    private static BookContract Valid() => new()
    {
        Id = 1,
        Year = 1974,
        Title = "Carrie",
        Publisher = "Doubleday",
        Isbn = "978-0-385-08695-0",
        Pages = 199,
        Villains = [new ResourceRef { Name = "Margaret White", Url = "/api/villain/113" }],
    };

    [Fact]
    public void Map_ValidContract_ProducesABookWithNoSkips()
    {
        var outcome = BookMapper.Map(Valid());

        Assert.NotNull(outcome.Value);
        Assert.Empty(outcome.Skipped);
        Assert.Equal(1, outcome.Value.Id.Value);
        Assert.Equal("Carrie", outcome.Value.Title.Value);
        Assert.Equal(1974, outcome.Value.Year.Value);
        Assert.Equal("978-0-385-08695-0", outcome.Value.Isbn.Value);
        Assert.Equal(199, outcome.Value.Pages.Value);
        Assert.Equal("Doubleday", outcome.Value.Publisher);
        Assert.Equal(113, Assert.Single(outcome.Value.VillainRefs).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Map_MissingOrNonPositiveId_IsRejected(int? id)
    {
        var outcome = BookMapper.Map(Valid() with { Id = id });

        Assert.Null(outcome.Value);
        Assert.Equal(SkipReason.InvalidIdentifier, Assert.Single(outcome.Skipped).Reason);
    }

    [Fact]
    public void Map_BlankTitle_IsRejectedAsMissingTitle()
    {
        var outcome = BookMapper.Map(Valid() with { Title = "   " });

        Assert.Null(outcome.Value);
        Assert.Equal(SkipReason.MissingTitle, Assert.Single(outcome.Skipped).Reason);
    }

    [Fact]
    public void Map_MissingPages_IsRejectedAsInvalidPageCount()
    {
        var outcome = BookMapper.Map(Valid() with { Pages = null });

        Assert.Null(outcome.Value);
        var skipped = Assert.Single(outcome.Skipped);
        Assert.Equal(SkipReason.InvalidPageCount, skipped.Reason);
        Assert.Equal("Carrie", skipped.Identifier);
    }

    [Fact]
    public void Map_BlankIsbn_IsRejectedAsInvalidIsbn()
    {
        var outcome = BookMapper.Map(Valid() with { Isbn = "" });

        Assert.Null(outcome.Value);
        Assert.Equal(SkipReason.InvalidIsbn, Assert.Single(outcome.Skipped).Reason);
    }

    [Fact]
    public void Map_OutOfRangeYear_IsRejectedAsInvalidYear()
    {
        var outcome = BookMapper.Map(Valid() with { Year = 1492 });

        Assert.Null(outcome.Value);
        Assert.Equal(SkipReason.InvalidYear, Assert.Single(outcome.Skipped).Reason);
    }

    [Fact]
    public void Map_UnparseableVillainUrl_IsIgnoredWithoutRejectingTheBook()
    {
        var outcome = BookMapper.Map(Valid() with
        {
            Villains = [new ResourceRef { Name = "Nobody", Url = "not-a-url" }],
        });

        Assert.NotNull(outcome.Value);
        Assert.Empty(outcome.Value.VillainRefs);
    }

    [Fact]
    public void MapAll_GoldenPayload_MapsAllSixtyFourBooksWithNoSkips()
    {
        var contracts = JsonSerializer
            .Deserialize<UpstreamEnvelope<BookContract>>(Fixtures.Books, Fixtures.Json)!.Data!;

        var mapped = BookMapper.MapAll(contracts);

        Assert.Equal(64, mapped.Items.Count);
        Assert.Empty(mapped.Skipped);
    }

    [Fact]
    public void MapAll_Null_ReturnsEmpty()
    {
        var mapped = BookMapper.MapAll(null);

        Assert.Empty(mapped.Items);
        Assert.Empty(mapped.Skipped);
    }
}
