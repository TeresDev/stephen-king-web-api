namespace Investec.SK.Domain;

public sealed record VillainName
{
    private VillainName(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? raw, out VillainName value)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            value = new VillainName(raw.Trim());
            return true;
        }

        value = null!;
        return false;
    }

    public override string ToString() => Value;
}
