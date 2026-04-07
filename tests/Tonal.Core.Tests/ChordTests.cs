using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Tonal.Core.Tests;

public class ChordTypeTests
{
    // -------------------------------------------------------------------------
    // Get — by name
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ByName_Major_ReturnsCorrectInfo()
    {
        var result = ChordType.Get("major");

        Assert.False(result.IsEmpty);
        Assert.Equal("major",          result.Name);
        Assert.Equal(ChordQuality.Major, result.Quality);
        Assert.Equal(3,                result.Length);
        Assert.Equal<IEnumerable<string>>(["1P", "3M", "5P"], result.Intervals);
        Assert.Contains("M",  result.Aliases);
        Assert.Contains("maj", result.Aliases);
    }

    [Fact]
    public void Get_ByName_MajorSeventh_ReturnsCorrectInfo()
    {
        var result = ChordType.Get("major seventh");

        Assert.False(result.IsEmpty);
        Assert.Equal("major seventh",    result.Name);
        Assert.Equal(ChordQuality.Major, result.Quality);
        Assert.Equal(4,                  result.Length);
        Assert.Contains("maj7", result.Aliases);
        Assert.Contains("M7",   result.Aliases);
    }

    [Fact]
    public void Get_ByName_Minor_ReturnsCorrectInfo()
    {
        var result = ChordType.Get("minor");

        Assert.False(result.IsEmpty);
        Assert.Equal("minor",            result.Name);
        Assert.Equal(ChordQuality.Minor, result.Quality);
        Assert.Equal(3,                  result.Length);
        Assert.Contains("m",   result.Aliases);
        Assert.Contains("min", result.Aliases);
    }

    [Fact]
    public void Get_ByName_Diminished_ReturnsCorrectInfo()
    {
        var result = ChordType.Get("diminished");

        Assert.False(result.IsEmpty);
        Assert.Equal(ChordQuality.Diminished, result.Quality);
        Assert.Contains("dim", result.Aliases);
    }

    [Fact]
    public void Get_ByName_Augmented_ReturnsCorrectInfo()
    {
        var result = ChordType.Get("augmented");

        Assert.False(result.IsEmpty);
        Assert.Equal(ChordQuality.Augmented, result.Quality);
        Assert.Contains("aug", result.Aliases);
    }

    [Fact]
    public void Get_ByName_SuspendedFourth_IsUnknownQuality()
    {
        var result = ChordType.Get("suspended fourth");
        Assert.Equal(ChordQuality.Unknown, result.Quality);
    }

    [Fact]
    public void Get_UnknownName_ReturnsEmpty()
    {
        Assert.True(ChordType.Get("not-a-chord").IsEmpty);
    }

    // -------------------------------------------------------------------------
    // Get — by alias
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("M",     "major")]
    [InlineData("maj",   "major")]
    [InlineData("maj7",  "major seventh")]
    [InlineData("m7",    "minor seventh")]
    [InlineData("7",     "dominant seventh")]
    [InlineData("dim",   "diminished")]
    [InlineData("dim7",  "diminished seventh")]
    [InlineData("aug",   "augmented")]
    [InlineData("sus4",  "suspended fourth")]
    [InlineData("sus2",  "suspended second")]
    [InlineData("m7b5",  "half-diminished")]
    [InlineData("ø",     "half-diminished")]
    [InlineData("9",     "dominant ninth")]
    [InlineData("Δ",     "major seventh")]
    public void Get_ByAlias_ResolvesToCorrectCanonicalName(string alias, string expectedName)
    {
        var result = ChordType.Get(alias);
        Assert.False(result.IsEmpty, $"alias '{alias}' should resolve");
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void Get_ByAlias_MatchesByName()
    {
        var byName  = ChordType.Get("major seventh");
        var byAlias = ChordType.Get("maj7");

        Assert.Equal(byName.Name,   byAlias.Name);
        Assert.Equal(byName.Chroma, byAlias.Chroma);
        Assert.Equal(byName.SetNum, byAlias.SetNum);
    }

    // -------------------------------------------------------------------------
    // Get — by chroma and set number
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ByChroma_ReturnsSameAsName()
    {
        var byName   = ChordType.Get("major");
        var byChroma = ChordType.Get(byName.Chroma);

        Assert.False(byChroma.IsEmpty);
        Assert.Equal(byName.Name, byChroma.Name);
    }

    [Fact]
    public void Get_BySetNum_ReturnsSameAsName()
    {
        var byName   = ChordType.Get("major seventh");
        var bySetNum = ChordType.Get(byName.SetNum);

        Assert.Equal(byName.Name, bySetNum.Name);
    }

    // -------------------------------------------------------------------------
    // Quality derivation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("major",            ChordQuality.Major)]
    [InlineData("major seventh",    ChordQuality.Major)]
    [InlineData("sixth",            ChordQuality.Major)]
    [InlineData("minor",            ChordQuality.Minor)]
    [InlineData("minor seventh",    ChordQuality.Minor)]
    [InlineData("diminished",       ChordQuality.Diminished)]
    [InlineData("diminished seventh", ChordQuality.Diminished)]
    [InlineData("half-diminished",  ChordQuality.Diminished)]
    [InlineData("augmented",        ChordQuality.Augmented)]
    [InlineData("augmented seventh",ChordQuality.Augmented)]
    [InlineData("minor augmented",  ChordQuality.Augmented)]
    [InlineData("dominant seventh", ChordQuality.Major)]
    [InlineData("suspended fourth", ChordQuality.Unknown)]
    [InlineData("suspended second", ChordQuality.Unknown)]
    [InlineData("fifth",            ChordQuality.Unknown)]
    public void Quality_IsCorrectForWellKnownChords(string name, ChordQuality expected)
    {
        Assert.Equal(expected, ChordType.Get(name).Quality);
    }

    // -------------------------------------------------------------------------
    // Names / All
    // -------------------------------------------------------------------------

    [Fact]
    public void Names_ContainsNamedChords()
    {
        var names = ChordType.Names();
        Assert.Contains("major",            names);
        Assert.Contains("minor",            names);
        Assert.Contains("dominant seventh", names);
        Assert.Contains("diminished",       names);
    }

    [Fact]
    public void Names_DoesNotContainAliases()
    {
        var names = ChordType.Names();
        Assert.DoesNotContain("maj7",  names);
        Assert.DoesNotContain("m7",    names);
        Assert.DoesNotContain("7",     names);
    }

    [Fact]
    public void All_ContainsMoreEntriesThanNames()
    {
        // All() includes unnamed legacy chords; Names() only returns named ones
        Assert.True(ChordType.All().Count > ChordType.Names().Count);
    }

    [Fact]
    public void All_ContainsNoEmptyIntervalEntries()
    {
        Assert.All(ChordType.All(), ct => Assert.NotEmpty(ct.Intervals));
    }
}

public class ChordTests
{
    // -------------------------------------------------------------------------
    // Tokenize
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Cmaj7",    "C",  "maj7", "")]
    [InlineData("Dm7",      "D",  "m7",   "")]
    [InlineData("Cmaj7/E",  "C",  "maj7", "E")]
    [InlineData("Cm/Eb",    "C",  "m",    "Eb")]
    [InlineData("C",        "C",  "",     "")]
    [InlineData("maj7",     "",   "maj7", "")]
    [InlineData("",         "",   "",     "")]
    public void Tokenize_ParsesSymbolCorrectly(
        string symbol, string expectedTonic, string expectedType, string expectedBass)
    {
        var (tonic, type, bass) = Chord.Tokenize(symbol);
        Assert.Equal(expectedTonic, tonic);
        Assert.Equal(expectedType,  type);
        Assert.Equal(expectedBass,  bass);
    }

    // -------------------------------------------------------------------------
    // Get — basic properties
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_CMajor_ReturnsCorrectInfo()
    {
        var chord = Chord.Get("C");

        Assert.False(chord.IsEmpty);
        Assert.Equal("C major", chord.Name);
        Assert.Equal("major",   chord.Type);
        Assert.Equal("C",       chord.Tonic);
        Assert.Null(chord.Bass);
        Assert.Equal(1, chord.RootDegree);
        Assert.Equal(3, chord.Length);
        Assert.Equal(ChordQuality.Major, chord.Quality);
    }

    [Fact]
    public void Get_Cmaj7_ReturnsCorrectInfo()
    {
        var chord = Chord.Get("Cmaj7");

        Assert.False(chord.IsEmpty);
        Assert.Equal("C major seventh", chord.Name);
        Assert.Equal("Cmaj7",           chord.Symbol);
        Assert.Equal("C",               chord.Tonic);
        Assert.Equal(4,                 chord.Length);
    }

    [Fact]
    public void Get_WithoutTonic_HasEmptyNotes()
    {
        var chord = Chord.Get("maj7");

        Assert.False(chord.IsEmpty);
        Assert.Null(chord.Tonic);
        Assert.Empty(chord.Notes);
        Assert.NotEmpty(chord.Intervals);
    }

    [Fact]
    public void Get_UnknownChordType_ReturnsEmpty()
    {
        Assert.True(Chord.Get("Cnotachord").IsEmpty);
    }

    // -------------------------------------------------------------------------
    // Get — slash chords (inversions)
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_SlashChord_Cmaj7OverE_HasCorrectBassAndRootDegree()
    {
        var chord = Chord.Get("Cmaj7/E");

        Assert.False(chord.IsEmpty);
        Assert.Equal("C",  chord.Tonic);
        Assert.Equal("E",  chord.Bass);
        Assert.Equal(2,    chord.RootDegree); // E is the 2nd note of Cmaj7
    }

    [Fact]
    public void Get_SlashChord_CmOverEb_HasCorrectRootDegree()
    {
        var chord = Chord.Get("Cm/Eb");

        Assert.Equal("Eb", chord.Bass);
        Assert.Equal(2,    chord.RootDegree); // Eb is the 2nd note of Cm
    }

    [Fact]
    public void Get_SlashChord_RootPosition_HasRootDegreeOne()
    {
        var chord = Chord.Get("Cmaj7/C");
        Assert.Equal(1, chord.RootDegree);
    }

    // -------------------------------------------------------------------------
    // Get — notes for well-known chords
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("C",      new[] { "C", "E", "G" })]
    [InlineData("Cmaj7",  new[] { "C", "E", "G", "B" })]
    [InlineData("Cm",     new[] { "C", "Eb", "G" })]
    [InlineData("Cm7",    new[] { "C", "Eb", "G", "Bb" })]
    [InlineData("Cdim",   new[] { "C", "Eb", "Gb" })]
    [InlineData("Caug",   new[] { "C", "E", "G#" })]
    [InlineData("C7",     new[] { "C", "E", "G", "Bb" })]
    [InlineData("Cmaj9",  new[] { "C", "E", "G", "B", "D" })]
    [InlineData("Csus4",  new[] { "C", "F", "G" })]
    [InlineData("Csus2",  new[] { "C", "D", "G" })]
    [InlineData("G",      new[] { "G", "B", "D" })]
    [InlineData("Dm7",    new[] { "D", "F", "A", "C" })]
    [InlineData("Fm",     new[] { "F", "Ab", "C" })]
    [InlineData("Bbmaj7", new[] { "Bb", "D", "F", "A" })]
    public void Get_WellKnownChords_HaveCorrectNotes(string symbol, string[] expectedNotes)
    {
        var chord = Chord.Get(symbol);
        Assert.False(chord.IsEmpty, $"'{symbol}' should be found");
        Assert.Equal<IEnumerable<string>>(expectedNotes, chord.Notes);
    }

    [Fact]
    public void Get_Cdim7_HasCorrectDiminishedSeventhSpelling()
    {
        // The diminished seventh (7d) above C is Bbb (B double-flat) — not A
        var chord = Chord.Get("Cdim7");
        Assert.Equal("C",   chord.Notes[0]);
        Assert.Equal("Eb",  chord.Notes[1]);
        Assert.Equal("Gb",  chord.Notes[2]);
        Assert.Equal("Bbb", chord.Notes[3]);
    }

    [Fact]
    public void Get_ChalfDim_HasCorrectSpelling()
    {
        // m7b5: C Eb Gb Bb
        var chord = Chord.Get("Cm7b5");
        Assert.Equal<IEnumerable<string>>(["C", "Eb", "Gb", "Bb"], chord.Notes);
    }

    // -------------------------------------------------------------------------
    // Transpose
    // -------------------------------------------------------------------------

    [Fact]
    public void Transpose_ByMajorSecond_RaisesChordByStep()
    {
        Assert.Equal("Dmaj7", Chord.Transpose("Cmaj7", "2M"));
        Assert.Equal("Dm7",   Chord.Transpose("Cm7",   "2M"));
        Assert.Equal("D7",    Chord.Transpose("C7",    "2M"));
    }

    [Fact]
    public void Transpose_ByPerfectFifth_RaisesChordFive()
    {
        Assert.Equal("G", Chord.Transpose("C", "5P"));
    }

    [Fact]
    public void Transpose_SlashChord_TransposesBothTonicAndBass()
    {
        Assert.Equal("Dmaj7/F#", Chord.Transpose("Cmaj7/E", "2M"));
    }

    [Fact]
    public void Transpose_NoTonic_ReturnsOriginal()
    {
        // Can't transpose a type-only chord symbol
        Assert.Equal("maj7", Chord.Transpose("maj7", "2M"));
    }

    // -------------------------------------------------------------------------
    // Degrees
    // -------------------------------------------------------------------------

    [Fact]
    public void Degrees_Cm_MapsInversions()
    {
        var triad = Chord.Degrees("Cm");

        // Root position
        Assert.Equal("C",  triad(1));
        Assert.Equal("Eb", triad(2));
        Assert.Equal("G",  triad(3));

        // First inversion (start from Eb)
        Assert.Equal<IEnumerable<string>>(["Eb", "G", "C"],
            new[] { 2, 3, 1 }.Select(triad).ToList());

        // Second inversion (start from G)
        Assert.Equal<IEnumerable<string>>(["G", "C", "Eb"],
            new[] { 3, 1, 2 }.Select(triad).ToList());
    }

    [Fact]
    public void Degrees_ZeroDegree_ReturnsEmpty()
    {
        var triad = Chord.Degrees("C");
        Assert.Equal("", triad(0));
    }

    [Fact]
    public void Degrees_BeyondChordLength_WrapsAround()
    {
        var triad = Chord.Degrees("C");
        Assert.Equal("C", triad(4)); // wraps back to root
        Assert.Equal("E", triad(5));
    }

    [Fact]
    public void Degrees_NoTonic_ReturnsEmpty()
    {
        var fn = Chord.Degrees("maj7");
        Assert.Equal("", fn(1));
    }

    // -------------------------------------------------------------------------
    // Steps
    // -------------------------------------------------------------------------

    [Fact]
    public void Steps_CMajor_MapsZeroBased()
    {
        var step = Chord.Steps("C");
        Assert.Equal("C", step(0));
        Assert.Equal("E", step(1));
        Assert.Equal("G", step(2));
    }

    // -------------------------------------------------------------------------
    // ChordScales
    // -------------------------------------------------------------------------

    [Fact]
    public void ChordScales_CMajorTriad_ContainsMajorAndOthers()
    {
        var scales = Chord.ChordScales("C");
        Assert.Contains("major",           scales);
        Assert.Contains("major pentatonic", scales);
        Assert.Contains("ionian pentatonic", scales);
    }

    [Fact]
    public void ChordScales_Cmaj7_ContainsLydian()
    {
        var scales = Chord.ChordScales("Cmaj7");
        Assert.Contains("major",  scales);
        Assert.Contains("lydian", scales);
    }

    [Fact]
    public void ChordScales_InvalidChord_ReturnsEmpty()
    {
        Assert.Empty(Chord.ChordScales("Cnotachord"));
    }

    // -------------------------------------------------------------------------
    // Extended / Reduced
    // -------------------------------------------------------------------------

    [Fact]
    public void Extended_CMajor_ContainsLargerChords()
    {
        var extended = Chord.Extended("C");
        // A major triad can be extended to major 7th, 9th, etc.
        Assert.NotEmpty(extended);
        // All results should start with C
        Assert.All(extended, name => Assert.StartsWith("C", name));
    }

    [Fact]
    public void Reduced_Cmaj7_ContainsMajorTriad()
    {
        var reduced = Chord.Reduced("Cmaj7");
        Assert.NotEmpty(reduced);
        Assert.All(reduced, name => Assert.StartsWith("C", name));
    }

    [Fact]
    public void Extended_And_Reduced_AreInverseRelations()
    {
        var reducedOfMaj7   = Chord.Reduced("Cmaj7");
        var extendedOfMaj   = Chord.Extended("C");

        // If C is a subset of Cmaj7, then Cmaj7's aliases should appear in C's extended list
        Assert.NotEmpty(reducedOfMaj7);
        Assert.NotEmpty(extendedOfMaj);
    }

    // -------------------------------------------------------------------------
    // Names
    // -------------------------------------------------------------------------

    [Fact]
    public void Names_ContainsCommonChords()
    {
        var names = Chord.Names();
        Assert.Contains("major",            names);
        Assert.Contains("minor",            names);
        Assert.Contains("dominant seventh", names);
        Assert.Contains("diminished",       names);
    }
}
