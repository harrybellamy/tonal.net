namespace Tonal.Core;

public static class Pitch
{
    // We need to get the steps from fifths
    // Fifths for CDEFGAB are [ 0, 2, 4, -1, 1, 3, 5 ]
    // We add 1 to fifths to avoid negative numbers, so:
    // for ["F", "C", "G", "D", "A", "E", "B"] we have:
    private static readonly int[] FIFTHS_TO_STEPS = [3, 0, 4, 1, 5, 2, 6];
    // The number of fifths of [C, D, E, F, G, A, B]
    public static readonly int[] FIFTHS = [0, 2, 4, -1, 1, 3, 5];
    // The number of octaves it span each step
    public static readonly int[] STEPS_TO_OCTS = FIFTHS.Select(fifth => (int)Math.Floor((fifth * 7) / 12D)).ToArray();

    /// <summary>
    /// Get pitch from coordinate objects
    /// </summary>
    public static PitchInfo Get(Coordinates coord)
     {
        if (coord is IntervalCoordinates iCoord)
        {
            var f = iCoord.Fifths;
            var o = iCoord.Octaves;
            var dir = (int)iCoord.Direction;       
            var step = FIFTHS_TO_STEPS[Unaltered(f)];
            var alt = (int)Math.Floor((f + 1) / 7D);
            var oct = o + 4 * alt + STEPS_TO_OCTS[step];
            return new PitchInfo { Step = step, Alt = alt, Oct = oct, Dir = (Direction)dir };
        } else if (coord is NoteCoordinates nCoord)
        {
            var f = nCoord.Fifths;
            var o = nCoord.Octaves;
            var dir = 1;
            var step = FIFTHS_TO_STEPS[Unaltered(f)];
            var alt = (int)Math.Floor((f + 1) / 7D);
            var oct = o + 4 * alt + STEPS_TO_OCTS[step];
            return new PitchInfo { Step = step, Alt = (int)alt, Oct = oct, Dir = (Direction)dir };
        } else if (coord is PitchClassCoordinates pcCoord)
        {
            var f = pcCoord.Fifths;
            var step = FIFTHS_TO_STEPS[Unaltered(f)];
            var alt = Math.Floor((f + 1) / 7D);
            return new PitchInfo { Step = step, Alt = (int)alt, Oct = null, Dir = null };
        }

        throw new ArgumentException("Invalid Coordinates type");
    }

    /// <summary>
    /// Return the number of fifths as if it were unaltered
    /// </summary>
    /// <param name="f"></param>
    /// <returns>The number of fifths</returns>
    private static int Unaltered(int f)
    {
        var i = (f + 1) % 7;
        return i < 0 ? 7 + i : i;
    }
}

public record PitchInfo
{
    public int Step { get; init; }
    public int Alt { get; init; }
    public int? Oct { get; init; }
    public Direction? Dir { get; init; }

    private char StepToLetter() => "CDEFGAB"[Step];
    private string AltToAcc() => Alt < 0 ? "".PadRight(-Alt, 'b') : "".PadRight(Alt, '#');
    public string GetName() 
    {
        if (Step< 0 || Step > 6) {
            return "";
        }
        var letter = StepToLetter();
        var pc = letter + AltToAcc();
        return Oct.HasValue ? pc + Oct.Value.ToString() : pc;
    }

    /// <summary>
    /// Get the coordinates of the pitch
    /// </summary>
    /// <returns>The pitch coordinates</returns>
    public PitchClassCoordinates ToCoordinates()
    {
        var f = Pitch.FIFTHS[Step] + 7 * Alt;
        var newDir = Dir ?? Direction.Ascending;
        if (!Oct.HasValue) {
            return new PitchClassCoordinates { Fifths = (int)newDir * f };
        }

        var o = Oct.Value - Pitch.STEPS_TO_OCTS[Step] - 4 * Alt;
        if (Dir.HasValue) 
        {
            return new IntervalCoordinates { Fifths = (int)Dir.Value * f, Octaves = (int)Dir.Value * o, Direction = Dir.Value };
        }
        else
        {
            return new NoteCoordinates { Fifths = (int)newDir * f, Octaves = (int)newDir * o };
        }
    }
}
