namespace Investec.SK.Domain;

/// <summary>
/// A non-blank ISBN as supplied by the upstream feed. Upstream formatting is inconsistent, so
/// this type guarantees presence rather than checksum validity.
/// </summary>
public sealed record Isbn
{
    private Isbn(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? raw, out Isbn value)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            value = new Isbn(raw.Trim());
            return true;
        }

        value = null!;
        return false;
    }

    public override string ToString() => Value;
}
