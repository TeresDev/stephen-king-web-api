using Investec.SK.Domain;

namespace Investec.SK.Domain.Tests;

public class ResidenceNotesTests
{
    [Fact]
    public void From_NoteListContainingMaine_FlagsMaine()
    {
        var notes = ResidenceNotes.From(["Characters", "Residents of Maine", "Villains"]);

        Assert.True(notes.IsResidentOfMaine);
        Assert.False(notes.IsResidentOfDerry);
    }

    [Fact]
    public void From_NoteListContainingDerry_FlagsDerryAndKeepsOriginalSpelling()
    {
        var notes = ResidenceNotes.From(["Characters", "Derry residents"]);

        Assert.True(notes.IsResidentOfDerry);
        Assert.Equal("Derry residents", notes.MatchedDerryNote);
    }

    [Fact]
    public void From_MisspelledDerry_FlagsDerryAndEchoesTheMisspelling()
    {
        var notes = ResidenceNotes.From(["Dery residents"]);

        Assert.True(notes.IsResidentOfDerry);
        Assert.Equal("Dery residents", notes.MatchedDerryNote);
    }

    [Fact]
    public void From_VillainInBothPlaces_FlagsBoth()
    {
        var notes = ResidenceNotes.From(["Residents of Maine", "Derry residents"]);

        Assert.True(notes.IsResidentOfMaine);
        Assert.True(notes.IsResidentOfDerry);
    }

    [Fact]
    public void From_NoResidenceNotes_FlagsNeitherAndHasNoMatchedNote()
    {
        var notes = ResidenceNotes.From(["Villains", "Antagonists"]);

        Assert.False(notes.IsResidentOfMaine);
        Assert.False(notes.IsResidentOfDerry);
        Assert.Null(notes.MatchedDerryNote);
    }

    [Fact]
    public void From_Null_ProducesEmptyNotes()
    {
        var notes = ResidenceNotes.From(null);

        Assert.Empty(notes.Raw);
        Assert.False(notes.IsResidentOfMaine);
        Assert.False(notes.IsResidentOfDerry);
    }

    [Fact]
    public void From_ListWithNullAndBlankEntries_IgnoresThem()
    {
        var notes = ResidenceNotes.From(["", null, "   ", "Derry"]);

        Assert.True(notes.IsResidentOfDerry);
        Assert.Single(notes.Raw);
    }
}
