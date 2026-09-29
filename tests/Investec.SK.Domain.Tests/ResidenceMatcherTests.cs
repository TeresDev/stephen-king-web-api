using Investec.SK.Domain;

namespace Investec.SK.Domain.Tests;

public class ResidenceMatcherNormaliseTests
{
    [Theory]
    [InlineData("Residents of Maine", "maine")]
    [InlineData("Derry residents", "derry")]
    [InlineData("Derry", "derry")]
    [InlineData("Dery residents", "dery")]
    [InlineData("  DERRY   RESIDENTS  ", "derry")]
    [InlineData("Jerusalem's Lot residents", "jerusalem's lot")]
    [InlineData("Pownal resident", "pownal")]
    public void Normalise_StripsAffixesCollapsesWhitespaceAndLowercases(string raw, string expected) =>
        Assert.Equal(expected, ResidenceMatcher.Normalise(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Normalise_NullOrBlank_ReturnsEmpty(string? raw) =>
        Assert.Equal(string.Empty, ResidenceMatcher.Normalise(raw));
}

public class ResidenceMatcherMaineTests
{
    [Theory]
    [InlineData("Residents of Maine")]
    [InlineData("residents of maine")]
    [InlineData("  Residents of Maine  ")]
    [InlineData("Maine residents")]
    [InlineData("Maine")]
    public void IsMaine_MaineVariants_ReturnsTrue(string raw) =>
        Assert.True(ResidenceMatcher.IsMaine(raw));

    [Theory]
    [InlineData("Derry residents")]
    [InlineData("Yarmouth residents")]
    [InlineData("Villains")]
    [InlineData("Maine Coon")]
    [InlineData(null)]
    [InlineData("")]
    public void IsMaine_NonMaineNote_ReturnsFalse(string? raw) =>
        Assert.False(ResidenceMatcher.IsMaine(raw));
}

public class ResidenceMatcherDerryTests
{
    [Theory]
    [InlineData("Derry residents")]
    [InlineData("Derry")]
    [InlineData("derry")]
    [InlineData("  DERRY RESIDENTS  ")]
    [InlineData("Residents of Derry")]
    public void IsDerry_CanonicalSpelling_ReturnsTrue(string raw) =>
        Assert.True(ResidenceMatcher.IsDerry(raw));

    // "Dery" never appears in the live feed. These synthetic cases are the only proof
    // that the tolerant branch works, which is exactly why they exist.
    [Theory]
    [InlineData("Dery residents")]
    [InlineData("Dery")]
    [InlineData("dery")]
    [InlineData("Residents of Dery")]
    public void IsDerry_MisspelledVariant_ReturnsTrue(string raw) =>
        Assert.True(ResidenceMatcher.IsDerry(raw));

    [Theory]
    [InlineData("Derry Township residents")]
    [InlineData("Yarmouth residents")]
    [InlineData("Residents of Maine")]
    [InlineData("Dairy")]
    [InlineData(null)]
    [InlineData("")]
    public void IsDerry_NonDerryNote_ReturnsFalse(string? raw) =>
        Assert.False(ResidenceMatcher.IsDerry(raw));
}
