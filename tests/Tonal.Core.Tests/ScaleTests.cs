using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tonal.Core.Tests;

public class ScaleTests
{
    // -------------------------------------------------------------------------
    // Tokenize
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("C mixolydian",  "C",  "mixolydian")]
    [InlineData("D dorian",      "D",  "dorian")]
    [InlineData("F# major",      "F#", "major")]
    [InlineData("Bb minor",      "Bb", "minor")]
    [InlineData("major",         "",   "major")]
    [InlineData("",              "",   "")]
    public void Tokenize_SplitsTonicAndType(string input, string expectedTonic, string expectedType)
    {
        var (tonic, type) = Scale.Tokenize(input);
        Assert.Equal(expectedTonic, tonic);
        Assert.Equal(expectedType,  type);
    }

    // -------------------------------------------------------------------------
    // Get — basic properties
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_CMajor_ReturnsCorrectInfo()
    {
        var scale = Scale.Get("C major");

        Assert.False(scale.IsEmpty);
        Assert.Equal("C major", scale.Name);
        Assert.Equal("major",   scale.Type);
        Assert.Equal("C",       scale.Tonic);
        Assert.Equal(7,         scale.Length);
        Assert.Equal("101011010101", scale.Chroma);
    }

    [Fact]
    public void Get_CMajor_HasCorrectNotes()
    {
        var scale = Scale.Get("C major");
        Assert.Equal<IEnumerable<string>>(["C", "D", "E", "F", "G", "A", "B"], scale.Notes);
    }

    [Fact]
    public void Get_CMajor_HasCorrectIntervals()
    {
        var scale = Scale.Get("C major");
        Assert.Equal<IEnumerable<string>>(
            ["1P", "2M", "3M", "4P", "5P", "6M", "7M"],
            scale.Intervals);
    }

    [Fact]
    public void Get_WithoutTonic_HasEmptyNotes()
    {
        var scale = Scale.Get("major");

        Assert.False(scale.IsEmpty);
        Assert.Equal("major", scale.Name);
        Assert.Null(scale.Tonic);
        Assert.Empty(scale.Notes);
        Assert.NotEmpty(scale.Intervals);
    }

    [Fact]
    public void Get_UnknownScaleType_ReturnsEmpty()
    {
        var scale = Scale.Get("C notascale");
        Assert.True(scale.IsEmpty);
    }

    [Fact]
    public void Get_ViaAlias_ReturnsCorrectScale()
    {
        var byName  = Scale.Get("C aeolian");
        var byAlias = Scale.Get("C minor");

        Assert.Equal(byName.Chroma, byAlias.Chroma);
        Assert.Equal(byName.Notes,  byAlias.Notes);
    }

    // -------------------------------------------------------------------------
    // Get — well-known scales and their notes
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("C major",          new[] { "C","D","E","F","G","A","B" })]
    [InlineData("C minor",          new[] { "C","D","Eb","F","G","Ab","Bb" })]
    [InlineData("C dorian",         new[] { "C","D","Eb","F","G","A","Bb" })]
    [InlineData("C phrygian",       new[] { "C","Db","Eb","F","G","Ab","Bb" })]
    [InlineData("C lydian",         new[] { "C","D","E","F#","G","A","B" })]
    [InlineData("C mixolydian",     new[] { "C","D","E","F","G","A","Bb" })]
    [InlineData("C locrian",        new[] { "C","Db","Eb","F","Gb","Ab","Bb" })]
    [InlineData("C major pentatonic", new[] { "C","D","E","G","A" })]
    [InlineData("C minor pentatonic", new[] { "C","Eb","F","G","Bb" })]
    [InlineData("D major",          new[] { "D","E","F#","G","A","B","C#" })]
    [InlineData("G major",          new[] { "G","A","B","C","D","E","F#" })]
    [InlineData("F major",          new[] { "F","G","A","Bb","C","D","E" })]
    [InlineData("Bb major",         new[] { "Bb","C","D","Eb","F","G","A" })]
    public void Get_WellKnownScales_HaveCorrectNotes(string scaleName, string[] expectedNotes)
    {
        var scale = Scale.Get(scaleName);
        Assert.False(scale.IsEmpty, $"'{scaleName}' should be found");
        Assert.Equal<IEnumerable<string>>(expectedNotes, scale.Notes);
    }

    // -------------------------------------------------------------------------
    // Get — via tokens
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ViaTokens_MatchesStringVersion()
    {
        var byString = Scale.Get("C major");
        var byTokens = Scale.Get("C", "major");

        Assert.Equal(byString.Name,   byTokens.Name);
        Assert.Equal(byString.Chroma, byTokens.Chroma);
        Assert.Equal<IEnumerable<string>>(byString.Notes, byTokens.Notes);
    }

    // -------------------------------------------------------------------------
    // Names
    // -------------------------------------------------------------------------

    [Fact]
    public void Names_ContainsExpectedScales()
    {
        var names = Scale.Names();
        Assert.Contains("major",   names);
        Assert.Contains("minor",   names);
        Assert.Contains("dorian",  names);
        Assert.Contains("chromatic", names);
    }

    [Fact]
    public void Names_DoesNotContainAliases()
    {
        var names = Scale.Names();
        Assert.DoesNotContain("ionian",  names);
        Assert.DoesNotContain("aeolian", names);
    }

    // -------------------------------------------------------------------------
    // Detect
    // -------------------------------------------------------------------------

    [Fact]
    public void Detect_CMajorNotes_FindsCMajor()
    {
        var results = Scale.Detect(["C", "D", "E", "F", "G", "A", "B"]);
        Assert.Contains("C major", results);
    }

    [Fact]
    public void Detect_ExactMatch_ReturnsOnlyExact()
    {
        var results = Scale.Detect(["C", "D", "E", "F", "G", "A", "B"], matchExact: true);
        Assert.Single(results);
        Assert.Equal("C major", results[0]);
    }

    [Fact]
    public void Detect_WithExplicitTonic_UsesProvidedTonic()
    {
        var results = Scale.Detect(["C", "D", "E", "F", "G", "A", "B"], tonic: "C", matchExact: true);
        Assert.Contains("C major", results);
    }

    [Fact]
    public void Detect_EmptyNotes_ReturnsEmpty()
    {
        var results = Scale.Detect([]);
        Assert.Empty(results);
    }

    // -------------------------------------------------------------------------
    // Extended
    // -------------------------------------------------------------------------

    [Fact]
    public void Extended_Major_ContainsBebopAndChromatic()
    {
        var extended = Scale.Extended("major");
        Assert.Contains("bebop",       extended);
        Assert.Contains("bebop major", extended);
        Assert.Contains("chromatic",   extended);
    }

    [Fact]
    public void Extended_Major_DoesNotContainMajorItself()
    {
        var extended = Scale.Extended("major");
        Assert.DoesNotContain("major", extended);
    }

    [Fact]
    public void Extended_InvalidScale_ReturnsEmpty()
    {
        Assert.Empty(Scale.Extended("not-a-scale"));
    }

    // -------------------------------------------------------------------------
    // Reduced
    // -------------------------------------------------------------------------

    [Fact]
    public void Reduced_Major_ContainsMajorPentatonic()
    {
        var reduced = Scale.Reduced("major");
        Assert.Contains("major pentatonic", reduced);
        Assert.Contains("ionian pentatonic", reduced);
    }

    [Fact]
    public void Reduced_Major_DoesNotContainMajorItself()
    {
        var reduced = Scale.Reduced("major");
        Assert.DoesNotContain("major", reduced);
    }

    [Fact]
    public void Extended_And_Reduced_AreInverseRelations()
    {
        // If pentatonic is a subset of major, then major should be in pentatonic's extended list
        var reducedOfMajor    = Scale.Reduced("major");
        var extendedOfPenta   = Scale.Extended("major pentatonic");

        Assert.Contains("major pentatonic", reducedOfMajor);
        Assert.Contains("major", extendedOfPenta);
    }

    // -------------------------------------------------------------------------
    // ModeNames
    // -------------------------------------------------------------------------

    [Fact]
    public void ModeNames_CMajor_ReturnsAllSevenModes()
    {
        var modes = Scale.ModeNames("C major");

        Assert.Equal(7, modes.Count);
        Assert.Contains(("C", "major"),      modes);
        Assert.Contains(("D", "dorian"),     modes);
        Assert.Contains(("E", "phrygian"),   modes);
        Assert.Contains(("F", "lydian"),     modes);
        Assert.Contains(("G", "mixolydian"), modes);
        Assert.Contains(("A", "minor"),      modes);
        Assert.Contains(("B", "locrian"),    modes);
    }

    [Fact]
    public void ModeNames_CPentatonic_ReturnsNamedPentaModes()
    {
        var modes = Scale.ModeNames("C pentatonic");

        Assert.Contains(("C", "major pentatonic"), modes);
        Assert.Contains(("D", "egyptian"),         modes);
        Assert.Contains(("E", "malkos raga"),      modes);
        Assert.Contains(("G", "ritusen"),          modes);
        Assert.Contains(("A", "minor pentatonic"), modes);
    }

    [Fact]
    public void ModeNames_EmptyScale_ReturnsEmpty()
    {
        Assert.Empty(Scale.ModeNames("C not-a-scale"));
    }

    // -------------------------------------------------------------------------
    // ScaleNotes
    // -------------------------------------------------------------------------

    [Fact]
    public void ScaleNotes_RotatesToStartFromFirstNote()
    {
        var notes = Scale.ScaleNotes(["D4", "C#5", "A5", "F#6"]);
        Assert.Equal<IEnumerable<string>>(["D", "F#", "A", "C#"], notes);
    }

    [Fact]
    public void ScaleNotes_DeduplicatesOctaves()
    {
        var notes = Scale.ScaleNotes(["C4", "C3", "C5", "C4"]);
        Assert.Single(notes);
        Assert.Equal("C", notes[0]);
    }

    [Fact]
    public void ScaleNotes_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(Scale.ScaleNotes([]));
    }

    // -------------------------------------------------------------------------
    // Degrees
    // -------------------------------------------------------------------------

    [Fact]
    public void Degrees_CMajor_MapsPositiveDegrees()
    {
        var degree = Scale.Degrees("C major");

        Assert.Equal("C", degree(1));
        Assert.Equal("D", degree(2));
        Assert.Equal("E", degree(3));
        Assert.Equal("F", degree(4));
        Assert.Equal("G", degree(5));
        Assert.Equal("A", degree(6));
        Assert.Equal("B", degree(7));
    }

    [Fact]
    public void Degrees_ZeroDegree_ReturnsEmpty()
    {
        var degree = Scale.Degrees("C major");
        Assert.Equal("", degree(0));
    }

    [Fact]
    public void Degrees_BeyondOctave_WrapsAround()
    {
        var degree = Scale.Degrees("C major");
        // 8th degree wraps back to C (same pitch class, higher octave without octave info)
        Assert.Equal("C", degree(8));
    }

    [Fact]
    public void Degrees_WithOctave_PreservesOctaveOnWrap()
    {
        var degree = Scale.Degrees("C4 major");
        Assert.Equal("C4", degree(1));
        Assert.Equal("D4", degree(2));
        Assert.Equal("C5", degree(8));  // octave above
    }

    [Fact]
    public void Degrees_Functional_CanProjectSequence()
    {
        var degree = Scale.Degrees("C major");
        var notes  = new[] { 1, 2, 3, 4, 5 }.Select(degree).ToList();
        Assert.Equal<IEnumerable<string>>(["C", "D", "E", "F", "G"], notes);
    }

    // -------------------------------------------------------------------------
    // Steps
    // -------------------------------------------------------------------------

    [Fact]
    public void Steps_CMajor_MapsZeroBasedSteps()
    {
        var step = Scale.Steps("C major");

        Assert.Equal("C", step(0));
        Assert.Equal("D", step(1));
        Assert.Equal("E", step(2));
    }

    [Fact]
    public void Steps_Functional_CanProjectSequence()
    {
        var step  = Scale.Steps("C major");
        var notes = new[] { 0, 2, 4 }.Select(step).ToList();
        Assert.Equal<IEnumerable<string>>(["C", "E", "G"], notes);
    }

    // -------------------------------------------------------------------------
    // RangeOf
    // -------------------------------------------------------------------------

    [Fact]
    public void RangeOf_CMajor_C4ToC5_ReturnsOneOctave()
    {
        var range = Scale.RangeOf("C major", "C4", "C5");
        Assert.Equal<IEnumerable<string>>(
            ["C4", "D4", "E4", "F4", "G4", "A4", "B4", "C5"],
            range);
    }

    [Fact]
    public void RangeOf_CMajor_Descending_ReturnsNotesInOrder()
    {
        var range = Scale.RangeOf("C major", "C5", "C4");
        Assert.Equal<IEnumerable<string>>(
            ["C5", "B4", "A4", "G4", "F4", "E4", "D4", "C4"],
            range);
    }

    [Fact]
    public void RangeOf_WithoutTonic_ReturnsEmpty()
    {
        var range = Scale.RangeOf("major", "C4", "C5");
        Assert.Empty(range);
    }

    [Fact]
    public void RangeOf_InvalidNotes_ReturnsEmpty()
    {
        var range = Scale.RangeOf("C major", "not-a-note", "C5");
        Assert.Empty(range);
    }

    [Fact]
    public void RangeOf_UsesCorrectEnharmonicSpelling()
    {
        // Bb major range should use flats, not sharps
        var range = Scale.RangeOf("Bb major", "Bb3", "Bb4");
        Assert.DoesNotContain("A#4", range);
        Assert.Contains("Bb4", range);
        Assert.Contains("Eb4", range);
    }
}
