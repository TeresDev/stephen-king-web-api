using Investec.SK.Application.Responses;
using Investec.SK.Domain;
using Investec.SK.Infrastructure.Contracts;

namespace Investec.SK.Infrastructure.Mapping;

/// <summary>
/// Turns an untrusted villain contract into a validated <see cref="Villain"/>. A single unusable
/// book link is reported but does not discard the villain.
/// </summary>
public static class VillainMapper
{
    public static MappingOutcome<Villain> Map(VillainContract contract)
    {
        var label = string.IsNullOrWhiteSpace(contract.Name)
            ? $"villain {contract.Id?.ToString() ?? "(no id)"}"
            : contract.Name.Trim();

        if (contract.Id is not > 0)
        {
            return MappingOutcome<Villain>.Rejected(new SkippedRecord(
                "Villain", label, SkipReason.InvalidIdentifier, "Upstream supplied no usable villain id"));
        }

        if (!VillainName.TryCreate(contract.Name, out var name))
        {
            return MappingOutcome<Villain>.Rejected(new SkippedRecord(
                "Villain", label, SkipReason.MissingVillainName, "Upstream supplied no villain name"));
        }

        var links = new List<BookLink>();
        var skipped = new List<SkippedRecord>();

        foreach (var reference in contract.Books ?? [])
        {
            if (!UpstreamUrl.TryParseId(reference.Url, out var bookId))
            {
                skipped.Add(new SkippedRecord(
                    "BookReference",
                    $"{name.Value} -> {reference.Url ?? "(no url)"}",
                    SkipReason.UnresolvedBookReference,
                    "Book reference url could not be parsed into an id"));
                continue;
            }

            var title = string.IsNullOrWhiteSpace(reference.Title)
                ? $"Book {bookId}"
                : reference.Title.Trim();

            links.Add(new BookLink(new BookId(bookId), title));
        }

        var villain = new Villain(
            new VillainId(contract.Id.Value),
            name,
            GenderParser.Parse(contract.Gender),
            contract.Status,
            ResidenceNotes.From(contract.Notes),
            links);

        return MappingOutcome<Villain>.Mapped(villain, skipped);
    }

    public static MappedSet<Villain> MapAll(IEnumerable<VillainContract>? contracts)
    {
        var items = new List<Villain>();
        var skipped = new List<SkippedRecord>();

        foreach (var outcome in (contracts ?? []).Select(Map))
        {
            if (outcome.Value is not null) items.Add(outcome.Value);
            skipped.AddRange(outcome.Skipped);
        }

        return new MappedSet<Villain>(items, skipped);
    }
}
