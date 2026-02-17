using System.Collections.Generic;
using System.Linq;
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

    [Fact]
    public void Names_ShouldReturnDefaultAndFiltered()
    {
        Assert.Equal(new[] { "C", "D", "E", "F", "G", "A", "B" }, Note.Names().ToArray());
        var mixed = new object[] { "fx", "bb", 12, "nothing", new object(), null };
        Assert.Equal(new[] { "F##", "Bb" }, Note.Names(mixed).ToArray());
    }

    [Fact]
    public void SortedNames_ShouldSortAndPreserveDuplicates()
    {
        string[] tokens(string s) => s.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(new[] { "C", "F", "G", "A", "B" }, Note.SortedNames(tokens("c f g a b h j")).ToArray());

        Assert.Equal(
        new[] { "C", "C", "F", "F", "G", "G", "A", "A", "B", "B" },
        Note.SortedNames(tokens("c f g a b h j j h b a g f c")).ToArray());

        Assert.Equal(
        new[] { "C", "C0", "C1", "C2", "C5", "C6" },
        Note.SortedNames(tokens("c2 c5 c1 c0 c6 c")).ToArray());

        var descending = Comparer<string>.Create((a, b) => b.CompareTo(a));
        Assert.Equal(
        new[] { "C6", "C5", "C2", "C1", "C0", "C" },
        Note.SortedNames(tokens("c2 c5 c1 c0 c6 c"), descending).ToArray());
    }

    [Fact]
    public void SortedUniqNames_ShouldReturnUniqueSorted()
    {
        string[] tokens(string s) => s.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(new[] { "C", "A", "B", "C2", "C3" }, Note.SortedUniqNames(tokens("a b c2 1p p2 c2 b c c3")).ToArray());
    }

    [Fact]
    public void Transpose_Functions_ShouldWork()
    {
        Assert.Equal("C#5", Note.Transpose("A4", "3M"));

        var fromC4 = Note.TransposeFrom("C4");
        Assert.Equal("G4", fromC4("5P"));
        var res = new[] { "1P", "3M", "5P" }.Select(fromC4).ToArray();
        Assert.Equal(new[] { "C", "E", "G" }, res);

        var by5 = Note.TransposeBy("5P");
        Assert.Equal("G4", by5("C4"));
        var mapped = new[] { "C", "D", "E" }.Select(by5).ToArray();
        Assert.Equal(new[] { "G", "A", "B" }, mapped);
    }

    [Fact]
    public void Enharmonic_ShouldReturnExpected()
    {
        Assert.Equal("Db", Note.Enharmonic("C#"));
        Assert.Equal("D", Note.Enharmonic("C##"));
        Assert.Equal("Eb", Note.Enharmonic("C###"));
        Assert.Equal("C5", Note.Enharmonic("B#4"));

        var notes = new[] { "C##", "C###", "F##4", "Gbbb5", "B#4", "Cbb4" };
        var enh = notes.Select(n => Note.Enharmonic(n)).ToArray();
        Assert.Equal(new[] { "D", "Eb", "G4", "E5", "C5", "A#3" }, enh);

        Assert.Equal(string.Empty, Note.Enharmonic("x"));
        Assert.Equal("E#2", Note.Enharmonic("F2", "E#"));
        Assert.Equal("Cb3", Note.Enharmonic("B2", "Cb"));
        Assert.Equal("B#1", Note.Enharmonic("C2", "B#"));
        Assert.Equal(string.Empty, Note.Enharmonic("F2", "Eb"));
    }

    [Fact]
    public void TransposeFifths_ShouldWork()
    {
        Assert.Equal("E6", Note.TransposeFifths("G4", 3));
        Assert.Equal("E", Note.TransposeFifths("G", 3));

        var ns = new[] { 0, 1, 2, 3, 4, 5 }.Select(n => Note.TransposeFifths("C2", n)).ToArray();
        Assert.Equal(new[] { "C2", "G2", "D3", "A3", "E4", "B4" }, ns);

        var sharps = new[] { 0, 1, 2, 3, 4, 5, 6 }.Select(n => Note.TransposeFifths("F#", n)).ToArray();
        Assert.Equal(new[] { "F#", "C#", "G#", "D#", "A#", "E#", "B#" }, sharps);

        var flats = new[] { 0, -1, -2, -3, -4, -5, -6 }.Select(n => Note.TransposeFifths("Bb", n)).ToArray();
        Assert.Equal(new[] { "Bb", "Eb", "Ab", "Db", "Gb", "Cb", "Fb" }, flats);
    }

    [Fact]
    public void FromFreq_ShouldMapToNotes()
    {
        Assert.Equal("A4", Note.FromFreq(440));
        Assert.Equal("A4", Note.FromFreq(444));
        Assert.Equal("Bb4", Note.FromFreq(470));
        Assert.Equal("A#4", Note.FromFreqSharps(470));
        Assert.Equal(string.Empty, Note.FromFreq(0));
        Assert.Equal(string.Empty, Note.FromFreq(double.NaN));
    }
}
