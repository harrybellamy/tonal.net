using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Tonal.Core.Tests;

public class ScaleTypeTests
{
    // -------------------------------------------------------------------------
    // Get — by name
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ByName_ReturnsCorrectInfo()
    {
        var result = ScaleType.Get("major");

        Assert.False(result.IsEmpty);
        Assert.Equal("major", result.Name);
        Assert.Equal(7, result.Length);
        Assert.Equal("101011010101", result.Chroma);
        Assert.Equal(2773, result.SetNum);
    }

    [Fact]
    public void Get_ByName_HasCorrectIntervals()
    {
        var result = ScaleType.Get("major");
        Assert.Equal<IEnumerable<string>>(
            ["1P", "2M", "3M", "4P", "5P", "6M", "7M"],
            result.Intervals);
    }

    [Fact]
    public void Get_ByName_HasCorrectAliases()
    {
        var result = ScaleType.Get("major");
        Assert.Equal<IEnumerable<string>>(["ionian"], result.Aliases);
    }

    [Fact]
    public void Get_Minor_ReturnsCorrectInfo()
    {
        var result = ScaleType.Get("minor");

        Assert.False(result.IsEmpty);
        Assert.Equal("minor", result.Name);
        Assert.Equal(7, result.Length);
        Assert.Equal<IEnumerable<string>>(
            ["1P", "2M", "3m", "4P", "5P", "6m", "7m"],
            result.Intervals);
        Assert.Equal<IEnumerable<string>>(["aeolian"], result.Aliases);
    }

    [Fact]
    public void Get_UnknownName_ReturnsEmpty()
    {
        var result = ScaleType.Get("not-a-scale");
        Assert.True(result.IsEmpty);
    }

    // -------------------------------------------------------------------------
    // Get — by alias
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ByAlias_ReturnsSameAsCanonicalName()
    {
        var byName  = ScaleType.Get("major");
        var byAlias = ScaleType.Get("ionian");

        Assert.False(byAlias.IsEmpty);
        Assert.Equal(byName.Name,   byAlias.Name);
        Assert.Equal(byName.Chroma, byAlias.Chroma);
        Assert.Equal(byName.SetNum, byAlias.SetNum);
    }

    [Theory]
    [InlineData("aeolian",   "minor")]
    [InlineData("pentatonic","major pentatonic")]
    [InlineData("blues",     "minor blues")]
    [InlineData("dominant",  "mixolydian")]
    [InlineData("chinese",   "lydian pentatonic")]
    [InlineData("pomeroy",   "altered")]
    [InlineData("gypsy",     "double harmonic major")]
    [InlineData("spanish",   "phrygian dominant")]
    public void Get_ByAlias_ResolvesToCorrectCanonicalName(string alias, string expectedName)
    {
        var result = ScaleType.Get(alias);
        Assert.False(result.IsEmpty);
        Assert.Equal(expectedName, result.Name);
    }

    // -------------------------------------------------------------------------
    // Get — by chroma
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_ByChroma_ReturnsSameAsName()
    {
        var byName   = ScaleType.Get("major");
        var byChroma = ScaleType.Get(byName.Chroma);

        Assert.False(byChroma.IsEmpty);
        Assert.Equal(byName.Name,   byChroma.Name);
        Assert.Equal(byName.SetNum, byChroma.SetNum);
    }

    [Theory]
    [InlineData("101011010101", "major")]
    [InlineData("101101011010", "minor")]
    [InlineData("101101010110", "dorian")]
    [InlineData("110101011010", "phrygian")]
    [InlineData("101010110101", "lydian")]
    [InlineData("101011010110", "mixolydian")]
    public void Get_ByChroma_ReturnsCorrectScale(string chroma, string expectedName)
    {
        var result = ScaleType.Get(chroma);
        Assert.False(result.IsEmpty);
        Assert.Equal(expectedName, result.Name);
    }

    // -------------------------------------------------------------------------
    // Get — by set number
    // -------------------------------------------------------------------------

    [Fact]
    public void Get_BySetNum_ReturnsSameAsName()
    {
        var byName   = ScaleType.Get("major");
        var bySetNum = ScaleType.Get(byName.SetNum);

        Assert.False(bySetNum.IsEmpty);
        Assert.Equal(byName.Name,   bySetNum.Name);
        Assert.Equal(byName.Chroma, bySetNum.Chroma);
    }

    [Fact]
    public void Get_BySetNum_UnknownNumber_ReturnsEmpty()
    {
        // 0 is all zeros — no scale maps to this
        var result = ScaleType.Get(0);
        Assert.True(result.IsEmpty);
    }

    // -------------------------------------------------------------------------
    // All three lookup keys are consistent
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("major")]
    [InlineData("minor")]
    [InlineData("dorian")]
    [InlineData("whole tone")]
    [InlineData("chromatic")]
    [InlineData("major pentatonic")]
    public void AllThreeLookupKeysAreEquivalent(string name)
    {
        var byName   = ScaleType.Get(name);
        var byChroma = ScaleType.Get(byName.Chroma);
        var byNum    = ScaleType.Get(byName.SetNum);

        Assert.Equal(byName.Name, byChroma.Name);
        Assert.Equal(byName.Name, byNum.Name);
    }

    // -------------------------------------------------------------------------
    // Well-known scales — spot checks
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("major pentatonic",  5,  "101010010100")]
    [InlineData("major",             7,  "101011010101")]
    [InlineData("minor",             7,  "101101011010")]
    [InlineData("dorian",            7,  "101101010110")]
    [InlineData("phrygian",          7,  "110101011010")]
    [InlineData("lydian",            7,  "101010110101")]
    [InlineData("mixolydian",        7,  "101011010110")]
    [InlineData("locrian",           7,  "110101101010")]
    [InlineData("melodic minor",     7,  "101101010101")]
    [InlineData("harmonic minor",    7,  "101101011001")]
    [InlineData("whole tone",        6,  "101010101010")]
    [InlineData("chromatic",         12, "111111111111")]
    [InlineData("minor pentatonic",  5,  "100101010010")]
    public void Get_WellKnownScales_HaveCorrectChromaAndLength(
        string name, int expectedLength, string expectedChroma)
    {
        var result = ScaleType.Get(name);

        Assert.False(result.IsEmpty, $"'{name}' should be in the dictionary");
        Assert.Equal(expectedLength, result.Length);
        Assert.Equal(expectedChroma, result.Chroma);
    }

    // -------------------------------------------------------------------------
    // Names / All / Keys
    // -------------------------------------------------------------------------

    [Fact]
    public void Names_ContainsExpectedScales()
    {
        var names = ScaleType.Names();

        Assert.Contains("major",          names);
        Assert.Contains("minor",          names);
        Assert.Contains("dorian",         names);
        Assert.Contains("chromatic",      names);
        Assert.Contains("whole tone",     names);
        Assert.Contains("minor pentatonic", names);
    }

    [Fact]
    public void Names_DoesNotContainAliases()
    {
        var names = ScaleType.Names();

        // These are aliases, not canonical names — they should not appear
        Assert.DoesNotContain("ionian",     names);
        Assert.DoesNotContain("aeolian",    names);
        Assert.DoesNotContain("pentatonic", names);
        Assert.DoesNotContain("blues",      names);
    }

    [Fact]
    public void All_CountMatchesNames()
    {
        Assert.Equal(ScaleType.Names().Count, ScaleType.All().Count);
    }

    [Fact]
    public void All_ContainsNoEmptyEntries()
    {
        Assert.All(ScaleType.All(), s =>
        {
            Assert.False(s.IsEmpty);
            Assert.NotEmpty(s.Name);
            Assert.NotEmpty(s.Intervals);
            Assert.NotEmpty(s.Chroma);
        });
    }

    [Fact]
    public void Keys_ContainsNamesAliasesChromasAndSetNums()
    {
        var keys = ScaleType.Keys();

        // canonical name
        Assert.Contains("major",        keys);
        // alias
        Assert.Contains("ionian",       keys);
        // chroma
        Assert.Contains("101011010101", keys);
        // set number
        Assert.Contains("2773",         keys);
    }

    // -------------------------------------------------------------------------
    // Scale counts by note length
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void All_ContainsScalesOfExpectedLength(int noteCount)
    {
        var ofLength = ScaleType.All().Where(s => s.Length == noteCount).ToList();
        Assert.NotEmpty(ofLength);
    }

    [Fact]
    public void Chromatic_HasTwelveNotes()
    {
        var chromatic = ScaleType.Get("chromatic");
        Assert.Equal(12, chromatic.Length);
        Assert.Equal("111111111111", chromatic.Chroma);
    }

    // -------------------------------------------------------------------------
    // Add / RemoveAll
    // -------------------------------------------------------------------------

    [Fact]
    public void Add_CustomScale_IsRetrievableByNameChromaAndSetNum()
    {
        var intervals = new[] { "1P", "2M", "3M", "5P" };
        var name      = "test custom scale";

        try
        {
            var added = ScaleType.Add(intervals, name, ["custom alias"]);

            Assert.False(added.IsEmpty);
            Assert.Equal(name, added.Name);
            Assert.Equal(4, added.Length);

            // Retrievable by name
            Assert.Equal(name, ScaleType.Get(name).Name);
            // Retrievable by alias
            Assert.Equal(name, ScaleType.Get("custom alias").Name);
            // Retrievable by chroma
            Assert.Equal(name, ScaleType.Get(added.Chroma).Name);
            // Retrievable by set number
            Assert.Equal(name, ScaleType.Get(added.SetNum).Name);
        }
        finally
        {
            // Clean up: we can't easily remove one entry so we just verify
            // the standard entries still work after an addition
            Assert.Equal("major", ScaleType.Get("major").Name);
        }
    }

    // -------------------------------------------------------------------------
    // Chroma correctness — verify the chroma is derivable from intervals
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("major")]
    [InlineData("minor")]
    [InlineData("dorian")]
    [InlineData("whole tone")]
    [InlineData("minor pentatonic")]
    public void Chroma_MatchesDerivedFromIntervals(string name)
    {
        var scale    = ScaleType.Get(name);
        var derived  = PitchClassSet.Get(scale.Intervals
            .Select(ivl => Note.Transpose("C", ivl))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray());

        Assert.Equal(derived.Chroma, scale.Chroma);
        Assert.Equal(derived.Num,    scale.SetNum);
    }

    // -------------------------------------------------------------------------
    // Multi-alias scales
    // -------------------------------------------------------------------------

    [Fact]
    public void Altered_HasMultipleAliases()
    {
        var result = ScaleType.Get("altered");

        Assert.False(result.IsEmpty);
        Assert.Contains("super locrian",        result.Aliases);
        Assert.Contains("diminished whole tone", result.Aliases);
        Assert.Contains("pomeroy",               result.Aliases);

        // Each alias should resolve back to the same scale
        foreach (var alias in result.Aliases)
            Assert.Equal("altered", ScaleType.Get(alias).Name);
    }

    [Fact]
    public void UkrainianDorian_HasMultipleAliases()
    {
        var result = ScaleType.Get("dorian #4");

        Assert.False(result.IsEmpty);
        Assert.Contains("ukrainian dorian", result.Aliases);
        Assert.Contains("romanian minor",   result.Aliases);
        Assert.Contains("altered dorian",   result.Aliases);
    }
}