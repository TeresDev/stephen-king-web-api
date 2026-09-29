namespace Investec.SK.Domain;

/// <summary>A publication year constrained to a plausible range for a published novel.</summary>
public readonly record struct PublishYear
{
    public const int Minimum = 1900;
    public const int Maximum = 2100;

    private PublishYear(int value) => Value = value;

    public int Value { get; }

    public static bool TryCreate(int? raw, out PublishYear value)
    {
        if (raw is >= Minimum and <= Maximum)
        {
            value = new PublishYear(raw.Value);
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
