using System.Text.Json;
using Investec.SK.Infrastructure.Contracts;

namespace Investec.SK.Infrastructure.Tests;

public class BookContractTests
{
    private static IReadOnlyList<BookContract> AllBooks() =>
        JsonSerializer.Deserialize<UpstreamEnvelope<BookContract>>(Fixtures.Books, Fixtures.Json)!.Data!;

    [Fact]
    public void Deserialise_GoldenPayload_ReadsEverySixtyFourBooks() =>
        Assert.Equal(64, AllBooks().Count);

    [Fact]
    public void Deserialise_ReadsPascalCaseFieldsTheBooksFeedUses()
    {
        var carrie = AllBooks().Single(b => b.Id == 1);

        Assert.Equal(1974, carrie.Year);
        Assert.Equal("Carrie", carrie.Title);
        Assert.Equal("Doubleday", carrie.Publisher);
        Assert.Equal("978-0-385-08695-0", carrie.Isbn);
        Assert.Equal(199, carrie.Pages);
    }

    [Fact]
    public void Deserialise_ReadsEmbeddedVillainReferences()
    {
        var carrie = AllBooks().Single(b => b.Id == 1);

        Assert.NotNull(carrie.Villains);
        Assert.Contains(carrie.Villains, v => v.Name == "Margaret White");
        Assert.All(carrie.Villains, v => Assert.False(string.IsNullOrWhiteSpace(v.Url)));
    }

    [Fact]
    public void Deserialise_MissingFields_BecomeNullRatherThanThrowing()
    {
        const string json = """{ "data": [ { "id": 1 } ] }""";

        var book = JsonSerializer.Deserialize<UpstreamEnvelope<BookContract>>(json, Fixtures.Json)!.Data![0];

        Assert.Equal(1, book.Id);
        Assert.Null(book.Title);
        Assert.Null(book.Year);
        Assert.Null(book.Pages);
        Assert.Null(book.Isbn);
        Assert.Null(book.Villains);
    }

    [Fact]
    public void Deserialise_UnknownExtraFields_AreIgnored()
    {
        const string json = """{ "data": [ { "id": 1, "Title": "Carrie", "somethingNew": 42 } ] }""";

        var book = JsonSerializer.Deserialize<UpstreamEnvelope<BookContract>>(json, Fixtures.Json)!.Data![0];

        Assert.Equal("Carrie", book.Title);
    }
}

public class VillainContractTests
{
    private static IReadOnlyList<VillainContract> AllVillains() =>
        JsonSerializer.Deserialize<UpstreamEnvelope<VillainContract>>(Fixtures.Villains, Fixtures.Json)!.Data!;

    [Fact]
    public void Deserialise_GoldenPayload_ReadsEveryOneHundredAndFifteenVillains() =>
        Assert.Equal(115, AllVillains().Count);

    [Fact]
    public void Deserialise_ReadsLowercaseFieldsTheVillainsFeedUses()
    {
        var barlow = AllVillains().Single(v => v.Id == 2);

        Assert.Equal("Kurt Barlow", barlow.Name);
        Assert.Equal("Male", barlow.Gender);
        Assert.Equal("Deceased", barlow.Status);
        Assert.Contains("Residents of Maine", barlow.Notes!);
    }

    [Fact]
    public void Deserialise_ReadsEmbeddedBookReferencesWithTitles()
    {
        var barlow = AllVillains().Single(v => v.Id == 2);

        var link = Assert.Single(barlow.Books!);
        Assert.Equal("Salem's Lot", link.Title);
        Assert.Contains("/api/book/2", link.Url);
    }

    [Fact]
    public void Deserialise_NullGender_StaysNull()
    {
        // 12 villains in the live feed have a null gender.
        Assert.Contains(AllVillains(), v => v.Gender is null);
    }

    [Fact]
    public void Deserialise_VillainWithNoBooks_HasAnEmptyList()
    {
        var anderson = AllVillains().Single(v => v.Id == 1);

        Assert.NotNull(anderson.Books);
        Assert.Empty(anderson.Books);
    }
}
