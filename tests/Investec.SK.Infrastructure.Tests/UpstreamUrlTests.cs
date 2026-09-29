using Investec.SK.Infrastructure.Mapping;

namespace Investec.SK.Infrastructure.Tests;

public class UpstreamUrlTests
{
    [Theory]
    [InlineData("https://stephen-king-api.onrender.com/api/book/2", 2)]
    [InlineData("https://stephen-king-api.onrender.com/api/villain/113", 113)]
    [InlineData("/api/book/7", 7)]
    [InlineData("https://stephen-king-api.onrender.com/api/book/2/", 2)]
    public void TryParseId_WellFormedUrl_ExtractsTrailingId(string url, int expected)
    {
        Assert.True(UpstreamUrl.TryParseId(url, out var id));
        Assert.Equal(expected, id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://stephen-king-api.onrender.com/api/book/abc")]
    [InlineData("https://stephen-king-api.onrender.com/api/book/-3")]
    [InlineData("https://stephen-king-api.onrender.com/api/book/0")]
    [InlineData("not-a-url")]
    public void TryParseId_UnusableUrl_Fails(string? url)
    {
        Assert.False(UpstreamUrl.TryParseId(url, out var id));
        Assert.Equal(0, id);
    }
}
