using System.Text.Json;
using Investec.SK.Application.Responses;
using Investec.SK.Domain;
using Investec.SK.Infrastructure.Contracts;
using Investec.SK.Infrastructure.Mapping;

namespace Investec.SK.Infrastructure.Tests;

public class VillainMapperTests
{
    private static VillainContract Valid() => new()
    {
        Id = 2,
        Name = "Kurt Barlow",
        Gender = "Male",
        Status = "Deceased",
        Notes = ["Characters", "Residents of Maine"],
        Books = [new ResourceRef { Title = "Salem's Lot", Url = "/api/book/2" }],
    };

    [Fact]
    public void Map_ValidContract_ProducesAVillainWithNoSkips()
    {
        var outcome = VillainMapper.Map(Valid());

        Assert.NotNull(outcome.Value);
        Assert.Empty(outcome.Skipped);
        Assert.Equal("Kurt Barlow", outcome.Value.Name.Value);
        Assert.Equal(Gender.Male, outcome.Value.Gender);
        Assert.True(outcome.Value.Notes.IsResidentOfMaine);
        var link = Assert.Single(outcome.Value.BookRefs);
        Assert.Equal(2, link.Id.Value);
        Assert.Equal("Salem's Lot", link.Title);
    }

    [Fact]
    public void Map_NullGender_BecomesUnspecified()
    {
        var outcome = VillainMapper.Map(Valid() with { Gender = null });

        Assert.Equal(Investec.SK.Domain.Gender.Unspecified, outcome.Value!.Gender);
    }

    [Fact]
    public void Map_BlankName_IsRejected()
    {
        var outcome = VillainMapper.Map(Valid() with { Name = "  " });

        Assert.Null(outcome.Value);
        Assert.Equal(SkipReason.MissingVillainName, Assert.Single(outcome.Skipped).Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Map_MissingOrNonPositiveId_IsRejected(int? id)
    {
        var outcome = VillainMapper.Map(Valid() with { Id = id });

        Assert.Null(outcome.Value);
        Assert.Equal(SkipReason.InvalidIdentifier, Assert.Single(outcome.Skipped).Reason);
    }

    [Fact]
    public void Map_NullNotes_ProducesAVillainWithNoResidence()
    {
        var outcome = VillainMapper.Map(Valid() with { Notes = null });

        Assert.NotNull(outcome.Value);
        Assert.False(outcome.Value.Notes.IsResidentOfMaine);
        Assert.False(outcome.Value.Notes.IsResidentOfDerry);
    }

    [Fact]
    public void Map_BookLinkWithUnparseableUrl_KeepsTheVillainAndReportsThatOneLink()
    {
        var outcome = VillainMapper.Map(Valid() with
        {
            Books =
            [
                new ResourceRef { Title = "Salem's Lot", Url = "/api/book/2" },
                new ResourceRef { Title = "Broken", Url = "not-a-url" },
            ],
        });

        Assert.NotNull(outcome.Value);
        Assert.Single(outcome.Value.BookRefs);
        var skipped = Assert.Single(outcome.Skipped);
        Assert.Equal(SkipReason.UnresolvedBookReference, skipped.Reason);
        Assert.Contains("Kurt Barlow", skipped.Identifier);
    }

    [Fact]
    public void Map_BookLinkWithBlankTitle_FallsBackToAPlaceholderRatherThanDropping()
    {
        var outcome = VillainMapper.Map(Valid() with
        {
            Books = [new ResourceRef { Title = null, Url = "/api/book/2" }],
        });

        var link = Assert.Single(outcome.Value!.BookRefs);
        Assert.Equal(2, link.Id.Value);
        Assert.Equal("Book 2", link.Title);
    }

    [Fact]
    public void MapAll_GoldenPayload_MapsAllOneHundredAndFifteenVillainsWithNoSkips()
    {
        var contracts = JsonSerializer
            .Deserialize<UpstreamEnvelope<VillainContract>>(Fixtures.Villains, Fixtures.Json)!.Data!;

        var mapped = VillainMapper.MapAll(contracts);

        Assert.Equal(115, mapped.Items.Count);
        Assert.Empty(mapped.Skipped);
    }

    [Fact]
    public void MapAll_GoldenPayload_FindsThirtyEightMaineResidents()
    {
        var contracts = JsonSerializer
            .Deserialize<UpstreamEnvelope<VillainContract>>(Fixtures.Villains, Fixtures.Json)!.Data!;

        var mapped = VillainMapper.MapAll(contracts);

        Assert.Equal(38, mapped.Items.Count(v => v.Notes.IsResidentOfMaine));
    }

    [Fact]
    public void MapAll_GoldenPayload_FindsThreeFemaleDerryResidents()
    {
        var contracts = JsonSerializer
            .Deserialize<UpstreamEnvelope<VillainContract>>(Fixtures.Villains, Fixtures.Json)!.Data!;

        var mapped = VillainMapper.MapAll(contracts);

        Assert.Equal(3, mapped.Items.Count(
            v => v.Notes.IsResidentOfDerry && v.Gender == Investec.SK.Domain.Gender.Female));
    }
}
