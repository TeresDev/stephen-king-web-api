using Investec.SK.Domain;

namespace Investec.SK.Domain.Tests;

public class BookTests
{
    private static Book CarrieWithVillains(params int[] villainIds)
    {
        BookTitle.TryCreate("Carrie", out var title);
        PublishYear.TryCreate(1974, out var year);
        Isbn.TryCreate("978-0-385-08695-0", out var isbn);
        PageCount.TryCreate(199, out var pages);

        return new Book(
            new BookId(1), title, year, isbn, pages, "Doubleday",
            villainIds.Select(id => new VillainId(id)).ToArray());
    }

    [Fact]
    public void Book_ExposesEveryFieldTheMaineEndpointNeeds()
    {
        var book = CarrieWithVillains(4, 14);

        Assert.Equal(1, book.Id.Value);
        Assert.Equal("Carrie", book.Title.Value);
        Assert.Equal(1974, book.Year.Value);
        Assert.Equal("978-0-385-08695-0", book.Isbn.Value);
        Assert.Equal(199, book.Pages.Value);
        Assert.Equal("Doubleday", book.Publisher);
        Assert.Equal(2, book.VillainRefs.Count);
    }

    [Fact]
    public void Book_PublisherIsOptional()
    {
        BookTitle.TryCreate("Carrie", out var title);
        PublishYear.TryCreate(1974, out var year);
        Isbn.TryCreate("978-0-385-08695-0", out var isbn);
        PageCount.TryCreate(199, out var pages);

        var book = new Book(new BookId(1), title, year, isbn, pages, null, []);

        Assert.Null(book.Publisher);
    }
}

public class VillainTests
{
    [Fact]
    public void Villain_ExposesResidenceAndBookReferences()
    {
        VillainName.TryCreate("Kurt Barlow", out var name);

        var villain = new Villain(
            new VillainId(2), name, Gender.Male, "Deceased",
            ResidenceNotes.From(["Residents of Maine"]),
            [new BookLink(new BookId(2), "Salem's Lot")]);

        Assert.Equal("Kurt Barlow", villain.Name.Value);
        Assert.Equal(Gender.Male, villain.Gender);
        Assert.True(villain.Notes.IsResidentOfMaine);
        var link = Assert.Single(villain.BookRefs);
        Assert.Equal(2, link.Id.Value);
        Assert.Equal("Salem's Lot", link.Title);
    }

    [Fact]
    public void Villain_WithNoBooks_HasEmptyBookRefs()
    {
        VillainName.TryCreate("Bob Anderson", out var name);

        var villain = new Villain(
            new VillainId(1), name, Gender.Male, "Deceased",
            ResidenceNotes.From(["Residents of Maine"]),
            []);

        Assert.Empty(villain.BookRefs);
    }
}
