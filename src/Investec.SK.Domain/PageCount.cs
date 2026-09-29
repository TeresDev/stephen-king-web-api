namespace Investec.SK.Domain;

/// <summary>A strictly positive page count. A book with no pages cannot be ordered by length.</summary>
public readonly record struct PageCount
{
    private PageCount(int value) => Value = value;

    public int Value { get; }

    public static bool TryCreate(int? raw, out PageCount value)
    {
        if (raw is > 0)
        {
            value = new PageCount(raw.Value);
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
