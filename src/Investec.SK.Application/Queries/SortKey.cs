namespace Investec.SK.Application.Queries;

public enum SortKey
{
    Pages,
    BookName,
    VillainName,
}

public enum SortDirection
{
    Asc,
    Desc,
}

public static class SortKeyNames
{
    public const string Pages = "pages";
    public const string BookName = "bookName";
    public const string VillainName = "villainName";

    public static string ToWireName(this SortKey key) => key switch
    {
        SortKey.Pages => Pages,
        SortKey.BookName => BookName,
        SortKey.VillainName => VillainName,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unhandled sort key"),
    };

    public static bool TryParse(string raw, out SortKey key)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "pages": key = SortKey.Pages; return true;
            case "bookname": key = SortKey.BookName; return true;
            case "villainname": key = SortKey.VillainName; return true;
            default: key = default; return false;
        }
    }
}
