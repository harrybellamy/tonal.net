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
}
