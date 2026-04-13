using System.Collections.Generic;

using Xunit;

namespace Tonal.Core.Tests;

public class RomanNumeralTests
{
    // -------------------------------------------------------------------------
    // Tokenize
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("I",        "I",        "",  "I",   "")]
    [InlineData("IV",       "IV",       "",  "IV",  "")]
    [InlineData("VII",      "VII",      "",  "VII", "")]
    [InlineData("bVII",     "bVII",     "b", "VII", "")]
    [InlineData("#IV",      "#IV",      "#", "IV",  "")]
    [InlineData("bII",      "bII",      "b", "II",  "")]
    [InlineData("Imaj7",    "Imaj7",    "",  "I",   "maj7")]
    [InlineData("IVmaj7",   "IVmaj7",   "",  "IV",  "maj7")]
    [InlineData("bVIImaj7", "bVIImaj7", "b", "VII", "maj7")]
    [InlineData("V7",       "V7",       "",  "V",   "7")]
    [InlineData("ii",       "ii",       "",  "ii",  "")]
    [InlineData("iim7",     "iim7",     "",  "ii",  "m7")]
    [InlineData("vii",      "vii",      "",  "vii", "")]
    [InlineData("viio7",    "viio7",    "",  "vii", "o7")]
    [InlineData("viim7b5",  "viim7b5",  "",  "vii", "m7b5")]
    [InlineData("##II",     "##II",     "##","II",  "")]
    [InlineData("xII",      "xII",      "x", "II",  "")]
    public void Tokenize_ParsesAllParts(
        string src, string expectedName, string expectedAcc,
        string expectedRoman, string expectedChordType)
    {
        var (name, acc, roman, chordType) = RomanNumeral.Tokenize(src);
        Assert.Equal(expectedName,      name);
        Assert.Equal(expectedAcc,       acc);
        Assert.Equal(expectedRoman,     roman);
        Assert.Equal(expectedChordType, chordType);
    }

    [Theory]
    [InlineData("C")]        // plain note name, not a roman numeral
    [InlineData("")]
    [InlineData("VIII")]     // no such roman numeral
    [InlineData("Cmaj7")]    // chord symbol, not roman numeral
    public void Tokenize_InvalidInput_ReturnsEmptyStrings(string src)
    {
        var (name, acc, roman, chordType) = RomanNumeral.Tokenize(src);
        Assert.Equal("", name);
        Assert.Equal("", acc);
        Assert.Equal("", roman);
        Assert.Equal("", chordType);
    }

    // -------------------------------------------------------------------------
    // Get — basic properties
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_I_ReturnsCorrectProperties()
    {
        var rn = RomanNumeral.Get("I");

        Assert.False(rn.IsEmpty);
        Assert.Equal("I",  rn.Name);
        Assert.Equal("I",  rn.Roman);
        Assert.Equal("",   rn.Acc);
        Assert.Equal("",   rn.ChordType);
        Assert.Equal(0,    rn.Step);
        Assert.Equal(0,    rn.Alt);
        Assert.True(rn.Major);
        Assert.Equal("1P", rn.Interval);
    }

    [Fact]
    public void Get_bVII_ReturnsCorrectProperties()
    {
        var rn = RomanNumeral.Get("bVII");

        Assert.False(rn.IsEmpty);
        Assert.Equal("bVII", rn.Name);
        Assert.Equal("VII",  rn.Roman);
        Assert.Equal("b",    rn.Acc);
        Assert.Equal("",     rn.ChordType);
        Assert.Equal(6,      rn.Step);
        Assert.Equal(-1,     rn.Alt);
        Assert.True(rn.Major);
        Assert.Equal("7m",   rn.Interval);
    }

    [Fact]
    public void Get_iim7_ReturnsCorrectProperties()
    {
        var rn = RomanNumeral.Get("iim7");

        Assert.False(rn.IsEmpty);
        Assert.Equal("iim7", rn.Name);
        Assert.Equal("ii",   rn.Roman);
        Assert.Equal("",     rn.Acc);
        Assert.Equal("m7",   rn.ChordType);
        Assert.Equal(1,      rn.Step);
        Assert.Equal(0,      rn.Alt);
        Assert.False(rn.Major);
        Assert.Equal("2M",   rn.Interval);
    }

    [Fact]
    public void Get_bIImaj7_ReturnsCorrectProperties()
    {
        var rn = RomanNumeral.Get("bIImaj7");

        Assert.Equal("bIImaj7", rn.Name);
        Assert.Equal("II",      rn.Roman);
        Assert.Equal("b",       rn.Acc);
        Assert.Equal("maj7",    rn.ChordType);
        Assert.Equal(-1,        rn.Alt);
        Assert.Equal("2m",      rn.Interval);
        Assert.True(rn.Major);
    }

    [Fact]
    public void Get_InvalidInput_ReturnsEmpty()
    {
        Assert.True(RomanNumeral.Get("").IsEmpty);
        Assert.True(RomanNumeral.Get("C").IsEmpty);
        Assert.True(RomanNumeral.Get("VIII").IsEmpty);
    }

    // -------------------------------------------------------------------------
    // Get — all diatonic degrees and their intervals
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("I",   "1P", true)]
    [InlineData("II",  "2M", true)]
    [InlineData("III", "3M", true)]
    [InlineData("IV",  "4P", true)]
    [InlineData("V",   "5P", true)]
    [InlineData("VI",  "6M", true)]
    [InlineData("VII", "7M", true)]
    [InlineData("i",   "1P", false)]
    [InlineData("ii",  "2M", false)]
    [InlineData("iii", "3M", false)]
    [InlineData("iv",  "4P", false)]
    [InlineData("v",   "5P", false)]
    [InlineData("vi",  "6M", false)]
    [InlineData("vii", "7M", false)]
    public void Get_DiatonicDegrees_HaveCorrectIntervalsAndCase(
        string src, string expectedInterval, bool expectedMajor)
    {
        var rn = RomanNumeral.Get(src);
        Assert.False(rn.IsEmpty);
        Assert.Equal(expectedInterval, rn.Interval);
        Assert.Equal(expectedMajor,    rn.Major);
    }

    // -------------------------------------------------------------------------
    // Get — accidental alterations and their intervals
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("bII",   "2m",  -1)]
    [InlineData("bIII",  "3m",  -1)]
    [InlineData("bVI",   "6m",  -1)]
    [InlineData("bVII",  "7m",  -1)]
    [InlineData("bV",    "5d",  -1)]
    [InlineData("#IV",   "4A",  +1)]
    [InlineData("#V",    "5A",  +1)]
    [InlineData("##II",  "2AA", +2)]   // ## = alt +2 → doubly augmented second
    [InlineData("bbVII", "7d",  -2)]
    [InlineData("xII",   "2AA", +2)]   // x = double sharp = alt +2 → doubly augmented second
    public void Get_WithAccidentals_HasCorrectIntervalAndAlt(
        string src, string expectedInterval, int expectedAlt)
    {
        var rn = RomanNumeral.Get(src);
        Assert.False(rn.IsEmpty, $"'{src}' should parse successfully");
        Assert.Equal(expectedAlt,       rn.Alt);
        Assert.Equal(expectedInterval,  rn.Interval);
    }

    // -------------------------------------------------------------------------
    // Get — chord type suffix is preserved verbatim
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Imaj7",    "maj7")]
    [InlineData("V7",       "7")]
    [InlineData("iim7",     "m7")]
    [InlineData("viim7b5",  "m7b5")]
    [InlineData("viio7",    "o7")]
    [InlineData("bVIImaj7", "maj7")]
    [InlineData("IV",       "")]
    public void Get_ChordTypeSuffix_IsPreservedVerbatim(string src, string expectedChordType)
    {
        Assert.Equal(expectedChordType, RomanNumeral.Get(src).ChordType);
    }

    // -------------------------------------------------------------------------
    // Get — step (0-based degree index)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("I",   0)]
    [InlineData("II",  1)]
    [InlineData("III", 2)]
    [InlineData("IV",  3)]
    [InlineData("V",   4)]
    [InlineData("VI",  5)]
    [InlineData("VII", 6)]
    [InlineData("bVI", 5)]  // accidentals don't change the step
    [InlineData("#IV", 3)]
    public void Get_Step_IsZeroBasedDegreeIndex(string src, int expectedStep)
    {
        Assert.Equal(expectedStep, RomanNumeral.Get(src).Step);
    }

    // -------------------------------------------------------------------------
    // Get — by degree index
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, "I",   "1P")]
    [InlineData(1, "II",  "2M")]
    [InlineData(2, "III", "3M")]
    [InlineData(3, "IV",  "4P")]
    [InlineData(4, "V",   "5P")]
    [InlineData(5, "VI",  "6M")]
    [InlineData(6, "VII", "7M")]
    public void Get_ByDegreeIndex_ReturnsCorrectRomanNumeral(
        int index, string expectedName, string expectedInterval)
    {
        var rn = RomanNumeral.Get(index);
        Assert.False(rn.IsEmpty);
        Assert.Equal(expectedName,     rn.Name);
        Assert.Equal(expectedInterval, rn.Interval);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(100)]
    public void Get_ByDegreeIndex_OutOfRange_ReturnsEmpty(int index)
    {
        Assert.True(RomanNumeral.Get(index).IsEmpty);
    }

    // -------------------------------------------------------------------------
    // Names
    // -------------------------------------------------------------------------

    [Fact]
    public void Names_Major_ReturnsUppercase()
    {
        var names = RomanNumeral.Names(major: true);
        Assert.Equal<IEnumerable<string>>(["I", "II", "III", "IV", "V", "VI", "VII"], names);
    }

    [Fact]
    public void Names_Minor_ReturnsLowercase()
    {
        var names = RomanNumeral.Names(major: false);
        Assert.Equal<IEnumerable<string>>(["i", "ii", "iii", "iv", "v", "vi", "vii"], names);
    }

    [Fact]
    public void Names_DefaultIsMajor()
    {
        Assert.Equal(RomanNumeral.Names(major: true), RomanNumeral.Names());
    }

    // -------------------------------------------------------------------------
    // Caching — same object returned for same input
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_SameInput_ReturnsCachedResult()
    {
        var first  = RomanNumeral.Get("bVIImaj7");
        var second = RomanNumeral.Get("bVIImaj7");
        Assert.Same(first, second);
    }

    // -------------------------------------------------------------------------
    // Integration: interval resolves to a real transposable interval
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("I",    "C", "C")]
    [InlineData("II",   "C", "D")]
    [InlineData("III",  "C", "E")]
    [InlineData("IV",   "C", "F")]
    [InlineData("V",    "C", "G")]
    [InlineData("VI",   "C", "A")]
    [InlineData("VII",  "C", "B")]
    [InlineData("bVII", "C", "Bb")]
    [InlineData("#IV",  "C", "F#")]
    [InlineData("I",    "G", "G")]
    [InlineData("V",    "G", "D")]
    [InlineData("IV",   "G", "C")]
    [InlineData("bVII", "Bb","Ab")]
    public void Interval_WhenTransposedFromTonic_GivesCorrectScaleDegreeNote(
        string romanStr, string tonic, string expectedNote)
    {
        var rn   = RomanNumeral.Get(romanStr);
        var note = Note.Transpose(tonic, rn.Interval);
        Assert.Equal(expectedNote, note);
    }

    // -------------------------------------------------------------------------
    // Edge cases: diminished intervals from double-flat
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_bbVII_HasDoublyDiminishedInterval()
    {
        var rn = RomanNumeral.Get("bbVII");
        Assert.Equal(-2,  rn.Alt);
        Assert.Equal("7d", rn.Interval); // diminished seventh
    }

    [Fact]
    public void Get_bIV_HasDiminishedFourth()
    {
        // b on a perfectable degree (IV): alt=-1 → diminished
        var rn = RomanNumeral.Get("bIV");
        Assert.Equal(-1,  rn.Alt);
        Assert.Equal("4d", rn.Interval);
    }

    // -------------------------------------------------------------------------
    // Case sensitivity: uppercase = major, lowercase = minor
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_UppercaseAndLowercase_DifferOnlyInMajorFlag()
    {
        var upper = RomanNumeral.Get("VI");
        var lower = RomanNumeral.Get("vi");

        Assert.True(upper.Major);
        Assert.False(lower.Major);
        Assert.Equal(upper.Interval, lower.Interval);
        Assert.Equal(upper.Step,     lower.Step);
    }
}
