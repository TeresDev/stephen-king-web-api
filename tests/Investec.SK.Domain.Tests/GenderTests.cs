using Investec.SK.Domain;

namespace Investec.SK.Domain.Tests;

public class GenderParserTests
{
    [Theory]
    [InlineData("Male", Gender.Male)]
    [InlineData("male", Gender.Male)]
    [InlineData("  MALE  ", Gender.Male)]
    [InlineData("Female", Gender.Female)]
    [InlineData("female", Gender.Female)]
    [InlineData("Unknown", Gender.Unknown)]
    [InlineData("unknown", Gender.Unknown)]
    public void Parse_RecognisedValue_ReturnsMatchingGender(string raw, Gender expected) =>
        Assert.Equal(expected, GenderParser.Parse(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrBlank_ReturnsUnspecified(string? raw) =>
        Assert.Equal(Gender.Unspecified, GenderParser.Parse(raw));

    [Fact]
    public void Parse_UnrecognisedValue_ReturnsUnknownNotUnspecified() =>
        Assert.Equal(Gender.Unknown, GenderParser.Parse("Non-binary"));

    [Fact]
    public void Unspecified_AndUnknown_AreDistinctStates() =>
        Assert.NotEqual(GenderParser.Parse(null), GenderParser.Parse("Unknown"));
}
