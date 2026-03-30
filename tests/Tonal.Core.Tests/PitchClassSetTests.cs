using System.Collections.Generic;

using Xunit;

namespace Tonal.Core.Tests;

public class PitchClassSetTests
{
    // -------------------------------------------------------------------------
    // Get — from note list
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_FromNotes_ReturnsCorrectChromaAndNum()
    {
        var result = PitchClassSet.Get(["C", "D", "E"]);

        Assert.Equal("101010000000", result.Chroma);
        Assert.Equal(2688, result.Num);
        Assert.Equal(3, result.Length);
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Get_FromNotes_IntervalsAreAlwaysFromC()
    {
        // Even when notes don't start from C, intervals are measured from C
        var fromC = PitchClassSet.Get(["C", "D", "E"]);
        var fromD = PitchClassSet.Get(["D", "F#", "A"]);

        Assert.Equal<IEnumerable<string>>(["1P", "2M", "3M"], fromC.Intervals);
        // D=2M, F#=4A (tritone from C, expressed as augmented 4th), A=6M
        Assert.Equal<IEnumerable<string>>(["2M", "5d", "6M"], fromD.Intervals);
    }

    [Fact]
    public void Get_FromNotes_WithOctaves_StripsOctaveInfo()
    {
        // Octave numbers should be ignored — D3 and D5 are the same pitch class
        var withOctaves = PitchClassSet.Get(["D3", "A3", "Bb3", "C4", "D4", "E4", "F4", "G4", "A4"]);
        var withoutOctaves = PitchClassSet.Get(["C", "D", "E", "F", "G", "A", "Bb"]);

        Assert.Equal(withoutOctaves.Chroma, withOctaves.Chroma);
    }

    [Fact]
    public void Get_FromNotes_DuplicatesAreIgnored()
    {
        var withDupes = PitchClassSet.Get(["C", "C", "E", "G"]);
        var withoutDupes = PitchClassSet.Get(["C", "E", "G"]);

        Assert.Equal(withoutDupes.Chroma, withDupes.Chroma);
        Assert.Equal(3, withDupes.Length);
    }

    [Fact]
    public void Get_EnharmonicEquivalentsProduceSameChroma()
    {
        // C# and Db are enharmonic — they share chroma position 1
        var withSharp = PitchClassSet.Get(["C", "C#", "E"]);
        var withFlat = PitchClassSet.Get(["C", "Db", "E"]);

        Assert.Equal(withSharp.Chroma, withFlat.Chroma);
        Assert.Equal(withSharp.Num, withFlat.Num);
    }

    // -------------------------------------------------------------------------
    // Get — from chroma string
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_FromChroma_RoundTrips()
    {
        var original = PitchClassSet.Get(["C", "D", "E"]);
        var fromChroma = PitchClassSet.Get(original.Chroma);

        Assert.Equal(original.Chroma, fromChroma.Chroma);
        Assert.Equal(original.Num, fromChroma.Num);
        Assert.Equal(original.Length, fromChroma.Length);
    }

    [Fact]
    public void Get_FromChroma_InvalidLength_ReturnsEmpty()
    {
        var result = PitchClassSet.Get("10101");
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Get_FromChroma_InvalidCharacters_ReturnsEmpty()
    {
        var result = PitchClassSet.Get("10101000X000");
        Assert.True(result.IsEmpty);
    }

    // -------------------------------------------------------------------------
    // Get — from number
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_FromNum_RoundTrips()
    {
        var original = PitchClassSet.Get(["C", "D", "E"]);
        var fromNum = PitchClassSet.Get(original.Num);

        Assert.Equal(original.Chroma, fromNum.Chroma);
    }

    [Fact]
    public void Get_FromNum_OutOfRange_ReturnsEmpty()
    {
        Assert.True(PitchClassSet.Get(-1).IsEmpty);
        Assert.True(PitchClassSet.Get(4096).IsEmpty);
    }

    [Fact]
    public void Get_AllThreeInputFormsAreEquivalent()
    {
        // The docs explicitly state all three return the same object
        var fromNotes  = PitchClassSet.Get(["C", "D", "E"]);
        var fromChroma = PitchClassSet.Get("101010000000");
        var fromNum    = PitchClassSet.Get(2688);

        Assert.Equal(fromNotes.Chroma, fromChroma.Chroma);
        Assert.Equal(fromNotes.Chroma, fromNum.Chroma);
        Assert.Equal(fromNotes.Num,    fromNum.Num);
    }

    // -------------------------------------------------------------------------
    // Well-known sets — spot checks
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(new[] { "C", "E", "G" },            "100010010000", 3)] // C major triad
    [InlineData(new[] { "C", "Eb", "G" },           "100100010000", 3)] // C minor triad
    [InlineData(new[] { "C", "E", "G", "B" },       "100010010001", 4)] // Cmaj7
    [InlineData(new[] { "C", "D", "E", "F", "G", "A", "B" }, "101011010101", 7)] // C major scale
    [InlineData(new[] { "C", "D", "Eb", "F", "G", "Ab", "Bb" }, "101101011010", 7)] // C natural minor
    public void Get_WellKnownSets_HaveCorrectChromaAndLength(
        string[] notes, string expectedChroma, int expectedLength)
    {
        var result = PitchClassSet.Get(notes);

        Assert.Equal(expectedChroma, result.Chroma);
        Assert.Equal(expectedLength, result.Length);
    }

    // -------------------------------------------------------------------------
    // Chroma shorthand
    // -------------------------------------------------------------------------

    [Fact]
    public void Chroma_FromNotes_MatchesGetChroma()
    {
        Assert.Equal("101010000000", PitchClassSet.Chroma(["C", "D", "E"]));
    }

    [Fact]
    public void Chroma_FromNum_MatchesExpected()
    {
        Assert.Equal("101010000000", PitchClassSet.Chroma(2688));
    }

    // -------------------------------------------------------------------------
    // Num shorthand
    // -------------------------------------------------------------------------

    [Fact]
    public void Num_FromNotes_MatchesExpected()
    {
        Assert.Equal(2688, PitchClassSet.Num(["C", "D", "E"]));
    }

    [Fact]
    public void Num_FromChroma_MatchesExpected()
    {
        Assert.Equal(2688, PitchClassSet.Num("101010000000"));
    }

    // -------------------------------------------------------------------------
    // Intervals shorthand
    // -------------------------------------------------------------------------

    [Fact]
    public void Intervals_FromNotes_AreAlwaysFromC()
    {
        Assert.Equal<IEnumerable<string>>(["1P", "2M", "3M"], PitchClassSet.Intervals(["C", "D", "E"]));
    }

    [Fact]
    public void Intervals_NonCRoot_StillMeasuredFromC()
    {
        // D major triad: D(2M), F#(tritone=5d from C), A(6M)
        Assert.Equal<IEnumerable<string>>(["2M", "5d", "6M"], PitchClassSet.Intervals(["D", "F#", "A"]));
    }

    [Fact]
    public void Intervals_MajorScale_HasCorrectDiatonicIntervals()
    {
        var intervals = PitchClassSet.Intervals(["C", "D", "E", "F", "G", "A", "B"]);
        Assert.Equal<IEnumerable<string>>(["1P", "2M", "3M", "4P", "5P", "6M", "7M"], intervals);
    }

    // -------------------------------------------------------------------------
    // Notes
    // -------------------------------------------------------------------------

    [Fact]
    public void Notes_FromNotes_ReturnsSortedPitchClasses()
    {
        var result = PitchClassSet.Notes(["D3", "A3", "Bb3", "C4", "D4", "E4", "F4", "G4", "A4"]);
        Assert.Equal<IEnumerable<string>>(["C", "D", "E", "F", "G", "A", "A#"], result);
    }

    [Fact]
    public void Notes_FromChroma_ReturnsSortedPitchClasses()
    {
        var result = PitchClassSet.Notes("101011010110");
        Assert.Equal<IEnumerable<string>>(["C", "D", "E", "F", "G", "A", "A#"], result);
    }

    [Fact]
    public void Notes_AlwaysStartFromC()
    {
        // Even if no C is in the original notes, output starts from lowest present chroma
        var result = PitchClassSet.Notes(["D", "F#", "A"]);
        Assert.Equal<IEnumerable<string>>(["D", "F#", "A"], result);
    }

    // -------------------------------------------------------------------------
    // IsIncludedIn
    // -------------------------------------------------------------------------

    [Fact]
    public void IsIncludedIn_NoteInSet_ReturnsTrue()
    {
        var inCTriad = PitchClassSet.IsIncludedIn(["C", "E", "G"]);

        Assert.True(inCTriad("C4"));
        Assert.True(inCTriad("E2"));
        Assert.True(inCTriad("G7"));
    }

    [Fact]
    public void IsIncludedIn_NoteNotInSet_ReturnsFalse()
    {
        var inCTriad = PitchClassSet.IsIncludedIn(["C", "E", "G"]);

        Assert.False(inCTriad("C#4"));
        Assert.False(inCTriad("D"));
        Assert.False(inCTriad("F"));
    }

    [Fact]
    public void IsIncludedIn_EnharmonicEquivalent_ReturnsTrue()
    {
        // Fb is enharmonic with E — chroma 4 — so it is in the C major triad
        var inCTriad = PitchClassSet.IsIncludedIn(["C", "E", "G"]);

        Assert.True(inCTriad("Fb"));
        Assert.True(inCTriad("B#")); // enharmonic of C
    }

    [Fact]
    public void IsIncludedIn_WithPitchClass_IgnoresOctave()
    {
        var inCMajorScale = PitchClassSet.IsIncludedIn(["C", "D", "E", "F", "G", "A", "B"]);

        Assert.True(inCMajorScale("D3"));
        Assert.True(inCMajorScale("D9"));
        Assert.False(inCMajorScale("Bb3"));
    }

    // -------------------------------------------------------------------------
    // IsSubsetOf
    // -------------------------------------------------------------------------

    [Fact]
    public void IsSubsetOf_TriadIsSubsetOfScale_ReturnsTrue()
    {
        var subsetOfMajor = PitchClassSet.IsSubsetOf(["C", "D", "E", "F", "G", "A", "B"]);

        Assert.True(subsetOfMajor(["C", "E", "G"]));    // C major triad ⊆ C major scale
        Assert.True(subsetOfMajor(["D", "F", "A"]));    // D minor triad ⊆ C major scale
        Assert.True(subsetOfMajor(["E", "G", "B"]));    // E minor triad ⊆ C major scale
    }

    [Fact]
    public void IsSubsetOf_NoteOutsideParent_ReturnsFalse()
    {
        var subsetOfMajor = PitchClassSet.IsSubsetOf(["C", "D", "E", "F", "G", "A", "B"]);

        Assert.False(subsetOfMajor(["C", "D", "Eb"])); // Eb not in C major
        Assert.False(subsetOfMajor(["C", "Bb"]));      // Bb not in C major
    }

    [Fact]
    public void IsSubsetOf_SetIsSubsetOfItself_ReturnsTrue()
    {
        var subsetOfCMajor = PitchClassSet.IsSubsetOf(["C", "D", "E", "F", "G", "A", "B"]);
        Assert.True(subsetOfCMajor(["C", "D", "E", "F", "G", "A", "B"]));
    }

    [Fact]
    public void IsSubsetOf_EmptySetIsSubsetOfAnything_ReturnsTrue()
    {
        var subsetOfMajor = PitchClassSet.IsSubsetOf(["C", "E", "G"]);
        Assert.True(subsetOfMajor([]));
    }

    // -------------------------------------------------------------------------
    // IsSupersetOf
    // -------------------------------------------------------------------------

    [Fact]
    public void IsSupersetOf_ScaleIsSupersetOfTriad_ReturnsTrue()
    {
        var supersetOfTriad = PitchClassSet.IsSupersetOf(["C", "E", "G"]);

        Assert.True(supersetOfTriad(["C", "D", "E", "F", "G", "A", "B"]));
        Assert.True(supersetOfTriad(["C", "E", "G", "B"])); // Cmaj7 is a superset
    }

    [Fact]
    public void IsSupersetOf_MissingNote_ReturnsFalse()
    {
        var supersetOfCTriad = PitchClassSet.IsSupersetOf(["C", "E", "G"]);

        Assert.False(supersetOfCTriad(["C", "E"]));       // G is missing
        Assert.False(supersetOfCTriad(["C", "Eb", "G"])); // E vs Eb — different pitch class
    }

    [Fact]
    public void IsSupersetOf_SetIsSupersetOfItself_ReturnsTrue()
    {
        var supersetOfTriad = PitchClassSet.IsSupersetOf(["C", "E", "G"]);
        Assert.True(supersetOfTriad(["C", "E", "G"]));
    }

    // -------------------------------------------------------------------------
    // IsSubsetOf / IsSupersetOf are inverse relations
    // -------------------------------------------------------------------------

    [Fact]
    public void SubsetAndSuperset_AreInverseRelations()
    {
        var scale = new[] { "C", "D", "E", "F", "G", "A", "B" };
        var triad = new[] { "C", "E", "G" };

        var triadIsSubsetOfScale   = PitchClassSet.IsSubsetOf(scale)(triad);
        var scaleIsSupersetOfTriad = PitchClassSet.IsSupersetOf(triad)(scale);

        Assert.True(triadIsSubsetOfScale);
        Assert.True(scaleIsSupersetOfTriad);
        Assert.Equal(triadIsSubsetOfScale, scaleIsSupersetOfTriad);
    }

    // -------------------------------------------------------------------------
    // Boundary / edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_EmptyNoteList_ReturnsAllZeroChroma()
    {
        var result = PitchClassSet.Get([]);
        Assert.Equal("000000000000", result.Chroma);
        Assert.Equal(0, result.Num);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Get_AllTwelveNotes_ReturnsFullChromaAndNum4095()
    {
        var chromatic = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var result = PitchClassSet.Get(chromatic);

        Assert.Equal("111111111111", result.Chroma);
        Assert.Equal(4095, result.Num);
        Assert.Equal(12, result.Length);
    }

    [Fact]
    public void Get_InvalidNoteNames_AreIgnored()
    {
        var result = PitchClassSet.Get(["C", "not-a-note", "G"]);
        var expected = PitchClassSet.Get(["C", "G"]);

        Assert.Equal(expected.Chroma, result.Chroma);
    }
}
