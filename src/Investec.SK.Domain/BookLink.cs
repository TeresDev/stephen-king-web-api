namespace Investec.SK.Domain;

/// <summary>
/// A villain's reference to a book, carrying the title the villain feed embeds alongside the id.
/// Holding the title here is what lets the Derry endpoint list books without the books feed.
/// </summary>
public sealed record BookLink(BookId Id, string Title);
