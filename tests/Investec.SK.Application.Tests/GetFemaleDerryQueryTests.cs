using Investec.SK.Application.Derry;
using Investec.SK.Application.Queries;
using Investec.SK.Domain;

namespace Investec.SK.Application.Tests;

public class GetFemaleDerryQueryTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 29, 11, 16, 0, TimeSpan.Zero);

    private static GetFemaleDerryQuery Handler(FakeVillainRepository villains) =>
        new(villains, new FakeTimeProvider(At));

    private static FakeVillainRepository DerryCast() =>
        FakeVillainRepository.Returning(
            TestData.DerryVillain(1, "Greta Bowie", Gender.Female, "Derry residents",
                                  TestData.Link(1, "It")),
            TestData.DerryVillain(2, "Marcia Fadden", Gender.Female, "Derry residents",
                                  TestData.Link(1, "It")),
            TestData.DerryVillain(3, "Sally Mueller", Gender.Female, "Derry residents"),
            TestData.DerryVillain(4, "Henry Bowers", Gender.Male, "Derry residents",
                                  TestData.Link(1, "It")),
            TestData.DerryVillain(5, "It (Creature)", Gender.Unknown, "Derry residents"),
            TestData.MaineVillain(6, "Kurt Barlow", TestData.Link(2, "Salem's Lot")));

    [Fact]
    public async Task ExecuteAsync_ReturnsOnlyFemaleDerryResidents()
    {
        var result = await Handler(DerryCast())
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.Success, result.Status);
        Assert.Equal(["Greta Bowie", "Marcia Fadden", "Sally Mueller"],
                     result.Response!.Data.Select(v => v.VillainName));
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesMaleAndUnknownGenderDerryResidents()
    {
        var result = await Handler(DerryCast())
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        Assert.DoesNotContain(result.Response!.Data, v => v.VillainName == "Henry Bowers");
        Assert.DoesNotContain(result.Response.Data, v => v.VillainName == "It (Creature)");
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesFemaleVillainsWhoDoNotLiveInDerry()
    {
        var villains = FakeVillainRepository.Returning(
            TestData.Villain(1, "Margaret White", Gender.Female, ["Residents of Maine"]));

        var result = await Handler(villains)
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        Assert.Empty(result.Response!.Data);
    }

    [Theory]
    [InlineData("Derry residents")]
    [InlineData("Derry")]
    [InlineData("Dery residents")]
    [InlineData("Dery")]
    public async Task ExecuteAsync_TolerantOfEitherSpellingAndEchoesTheMatchedNote(string note)
    {
        var villains = FakeVillainRepository.Returning(
            TestData.DerryVillain(1, "Greta Bowie", Gender.Female, note));

        var result = await Handler(villains)
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        var villain = Assert.Single(result.Response!.Data);
        Assert.Equal(note, villain.ResidenceNote);
    }

    [Fact]
    public async Task ExecuteAsync_ListsBookTitlesTakenFromTheVillainFeed()
    {
        var villains = FakeVillainRepository.Returning(
            TestData.DerryVillain(1, "Greta Bowie", Gender.Female, "Derry residents",
                                  TestData.Link(1, "It"), TestData.Link(2, "Dreamcatcher")));

        var result = await Handler(villains)
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        Assert.Equal(["It", "Dreamcatcher"], Assert.Single(result.Response!.Data).Books);
    }

    [Fact]
    public async Task ExecuteAsync_DerryVillainWithNoBooks_IsStillReturnedWithAnEmptyBookList()
    {
        var villains = FakeVillainRepository.Returning(
            TestData.DerryVillain(1, "Sally Mueller", Gender.Female));

        var result = await Handler(villains)
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        // Unlike the Maine endpoint, having no books is not disqualifying here,
        // so this is NOT a skipped record.
        var villain = Assert.Single(result.Response!.Data);
        Assert.Empty(villain.Books);
        Assert.Empty(result.Response.Skipped);
        Assert.False(result.Response.Meta.Degraded);
    }

    [Fact]
    public async Task ExecuteAsync_VillainsSourceDown_IsUpstreamUnavailable()
    {
        var result = await Handler(FakeVillainRepository.Failing())
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        Assert.Equal(QueryStatus.UpstreamUnavailable, result.Status);
        Assert.Equal("villains", Assert.Single(result.Failures).Source);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultSortIsVillainNameAscending()
    {
        var villains = FakeVillainRepository.Returning(
            TestData.DerryVillain(1, "Zelda", Gender.Female),
            TestData.DerryVillain(2, "Annie", Gender.Female));

        var result = await Handler(villains)
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        Assert.Equal(["Annie", "Zelda"], result.Response!.Data.Select(v => v.VillainName));
        Assert.Equal("villainName", result.Response.Meta.Sort);
        Assert.Equal("asc", result.Response.Meta.Direction);
    }

    [Fact]
    public async Task ExecuteAsync_DescendingDirection_ReversesTheOrder()
    {
        var villains = FakeVillainRepository.Returning(
            TestData.DerryVillain(1, "Annie", Gender.Female),
            TestData.DerryVillain(2, "Zelda", Gender.Female));

        var result = await Handler(villains)
            .ExecuteAsync(null, null, null, "desc", CancellationToken.None);

        Assert.Equal(["Zelda", "Annie"], result.Response!.Data.Select(v => v.VillainName));
    }

    [Fact]
    public async Task ExecuteAsync_BookNameSortKey_IsRejectedForThisEndpoint()
    {
        var result = await Handler(DerryCast())
            .ExecuteAsync(null, null, "bookName", null, CancellationToken.None);

        Assert.Equal(QueryStatus.InvalidRequest, result.Status);
        Assert.Contains(result.ValidationErrors, e => e.Parameter == "sort");
    }

    [Fact]
    public async Task ExecuteAsync_Paging_CountsVillainRowsInMeta()
    {
        var result = await Handler(DerryCast())
            .ExecuteAsync(2, 2, null, null, CancellationToken.None);

        Assert.Equal(3, result.Response!.Meta.TotalItems);
        Assert.Equal(2, result.Response.Meta.TotalPages);
        Assert.Equal(1, result.Response.Meta.ItemsOnPage);
        Assert.Equal("Sally Mueller", result.Response.Data[0].VillainName);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsGenderAsAReadableString()
    {
        var result = await Handler(DerryCast())
            .ExecuteAsync(null, null, null, null, CancellationToken.None);

        Assert.All(result.Response!.Data, v => Assert.Equal("Female", v.Gender));
    }
}
