using Investec.SK.Application.Responses;
using Investec.SK.Domain;
using Investec.SK.Infrastructure.Contracts;

namespace Investec.SK.Infrastructure.Mapping;

/// <summary>
/// Turns an untrusted book contract into a validated <see cref="Book"/>, or explains why it could
/// not. Nothing half-built is ever constructed.
/// </summary>
public static class BookMapper
{
    public static MappingOutcome<Book> Map(BookContract contract)
    {
        var label = string.IsNullOrWhiteSpace(contract.Title)
            ? $"book {contract.Id?.ToString() ?? "(no id)"}"
            : contract.Title.Trim();

        if (contract.Id is not > 0)
        {
            return Reject(label, SkipReason.InvalidIdentifier, "Upstream supplied no usable book id");
        }

        if (!BookTitle.TryCreate(contract.Title, out var title))
        {
            return Reject(label, SkipReason.MissingTitle, "Upstream supplied no book title");
        }

        if (!PublishYear.TryCreate(contract.Year, out var year))
        {
            return Reject(label, SkipReason.InvalidYear,
                $"Publish year '{contract.Year?.ToString() ?? "null"}' is outside {PublishYear.Minimum}-{PublishYear.Maximum}");
        }

        if (!Isbn.TryCreate(contract.Isbn, out var isbn))
        {
            return Reject(label, SkipReason.InvalidIsbn, "Upstream supplied no ISBN");
        }

        if (!PageCount.TryCreate(contract.Pages, out var pages))
        {
            return Reject(label, SkipReason.InvalidPageCount,
                $"Page count '{contract.Pages?.ToString() ?? "null"}' is not a positive number");
        }

        var villainRefs = (contract.Villains ?? [])
            .Select(v => UpstreamUrl.TryParseId(v.Url, out var id) ? new VillainId(id) : (VillainId?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToArray();

        return MappingOutcome<Book>.Mapped(
            new Book(new BookId(contract.Id.Value), title, year, isbn, pages,
                     contract.Publisher, villainRefs));
    }

    public static MappedSet<Book> MapAll(IEnumerable<BookContract>? contracts)
    {
        var items = new List<Book>();
        var skipped = new List<SkippedRecord>();

        foreach (var outcome in (contracts ?? []).Select(Map))
        {
            if (outcome.Value is not null) items.Add(outcome.Value);
            skipped.AddRange(outcome.Skipped);
        }

        return new MappedSet<Book>(items, skipped);
    }

    private static MappingOutcome<Book> Reject(string label, SkipReason reason, string detail) =>
        MappingOutcome<Book>.Rejected(new SkippedRecord("Book", label, reason, detail));
}
