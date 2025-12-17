using Xunit;

namespace Tonal.Core.Tests;

public class NoteTests
{
    [Theory]
    [InlineData("C4", 60)]
    [InlineData("A4", 69)]
    [InlineData("C#4", 61)]
    [InlineData("Db4", 61)]
    [InlineData("G5", 79)]
    public void Midi_ShouldReturnExpectedValues(string note, int expected)
    {
        Assert.Equal(expected, Note.Midi(note));
    }

    [Theory]
    [InlineData("A4", 440.0)]
    [InlineData("C4", 261.63, 0.5)]
    public void Frequency_ShouldReturnExpectedValues(string note, double expected, double tolerance = 0.1)
    {
        var freq = Note.Freq(note);
        Assert.NotNull(freq);
        Assert.InRange(freq.Value, expected - tolerance, expected + tolerance);
    }

    [Fact]
    public void Accidentals_ShouldExtractSharpOrFlat()
    {
        Assert.Equal("#", Note.Accidentals("C#4"));
        Assert.Equal("b", Note.Accidentals("Db3"));
        Assert.Equal("", Note.Accidentals("E4"));
    }

    [Fact]
    public void Get_NoteDetails_ShouldReturnCorrectDetails()
    {
        var details = Note.Get("C4");
        Assert.Equal("C4", details.Name);
        Assert.Equal("C", details.PitchClass);
        Assert.Equal("C", details.Letter);
        Assert.Equal(0, details.Step);
        Assert.Equal("", details.Accidentals);
        Assert.Equal(0, details.Alteration);
        Assert.Equal(4, details.Octave);
        Assert.Equal(60, details.Midi);
        Assert.InRange(details.Frequency.Value, 261.0, 262.0);
        Assert.Equal(0, details.Chroma);
    }

    [Theory]
    [InlineData("C#", "C#")]
    [InlineData("C##", "D")]
    [InlineData("C###", "D#")]
    [InlineData("B#4", "C5")]
    [InlineData("C## ", "D")]
    [InlineData("C### ", "D#")]
    [InlineData("F##4", "G4")]
    [InlineData("Gbbb5", "E5")]
    [InlineData("Cbb4", "Bb3")]
    [InlineData("x", "")]
    public void Simplify_ShouldReturnSimplifiedNoteNames(string input, string expected)
    {
        Assert.Equal(expected, Note.Simplify(input));
    }

    [Fact]
    public void FromMidi_ShouldReturnNoteName()
    {
        Assert.Equal("Bb4", Note.FromMidi(70));
    }

    [Theory]
    [InlineData(60, "C4")]
    [InlineData(61, "Db4")]
    [InlineData(62, "D4")]
    public void FromMidi_List_ShouldReturnFlatMappings(int midi, string expected)
    {
        Assert.Equal(expected, Note.FromMidi(midi));
    }

    [Theory]
    [InlineData(60, "C4")]
    [InlineData(61, "C#4")]
    [InlineData(62, "D4")]
    public void FromMidiSharps_List_ShouldReturnSharpMappings(int midi, string expected)
    {
        Assert.Equal(expected, Note.FromMidiSharps(midi));
    }

}
