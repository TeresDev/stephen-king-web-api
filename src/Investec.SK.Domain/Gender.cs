namespace Investec.SK.Domain;

/// <summary>
/// Upstream supplies "Male", "Female", "Unknown" or null. <see cref="Unspecified"/> and
/// <see cref="Unknown"/> are deliberately distinct: the first means the feed said nothing,
/// the second means the feed positively recorded the gender as unknown.
/// </summary>
public enum Gender
{
    Unspecified = 0,
    Male = 1,
    Female = 2,
    Unknown = 3,
}

public static class GenderParser
{
    public static Gender Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Gender.Unspecified;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "male" => Gender.Male,
            "female" => Gender.Female,
            _ => Gender.Unknown,
        };
    }
}
