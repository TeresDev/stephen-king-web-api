using Investec.SK.Domain;

namespace Investec.SK.Application.Tests;

/// <summary>
/// Hand-built domain objects for Application tests. Deliberately synthetic: Application must be
/// testable without any upstream payload, fixture file or HTTP client.
/// </summary>
public static class TestData
{
    public static Book Book(int id, string title, int year, int pages, string? isbn = null)
    {
        BookTitle.TryCreate(title, out var bookTitle);
        PublishYear.TryCreate(year, out var publishYear);
        PageCount.TryCreate(pages, out var pageCount);
        Isbn.TryCreate(isbn ?? $"978-0-000-{id:D5}-0", out var parsedIsbn);

        return new Book(new BookId(id), bookTitle, publishYear, parsedIsbn, pageCount, "Doubleday", []);
    }

    /// <summary>A villain's reference to a book, mirroring the embedded { title, url } upstream shape.</summary>
    public static BookLink Link(int id, string title) => new(new BookId(id), title);

    public static Villain Villain(
        int id,
        string name,
        Gender gender,
        IEnumerable<string> notes,
        params BookLink[] books)
    {
        VillainName.TryCreate(name, out var villainName);

        return new Villain(
            new VillainId(id),
            villainName,
            gender,
            "Deceased",
            ResidenceNotes.From(notes),
            books);
    }

    public static Villain MaineVillain(int id, string name, params BookLink[] books) =>
        Villain(id, name, Gender.Male, ["Residents of Maine"], books);

    public static Villain DerryVillain(
        int id,
        string name,
        Gender gender,
        string note = "Derry residents",
        params BookLink[] books) =>
        Villain(id, name, gender, [note], books);
}
