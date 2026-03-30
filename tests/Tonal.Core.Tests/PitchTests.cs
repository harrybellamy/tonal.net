using Xunit;
using System.Linq;

namespace Tonal.Core.Tests;

public class PitchTests
{
    // Pitch classes
    private static readonly PitchInfo C   = new() { Step = 0, Alt = 0 };
    private static readonly PitchInfo Cs  = new() { Step = 0, Alt = 1 };
    private static readonly PitchInfo Cb  = new() { Step = 0, Alt = -1 };
    private static readonly PitchInfo A   = new() { Step = 5, Alt = 0 };

    // Notes
    private static readonly PitchInfo C4  = new() { Step = 0, Alt = 0, Oct = 4 };
    private static readonly PitchInfo A4  = new() { Step = 5, Alt = 0, Oct = 4 };
    private static readonly PitchInfo Gs6 = new() { Step = 4, Alt = 1, Oct = 6 };

    // Intervals
    private static readonly PitchInfo P5  = new() { Step = 4, Alt = 0, Oct = 0, Dir = Direction.Ascending };
    private static readonly PitchInfo P_5 = new() { Step = 4, Alt = 0, Oct = 0, Dir = Direction.Descending };

    // [Fact]
    // public void Height_PitchClasses_ReturnsExpectedValues()
    // {
    //     var pitches = new[] { C, Cs, Cb, A };
    //     var result = pitches.Select(Pitch.Height).ToArray();
    //     Assert.Equal(new[] { -1200, -1199, -1201, -1191 }, result);
    // }

    // [Fact]
    // public void Height_Notes_ReturnsExpectedValues()
    // {
    //     var notes = new[] { C4, A4, Gs6 };
    //     var result = notes.Select(Pitch.Height).ToArray();
    //     Assert.Equal(new[] { 48, 57, 80 }, result);
    // }

    // [Fact]
    // public void Height_Intervals_ReturnsExpectedValues()
    // {
    //     var intervals = new[] { P5, P_5 };
    //     var result = intervals.Select(Pitch.Height).ToArray();
    //     Assert.Equal(new[] { 7, -7 }, result);
    // }

    // [Fact]
    // public void Midi_PitchClasses_ReturnsAllNull()
    // {
    //     var pitches = new[] { C, Cs, Cb, A };
    //     var result = pitches.Select(Pitch.Midi).ToArray();
    //     Assert.Equal(new int?[] { null, null, null, null }, result);
    // }

    // [Fact]
    // public void Midi_Notes_ReturnsExpectedValues()
    // {
    //     var notes = new[] { C4, A4, Gs6 };
    //     var result = notes.Select(Pitch.Midi).ToArray();
    //     Assert.Equal(new int?[] { 60, 69, 92 }, result);
    // }

    // [Fact]
    // public void Chroma_PitchClasses_ReturnsExpectedValues()
    // {
    //     var pitches = new[] { C, Cs, Cb, A };
    //     var result = pitches.Select(Pitch.Chroma).ToArray();
    //     Assert.Equal(new[] { 0, 1, 11, 9 }, result);
    // }

    // [Fact]
    // public void Chroma_Notes_ReturnsExpectedValues()
    // {
    //     var notes = new[] { C4, A4, Gs6 };
    //     var result = notes.Select(Pitch.Chroma).ToArray();
    //     Assert.Equal(new[] { 0, 9, 8 }, result);
    // }

    // [Fact]
    // public void Chroma_Intervals_ReturnsExpectedValues()
    // {
    //     var intervals = new[] { P5, P_5 };
    //     var result = intervals.Select(Pitch.Chroma).ToArray();
    //     Assert.Equal(new[] { 7, 7 }, result);
    // }

    [Fact]
    public void Coordinates_PitchClasses_ReturnsExpectedValues()
    {
        Assert.Equal(new PitchClassCoordinates { Fifths = 0 }, C.ToCoordinates());
        Assert.Equal(new PitchClassCoordinates { Fifths = 3 }, A.ToCoordinates());
        Assert.Equal(new PitchClassCoordinates { Fifths = 7 }, Cs.ToCoordinates());
        Assert.Equal(new PitchClassCoordinates { Fifths = -7 }, Cb.ToCoordinates());
    }

    [Fact]
    public void Coordinates_Notes_ReturnsExpectedValues()
    {
        Assert.Equal(new NoteCoordinates { Fifths = 0, Octaves = 4 }, C4.ToCoordinates());
        Assert.Equal(new NoteCoordinates { Fifths = 3, Octaves = 3 }, A4.ToCoordinates());
    }

    [Fact]
    public void Coordinates_Intervals_ReturnsExpectedValues()
    {
        Assert.Equal(new IntervalCoordinates { Fifths = 1, Octaves = 0, Direction = Direction.Ascending }, P5.ToCoordinates());
        Assert.Equal(new IntervalCoordinates { Fifths = -1, Octaves = 0, Direction = Direction.Descending }, P_5.ToCoordinates());
    }

    [Fact]
    public void Pitch_Get_ReturnsExpectedPitch()
    {
        Assert.Equal(C,  Pitch.Get(new PitchClassCoordinates{ Fifths = 0 }));
        Assert.Equal(Cs, Pitch.Get(new PitchClassCoordinates{ Fifths = 7 }));
    }
}