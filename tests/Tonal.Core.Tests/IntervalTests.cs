using Xunit;
using System.Linq;

namespace Tonal.Core.Tests;

public class IntervalTests
{
    [Fact]
    public void Properties()
    {
        var result = Interval.Get("P4");
        Assert.Equal(0, result.Alt);
        Assert.Equal(5, result.Chroma);
        Assert.Equal(new IntervalCoordinates { Fifths = -1, Octaves = 1, Direction = Direction.Ascending }, result.Coord);
        Assert.Equal(Direction.Ascending, result.Dir);
        Assert.False(result.Empty);
        Assert.Equal("4P", result.Name);
        Assert.Equal(4, result.Num);
        Assert.Equal(0, result.Oct);
        Assert.Equal("P", result.Q);
        Assert.Equal(5, result.Semitones);
        Assert.Equal(4, result.Simple);
        Assert.Equal(3, result.Step);
        Assert.Equal("perfectable", result.Type);
    }

    [Fact]
    public void ShorthandProperties()
    {
        Assert.Equal("5d", Interval.Name("d5"));
        Assert.Equal(5, Interval.Num("d5"));
        Assert.Equal("d", Interval.Quality("d5"));
        Assert.Equal(6, Interval.Semitones("d5"));
    }

    [Fact]
    public void Distance()
    {
        Assert.Equal("5P", Interval.Distance("C4", "G4"));
    }

    [Fact]
    public void Names()
    {
        Assert.Equal(
            new[] { "1P", "2M", "3M", "4P", "5P", "6m", "7m" },
            Interval.Names()
        );
    }

    [Fact]
    public void SimplifyIntervals()
    {
        Assert.Equal(
            "1P 2M 3M 4P 5P 6M 7M".Split(' ').ToList(),
            "1P 2M 3M 4P 5P 6M 7M".Split(' ').Select(Interval.Simplify).ToList()
        );
        Assert.Equal(
            "8P 2M 3M 4P 5P 6M 7M".Split(' ').ToList(),
            "8P 9M 10M 11P 12P 13M 14M".Split(' ').Select(Interval.Simplify).ToList()
        );
        Assert.Equal(
            "1d 1P 1A 8d 8P 8A 15d 15P 15A".Split(' ').ToList(),
            "1d 1P 1A 8d 8P 8A 15d 15P 15A".Split(' ').Select(Interval.Simplify).ToList()
        );
        Assert.Equal(
            "-1P -2M -3M -4P -5P -6M -7M".Split(' ').ToList(),
            "-1P -2M -3M -4P -5P -6M -7M".Split(' ').Select(Interval.Simplify).ToList()
        );
        Assert.Equal(
            "-8P -2M -3M -4P -5P -6M -7M".Split(' ').ToList(),
            "-8P -9M -10M -11P -12P -13M -14M".Split(' ').ToList().Select(Interval.Simplify).ToList()
        );
    }

    [Fact]
    public void InvertIntervals()
    {
        Assert.Equal(
            "1P 7m 6m 5P 4P 3m 2m".Split(' ').ToList(),
            "1P 2M 3M 4P 5P 6M 7M".Split(' ').Select(Interval.Invert).ToList()
        );
        Assert.Equal(
            "1A 7M 6M 5A 4A 3M 2M".Split(' ').ToList(),
            "1d 2m 3m 4d 5d 6m 7m".Split(' ').Select(Interval.Invert).ToList()
        );
        Assert.Equal(
            "1d 7d 6d 5d 4d 3d 2d".Split(' ').ToList(),
            "1A 2A 3A 4A 5A 6A 7A".Split(' ').Select(Interval.Invert).ToList()
        );
        Assert.Equal(
            "-1P -7m -6m -5P -4P -3m -2m".Split(' ').ToList(),
            "-1P -2M -3M -4P -5P -6M -7M".Split(' ').Select(Interval.Invert).ToList()
        );
        Assert.Equal(
            "8P 14m 13m 12P 11P 10m 9m".Split(' ').ToList(),
            "8P 9M 10M 11P 12P 13M 14M".Split(' ').Select(Interval.Invert).ToList()
        );
    }

    [Fact]
    public void FromSemitones()
    {
        var semis = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        Assert.Equal(
            "1P 2m 2M 3m 3M 4P 5d 5P 6m 6M 7m 7M".Split(' ').ToList(),
            semis.Select(Interval.FromSemitones).ToList()
        );

        semis = new[] { 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
        Assert.Equal(
            "8P 9m 9M 10m 10M 11P 12d 12P 13m 13M 14m 14M".Split(' ').ToList(),
            semis.Select(Interval.FromSemitones).ToList()
        );

        semis = new[] { -0, -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11 };
        Assert.Equal(
            "1P -2m -2M -3m -3M -4P -5d -5P -6m -6M -7m -7M".Split(' ').ToList(),
            semis.Select(Interval.FromSemitones).ToList()
        );

        semis = new[] { -12, -13, -14, -15, -16, -17, -18, -19, -20, -21, -22, -23 };
        Assert.Equal(
            "-8P -9m -9M -10m -10M -11P -12d -12P -13m -13M -14m -14M".Split(' ').ToList(),
            semis.Select(Interval.FromSemitones).ToList()
        );
    }

    [Fact]
    public void Add()
    {
        Assert.Equal("7m", Interval.Add("3m", "5P"));
        Assert.Equal(
            "5P 6M 7M 8P 9M 10m 11P".Split(' ').ToList(),
            Interval.Names().Select(n => Interval.Add("5P", n)).ToList()
        );
        Assert.Equal(
            "5P 6M 7M 8P 9M 10m 11P".Split(' ').ToList(),
            Interval.Names().Select(Interval.AddTo("5P")).ToList()
        );
    }

    [Fact]
    public void Subtract()
    {
        Assert.Equal("3m", Interval.Subtract("5P", "3M"));
        Assert.Equal("-3m", Interval.Subtract("3M", "5P"));
        Assert.Equal(
            "5P 4P 3m 2M 1P -2m -3m".Split(' ').ToList(),
            Interval.Names().Select(n => Interval.Subtract("5P", n)).ToList()
        );
    }

    [Fact]
    public void TransposeFifths()
    {
        Assert.Equal("8P", Interval.TransposeFifths("4P", 1));
        Assert.Equal(
            "1P 5P 9M 13M 17M",
            string.Join(" ", new[] { 0, 1, 2, 3, 4 }
                .Select(fifths => Interval.TransposeFifths("1P", fifths)))
        );
        Assert.Equal(
            "1P -5P -9M -13M -17M",
            string.Join(" ", new[] { 0, -1, -2, -3, -4 }
                .Select(fifths => Interval.TransposeFifths("1P", fifths)))
        );
    }
}