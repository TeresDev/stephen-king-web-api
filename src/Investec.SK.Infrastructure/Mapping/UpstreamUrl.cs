using System.Globalization;

namespace Investec.SK.Infrastructure.Mapping;

/// <summary>
/// Cross-references carry a url such as /api/book/2. Parsing the trailing id is a reliable join
/// key; matching on title strings is not, so it is never done.
/// </summary>
public static class UpstreamUrl
{
    public static bool TryParseId(string? url, out int id)
    {
        id = 0;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var lastSegment = url.TrimEnd('/').Split('/').LastOrDefault();

        if (string.IsNullOrWhiteSpace(lastSegment)
            || !int.TryParse(lastSegment, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed <= 0)
        {
            return false;
        }

        id = parsed;
        return true;
    }
}
