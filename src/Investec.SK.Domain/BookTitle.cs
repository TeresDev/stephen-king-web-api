namespace Investec.SK.Domain;

public sealed record BookTitle
{
    private BookTitle(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? raw, out BookTitle value)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            value = new BookTitle(raw.Trim());
            return true;
        }

        value = null!;
        return false;
    }

    public override string ToString() => Value;
}
