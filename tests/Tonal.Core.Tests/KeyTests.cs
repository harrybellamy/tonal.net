using System.Collections.Generic;

using Xunit;

namespace Tonal.Core.Tests;

public class KeyTests
{
    // -------------------------------------------------------------------------
    // MajorKey — basic properties
    // -------------------------------------------------------------------------

    [Fact]
    public void MajorKey_C_HasCorrectBaseProperties()
    {
        var key = Key.MajorKey("C");

        Assert.False(key.IsEmpty);
        Assert.Equal("C",  key.Tonic);
        Assert.Equal(0,    key.Alteration);
        Assert.Equal("",   key.KeySignature);
        Assert.Equal("A",  key.MinorRelative);
    }

    [Fact]
    public void MajorKey_G_HasOneSharps()
    {
        var key = Key.MajorKey("G");
        Assert.Equal(1,   key.Alteration);
        Assert.Equal("#", key.KeySignature);
        Assert.Equal("E", key.MinorRelative);
    }

    [Fact]
    public void MajorKey_F_HasOneFlat()
    {
        var key = Key.MajorKey("F");
        Assert.Equal(-1,  key.Alteration);
        Assert.Equal("b", key.KeySignature);
        Assert.Equal("D", key.MinorRelative);
    }

    [Fact]
    public void MajorKey_Bb_HasTwoFlats()
    {
        var key = Key.MajorKey("Bb");
        Assert.Equal(-2,   key.Alteration);
        Assert.Equal("bb", key.KeySignature);
        Assert.Equal("G",  key.MinorRelative);
    }

    [Fact]
    public void MajorKey_InvalidTonic_ReturnsEmpty()
    {
        Assert.True(Key.MajorKey("not-a-note").IsEmpty);
    }

    // -------------------------------------------------------------------------
    // MajorKey — key signatures for all 15 major keys
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Cb", -7, "bbbbbbb")]
    [InlineData("Gb", -6, "bbbbbb")]
    [InlineData("Db", -5, "bbbbb")]
    [InlineData("Ab", -4, "bbbb")]
    [InlineData("Eb", -3, "bbb")]
    [InlineData("Bb", -2, "bb")]
    [InlineData("F",  -1, "b")]
    [InlineData("C",   0, "")]
    [InlineData("G",   1, "#")]
    [InlineData("D",   2, "##")]
    [InlineData("A",   3, "###")]
    [InlineData("E",   4, "####")]
    [InlineData("B",   5, "#####")]
    [InlineData("F#",  6, "######")]
    [InlineData("C#",  7, "#######")]
    public void MajorKey_AllKeys_HaveCorrectSignature(
        string tonic, int expectedAlt, string expectedSig)
    {
        var key = Key.MajorKey(tonic);
        Assert.Equal(expectedAlt, key.Alteration);
        Assert.Equal(expectedSig, key.KeySignature);
    }

    // -------------------------------------------------------------------------
    // MajorKey — scale notes
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("C",  new[] { "C","D","E","F","G","A","B" })]
    [InlineData("G",  new[] { "G","A","B","C","D","E","F#" })]
    [InlineData("D",  new[] { "D","E","F#","G","A","B","C#" })]
    [InlineData("F",  new[] { "F","G","A","Bb","C","D","E" })]
    [InlineData("Bb", new[] { "Bb","C","D","Eb","F","G","A" })]
    public void MajorKey_Scale_HasCorrectNotes(string tonic, string[] expectedScale)
    {
        var key = Key.MajorKey(tonic);
        Assert.Equal<IEnumerable<string>>(expectedScale, key.Scale);
    }

    // -------------------------------------------------------------------------
    // MajorKey — chords and triads
    // -------------------------------------------------------------------------

    [Fact]
    public void MajorKey_C_HasCorrectSeventhChords()
    {
        var key = Key.MajorKey("C");
        Assert.Equal<IEnumerable<string>>(
            ["Cmaj7", "Dm7", "Em7", "Fmaj7", "G7", "Am7", "Bm7b5"],
            key.Chords);
    }

    [Fact]
    public void MajorKey_C_HasCorrectTriads()
    {
        var key = Key.MajorKey("C");
        Assert.Equal<IEnumerable<string>>(
            ["C", "Dm", "Em", "F", "G", "Am", "Bdim"],
            key.Triads);
    }

    [Fact]
    public void MajorKey_C_HasCorrectHarmonicFunctions()
    {
        var key = Key.MajorKey("C");
        Assert.Equal<IEnumerable<string>>(
            ["T", "SD", "T", "SD", "D", "T", "D"],
            key.ChordsHarmonicFunction);
    }

    [Fact]
    public void MajorKey_G_HasCorrectSeventhChords()
    {
        var key = Key.MajorKey("G");
        Assert.Equal<IEnumerable<string>>(
            ["Gmaj7", "Am7", "Bm7", "Cmaj7", "D7", "Em7", "F#m7b5"],
            key.Chords);
    }

    // -------------------------------------------------------------------------
    // MajorKey — chord scales
    // -------------------------------------------------------------------------

    [Fact]
    public void MajorKey_C_HasCorrectChordScales()
    {
        var key = Key.MajorKey("C");
        Assert.Equal<IEnumerable<string>>(
            ["C major","D dorian","E phrygian","F lydian","G mixolydian","A minor","B locrian"],
            key.ChordScales);
    }

    // -------------------------------------------------------------------------
    // MajorKey — secondary dominants
    // -------------------------------------------------------------------------

    [Fact]
    public void MajorKey_C_HasCorrectSecondaryDominants()
    {
        var key = Key.MajorKey("C");
        // V: G7 is already diatonic, VII: no P5 in scale -> both empty
        Assert.Equal<IEnumerable<string>>(
            ["C7", "D7", "E7", "F7", "", "A7", ""],
            key.SecondaryDominants);
    }

    [Fact]
    public void MajorKey_C_HasCorrectSubstituteDominants()
    {
        var key = Key.MajorKey("C");
        // Tritone substitution of each secondary dominant (down a dim 5th)
        // C7 -> Gb7, D7 -> Ab7, E7 -> Bb7, F7 -> B7, A7 -> Eb7
        Assert.Equal("Gb7",  key.SubstituteDominants[0]);
        Assert.Equal("Ab7",  key.SubstituteDominants[1]);
        Assert.Equal("Bb7",  key.SubstituteDominants[2]);
        Assert.Equal("B7",   key.SubstituteDominants[3]);
        Assert.Equal("",     key.SubstituteDominants[4]); // V has no secondary dominant
        Assert.Equal("Eb7",  key.SubstituteDominants[5]);
        Assert.Equal("",     key.SubstituteDominants[6]);
    }

    // -------------------------------------------------------------------------
    // MajorKey — relative minor
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("C",  "A")]
    [InlineData("G",  "E")]
    [InlineData("D",  "B")]
    [InlineData("A",  "F#")]
    [InlineData("F",  "D")]
    [InlineData("Bb", "G")]
    [InlineData("Eb", "C")]
    public void MajorKey_MinorRelative_IsCorrect(string tonic, string expectedRelative)
    {
        Assert.Equal(expectedRelative, Key.MajorKey(tonic).MinorRelative);
    }

    // -------------------------------------------------------------------------
    // MinorKey — basic properties
    // -------------------------------------------------------------------------

    [Fact]
    public void MinorKey_A_HasCorrectBaseProperties()
    {
        var key = Key.MinorKey("A");

        Assert.False(key.IsEmpty);
        Assert.Equal("A",  key.Tonic);
        Assert.Equal(0,    key.Alteration);
        Assert.Equal("",   key.KeySignature);
        Assert.Equal("C",  key.RelativeMajor);
    }

    [Fact]
    public void MinorKey_E_HasOneSharps()
    {
        var key = Key.MinorKey("E");
        Assert.Equal(1,   key.Alteration);
        Assert.Equal("#", key.KeySignature);
        Assert.Equal("G", key.RelativeMajor);
    }

    [Fact]
    public void MinorKey_D_HasOneFlat()
    {
        var key = Key.MinorKey("D");
        Assert.Equal(-1,  key.Alteration);
        Assert.Equal("b", key.KeySignature);
        Assert.Equal("F", key.RelativeMajor);
    }

    [Fact]
    public void MinorKey_InvalidTonic_ReturnsEmpty()
    {
        Assert.True(Key.MinorKey("not-a-note").IsEmpty);
    }

    // -------------------------------------------------------------------------
    // MinorKey — key signatures
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Ab", -7, "bbbbbbb")]
    [InlineData("Eb", -6, "bbbbbb")]
    [InlineData("Bb", -5, "bbbbb")]
    [InlineData("F",  -4, "bbbb")]
    [InlineData("C",  -3, "bbb")]
    [InlineData("G",  -2, "bb")]
    [InlineData("D",  -1, "b")]
    [InlineData("A",   0, "")]
    [InlineData("E",   1, "#")]
    [InlineData("B",   2, "##")]
    [InlineData("F#",  3, "###")]
    [InlineData("C#",  4, "####")]
    [InlineData("G#",  5, "#####")]
    [InlineData("D#",  6, "######")]
    [InlineData("A#",  7, "#######")]
    public void MinorKey_AllKeys_HaveCorrectSignature(
        string tonic, int expectedAlt, string expectedSig)
    {
        var key = Key.MinorKey(tonic);
        Assert.Equal(expectedAlt, key.Alteration);
        Assert.Equal(expectedSig, key.KeySignature);
    }

    // -------------------------------------------------------------------------
    // MinorKey — natural minor scale and chords
    // -------------------------------------------------------------------------

    [Fact]
    public void MinorKey_A_Natural_HasCorrectScale()
    {
        var key = Key.MinorKey("A");
        Assert.Equal<IEnumerable<string>>(
            ["A", "B", "C", "D", "E", "F", "G"],
            key.Natural.Scale);
    }

    [Fact]
    public void MinorKey_A_Natural_HasCorrectSeventhChords()
    {
        var key = Key.MinorKey("A");
        Assert.Equal<IEnumerable<string>>(
            ["Am7", "Bm7b5", "Cmaj7", "Dm7", "Em7", "Fmaj7", "G7"],
            key.Natural.Chords);
    }

    [Fact]
    public void MinorKey_A_Natural_HasCorrectTriads()
    {
        var key = Key.MinorKey("A");
        Assert.Equal<IEnumerable<string>>(
            ["Am", "Bdim", "C", "Dm", "Em", "F", "G"],
            key.Natural.Triads);
    }

    // -------------------------------------------------------------------------
    // MinorKey — harmonic minor scale and chords
    // -------------------------------------------------------------------------

    [Fact]
    public void MinorKey_A_Harmonic_HasCorrectScale()
    {
        var key = Key.MinorKey("A");
        // Harmonic minor: raised 7th (G# instead of G)
        Assert.Equal<IEnumerable<string>>(
            ["A", "B", "C", "D", "E", "F", "G#"],
            key.Harmonic.Scale);
    }

    [Fact]
    public void MinorKey_A_Harmonic_HasCorrectSeventhChords()
    {
        var key = Key.MinorKey("A");
        Assert.Equal<IEnumerable<string>>(
            ["AmMaj7", "Bm7b5", "C+maj7", "Dm7", "E7", "Fmaj7", "G#o7"],
            key.Harmonic.Chords);
    }

    [Fact]
    public void MinorKey_A_Harmonic_HasDominantSeventhOnV()
    {
        var key = Key.MinorKey("A");
        // The raised 7th creates a real dominant 7th chord on V (E7)
        Assert.Contains("E7", key.Harmonic.Chords);
    }

    // -------------------------------------------------------------------------
    // MinorKey — melodic minor scale and chords
    // -------------------------------------------------------------------------

    [Fact]
    public void MinorKey_A_Melodic_HasCorrectScale()
    {
        var key = Key.MinorKey("A");
        // Melodic minor: raised 6th and 7th (F# and G#)
        Assert.Equal<IEnumerable<string>>(
            ["A", "B", "C", "D", "E", "F#", "G#"],
            key.Melodic.Scale);
    }

    [Fact]
    public void MinorKey_A_Melodic_HasCorrectSeventhChords()
    {
        var key = Key.MinorKey("A");
        Assert.Equal<IEnumerable<string>>(
            ["Am6", "Bm7", "C+maj7", "D7", "E7", "F#m7b5", "G#m7b5"],
            key.Melodic.Chords);
    }

    // -------------------------------------------------------------------------
    // MinorKey — relative major
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("A",  "C")]
    [InlineData("E",  "G")]
    [InlineData("B",  "D")]
    [InlineData("F#", "A")]
    [InlineData("D",  "F")]
    [InlineData("G",  "Bb")]
    [InlineData("C",  "Eb")]
    public void MinorKey_RelativeMajor_IsCorrect(string tonic, string expectedRelative)
    {
        Assert.Equal(expectedRelative, Key.MinorKey(tonic).RelativeMajor);
    }

    // -------------------------------------------------------------------------
    // Parallel key relationship
    // -------------------------------------------------------------------------

    [Fact]
    public void ParallelKeys_ShareSameTonic()
    {
        var major = Key.MajorKey("C");
        var minor = Key.MinorKey("C");

        Assert.Equal(major.Tonic, minor.Tonic);
    }

    [Fact]
    public void ParallelKeys_HaveDifferentKeySignatures()
    {
        var major = Key.MajorKey("C");
        var minor = Key.MinorKey("C");

        Assert.NotEqual(major.KeySignature, minor.KeySignature);
        Assert.Equal("",    major.KeySignature); // C major: no accidentals
        Assert.Equal("bbb", minor.KeySignature); // C minor: 3 flats
    }

    // -------------------------------------------------------------------------
    // Relative key round-trips
    // -------------------------------------------------------------------------

    [Fact]
    public void MajorKey_RelativeMinor_RoundTrips()
    {
        var major = Key.MajorKey("C");
        var minor = Key.MinorKey(major.MinorRelative);

        Assert.Equal(major.Tonic, minor.RelativeMajor);
    }

    [Fact]
    public void MinorKey_RelativeMajor_RoundTrips()
    {
        var minor = Key.MinorKey("A");
        var major = Key.MajorKey(minor.RelativeMajor);

        Assert.Equal(minor.Tonic, major.MinorRelative);
    }

    // -------------------------------------------------------------------------
    // MajorTonicFromKeySignature
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("#",       "G")]
    [InlineData("##",      "D")]
    [InlineData("###",     "A")]
    [InlineData("####",    "E")]
    [InlineData("#####",   "B")]
    [InlineData("b",       "F")]
    [InlineData("bb",      "Bb")]
    [InlineData("bbb",     "Eb")]
    [InlineData("bbbb",    "Ab")]
    [InlineData("",        "C")]
    public void MajorTonicFromKeySignature_String_ReturnsCorrectTonic(
        string sig, string expectedTonic)
    {
        Assert.Equal(expectedTonic, Key.MajorTonicFromKeySignature(sig));
    }

    [Theory]
    [InlineData(0,   "C")]
    [InlineData(1,   "G")]
    [InlineData(2,   "D")]
    [InlineData(3,   "A")]
    [InlineData(-1,  "F")]
    [InlineData(-2,  "Bb")]
    [InlineData(-3,  "Eb")]
    public void MajorTonicFromKeySignature_Int_ReturnsCorrectTonic(
        int alt, string expectedTonic)
    {
        Assert.Equal(expectedTonic, Key.MajorTonicFromKeySignature(alt));
    }

    [Fact]
    public void MajorTonicFromKeySignature_InvalidString_ReturnsNull()
    {
        Assert.Null(Key.MajorTonicFromKeySignature("x#b"));
    }

    // -------------------------------------------------------------------------
    // Grades and intervals
    // -------------------------------------------------------------------------

    [Fact]
    public void MajorKey_C_HasCorrectGradesAndIntervals()
    {
        var key = Key.MajorKey("C");

        Assert.Equal<IEnumerable<string>>(
            ["I", "II", "III", "IV", "V", "VI", "VII"],
            key.Grades);

        Assert.Equal<IEnumerable<string>>(
            ["1P", "2M", "3M", "4P", "5P", "6M", "7M"],
            key.Intervals);
    }

    [Fact]
    public void MinorKey_A_Natural_HasCorrectGradesAndIntervals()
    {
        var key = Key.MinorKey("A");

        Assert.Equal<IEnumerable<string>>(
            ["I", "II", "bIII", "IV", "V", "bVI", "bVII"],
            key.Natural.Grades);

        Assert.Equal<IEnumerable<string>>(
            ["1P", "2M", "3m", "4P", "5P", "6m", "7m"],
            key.Natural.Intervals);
    }
}
