namespace Tonal.Core;

public abstract record Coordinates;

/// <summary>
/// Coordinates for Pitch Classes
/// </summary>
public record PitchClassCoordinates : Coordinates
{
    public int Fifths { get; init; }
}

/// <summary>
/// Coordinates for Notes
/// </summary>
public record NoteCoordinates : PitchClassCoordinates
{
    public int Octaves {get; init;}
}

/// <summary>
/// Coordinates for Intervals
/// </summary>
public record IntervalCoordinates : NoteCoordinates
{
    public Direction Direction { get; init; }
}

/// <summary>
/// Direction of an Interval
/// </summary>
public enum Direction
{
    Ascending = 1,
    Descending = -1
}