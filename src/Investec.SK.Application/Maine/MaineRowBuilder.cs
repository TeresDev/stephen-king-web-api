using Investec.SK.Application.Responses;
using Investec.SK.Domain;

namespace Investec.SK.Application.Maine;

/// <summary>
/// Joins Maine-resident villains to their books. A villain with no books, or a book reference
/// that resolves to nothing, is reported as skipped rather than silently dropped.
/// </summary>
public static class MaineRowBuilder
{
    public static MaineRowSet Build(IReadOnlyList<Villain> villains, IReadOnlyList<Book> books)
    {
        var booksById = books.ToDictionary(b => b.Id);
        var rows = new List<MaineRow>();
        var skipped = new List<SkippedRecord>();

        foreach (var villain in villains.Where(v => v.Notes.IsResidentOfMaine))
        {
            if (villain.BookRefs.Count == 0)
            {
                skipped.Add(new SkippedRecord(
                    "Villain",
                    villain.Name.Value,
                    SkipReason.NoLinkedBooks,
                    "Villain is a Maine resident but appears in no book"));
                continue;
            }

            foreach (var bookRef in villain.BookRefs)
            {
                if (!booksById.TryGetValue(bookRef.Id, out var book))
                {
                    skipped.Add(new SkippedRecord(
                        "BookReference",
                        $"{villain.Name.Value} -> book {bookRef.Id.Value}",
                        SkipReason.UnresolvedBookReference,
                        $"Book {bookRef.Id.Value} was referenced by the villain but is absent from the books feed"));
                    continue;
                }

                rows.Add(new MaineRow(
                    book.Title.Value,
                    book.Isbn.Value,
                    book.Pages.Value,
                    villain.Name.Value,
                    book.Year.Value));
            }
        }

        return new MaineRowSet(rows, skipped);
    }
}
