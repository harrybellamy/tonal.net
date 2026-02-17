using System.Text.RegularExpressions;

namespace Tonal.Core;

public static class Interval
{
    public enum IntervalType
    {
        Perfectable,
        Majorable
     }

    // interval numbers
    private static readonly int[] IN = [1, 2, 2, 3, 3, 4, 5, 5, 6, 6, 7, 7];
    // interval qualities
    private static readonly string[] IQ = "P m M m M P d P m M m M".Split(" ");
    private static readonly int[] SIZES = [0, 2, 4, 5, 7, 9, 11];
    private static readonly string TYPES = "PMMPPMM";

    /// shorthand tonal notation (with quality after number)
    private const string INTERVAL_TONAL_REGEX = @"([-+]?\d+)(d{1,4}|m|M|P|A{1,4})";
    // standard shorthand notation (with quality before number)
    private const string INTERVAL_SHORTHAND_REGEX = @"(AA|A|P|M|m|d|dd)([-+]?\d+)";
    private static readonly Regex IntervalRegex = new Regex(
        @"^(?:" + INTERVAL_TONAL_REGEX + "|" + INTERVAL_SHORTHAND_REGEX + ")$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Get the natural list of interval names
    /// </summary>
    public static string[] Names() => "1P 2M 3M 4P 5P 6m 7m".Split(" ");

    /// <summary>
    /// Get interval properties. It returns an object with:
    /// - name: the interval name
    /// - num: the interval number
    /// - type: 'perfectable' or 'majorable'
    /// - q: the interval quality (d, m, M, A)
    /// - dir: interval direction (1 ascending, -1 descending)
    /// - simple: the simplified number
    /// - semitones: the size in semitones
    /// - chroma: the interval chroma
    /// </summary>
    /// <param name="intervalName">the interval name</param>
    /// <returns>the interval properties</returns>
    public static IntervalInfo Get(string intervalName)
    {
        return Parse(intervalName);
    }

    /// <summary>
    /// Get interval properties. It returns an object with:
    /// - name: the interval name
    /// - num: the interval number
    /// - type: 'perfectable' or 'majorable'
    /// - q: the interval quality (d, m, M, A)
    /// - dir: interval direction (1 ascending, -1 descending)
    /// - simple: the simplified number
    /// - semitones: the size in semitones
    /// - chroma: the interval chroma
    /// </summary>
    /// <param name="pitch">the pitch information</param>
    /// <returns>the interval properties</returns>
    public static IntervalInfo Get(PitchInfo pitch)
    {
        return Get(PitchName(pitch));
    }

    private static Tuple<string, string> TokenizeInterval(string str)
    {
        if (string.IsNullOrEmpty(str))
            return Tuple.Create(string.Empty, string.Empty);

        var m = IntervalRegex.Match(str);
        if (!m.Success)
        {
            return Tuple.Create(string.Empty, string.Empty);
        }

        // tonal form -> groups 1=num, 2=quality
        if (m.Groups[1].Success)
        {
            return Tuple.Create(m.Groups[1].Value, m.Groups[2].Value);
        }

        // shorthand form -> groups 3=quality, 4=num -> return [num, quality]
        return Tuple.Create(m.Groups[4].Value, m.Groups[3].Value);
    }

    private static int qToAlt(string type, string q)
    {
        if ((q == "M" && type == "majorable") || (q == "P" && type == "perfectable"))
        {
            return 0;
        }

        if (q == "m" && type == "majorable")
        {
            return -1;
        }

        if (Regex.IsMatch(q, @"^A+$"))
        {
            return q.Length;
        }

        if (Regex.IsMatch(q, @"^d+$"))
        {
            var len = q.Length;
            return -1 * (type == "perfectable" ? len : len + 1);
        }

        return 0;
    }

    private static IntervalInfo Parse(string str)
    {
        var tokens = TokenizeInterval(str);
        if (tokens.Item1 == "") 
        {
            return IntervalInfo.NoInterval;
        }

        var num = int.Parse(tokens.Item1);
        var q = new Quality(tokens.Item2);
        var step = (Math.Abs(num) - 1) % 7;
        var t = TYPES[step];
        if (t == 'M' && q == "P") 
        {
            return IntervalInfo.NoInterval;
        }

        var type = t == 'M' ? "majorable" : "perfectable";

        var name = "" + num + q;
        var dir = num < 0 ? -1 : 1;
        var simple = num == 8 || num == -8 ? num : dir * (step + 1);
        var alt = qToAlt(type, q);
        var oct = (int)Math.Floor((Math.Abs(num) - 1) / 7D);
        var semitones = dir * (SIZES[step] + alt + 12 * oct);
        var chroma = (((dir * (SIZES[step] + alt)) % 12) + 12) % 12;

        PitchInfo pitchInfo = new() { Step = step, Alt = alt, Oct = oct, Dir = dir < 0 ? Direction.Descending : Direction.Ascending };

        var coord = pitchInfo.ToCoordinates() as IntervalCoordinates; 
        return new IntervalInfo
        {
            Empty = false,
            Name = name,
            Num = num,
            Q = q,
            Step = step,
            Alt = alt,
            Dir = dir < 0 ? Direction.Descending : Direction.Ascending,
            Type = type,
            Simple = simple,
            Semitones = semitones,
            Chroma = chroma,
            Coord = coord,
            Oct = oct         
        };
    }

    private static IntervalInfo CoordToInterval(
        PitchClassCoordinates coord, 
        bool forceDescending = false)
    {
        var f = coord.Fifths;
        var o = coord is NoteCoordinates nc ? nc.Octaves : 0;

        var isDescending = f * 7 + o * 12 < 0;
        var ivl = forceDescending || isDescending 
        ? new IntervalCoordinates { Fifths = -f, Octaves = -o, Direction = Direction.Descending } 
        : new IntervalCoordinates { Fifths = f, Octaves = o, Direction = Direction.Ascending };


        var pitch = Pitch.Get(ivl);
        return Interval.Get(pitch);
    }

    private static string PitchName(PitchInfo pitch)
    {
        if (!pitch.Dir.HasValue) 
        {
            return "";

        }

        var calcNum = pitch.Step + 1 + 7 * pitch.Oct;
        // this is an edge case: descending pitch class unison (see #243)
        var num = calcNum == 0 ? pitch.Step + 1 : calcNum;
        var d = pitch.Dir.Value < 0 ? "-" : "";
        var type = TYPES[pitch.Step ] == 'M' ? IntervalType.Majorable : IntervalType.Perfectable;
        var name = d + num + AltToQ(type, pitch.Alt);
        return name;
    }

    private static Quality AltToQ(IntervalType type, int alt)
    {
        if (alt == 0)
        {
            return type == IntervalType.Majorable ? new Quality("M") : new Quality("P");
        }

        if (alt == -1 && type == IntervalType.Majorable)
        {
            return new Quality("m");
        }

        if (alt > 0)
        {
            return new Quality(new string('A', alt));
        }

        return new Quality(new string('d', type == IntervalType.Perfectable ? alt : alt + 1));
    }

    /// <summary>
    /// Get name of an interval
    /// </summary>
    /// <param name="v">The interval string</param>
    /// <example>
    /// Interval.Name("4P") // => "4P"
    /// Interval.Name("P4") // => "4P"
    /// Interval.Name("C4") // => ""
    /// </example>
    /// <returns>The name of the interval</returns>
    public static string Name(string v) => Get(v).Name;

    /// <summary>
    /// Get number of an interval
    /// </summary>
    /// <param name="v">The interval string</param>
    /// <example>
    /// Interval.Num("4P") // => 4
    /// </example>
    /// <returns>The number of the interval</returns>
    public static int Num(string v) => Get(v).Num;

    /// <summary>
    /// Get quality of an interval
    /// </summary>
    /// <param name="v">The interval string</param>
    /// <example>
    /// Interval.Quality("4P") // => "P"
    /// </example>
    /// <returns>The quality of the interval</returns>
    public static string Quality(string v) => Get(v).Q; 

    /// <summary>
    /// Get semitones of an interval
    /// </summary>
    /// <param name="v">The interval string</param>
    /// <example>
    /// Interval.Semitones("P4") // => 5
    /// </example>
    /// <returns>The semitones of the interval</returns>
    public static int Semitones(string v) => Get(v).Semitones;

    /// <summary>
    /// Find the interval distance between two notes.
    /// </summary>
    /// <param name="from">The note to calculate distance from</param>
    /// <param name="to">The note to calculate distance to</param>
    /// <returns>The interval name or empty string if not valid notes</returns>
    public static string Distance(NoteInfo from, NoteInfo to)
    {
        if (from.Empty || to.Empty)
            return "";

        var fcoord = from.Coord as PitchClassCoordinates;
        var tcoord = to.Coord as PitchClassCoordinates;

        int fifths = tcoord.Fifths - fcoord.Fifths;
        int octs = fcoord is NoteCoordinates fc && tcoord is NoteCoordinates tc
            ? tc.Octaves - fc.Octaves
            : -(int)Math.Floor((fifths * 7.0) / 12);

        bool forceDescending =
            to.Height == from.Height &&
            to.Midi != null &&
            from.Octave == to.Octave &&
            from.Step > to.Step;

        return CoordToInterval(
            new NoteCoordinates { Fifths = fifths, Octaves = octs }, 
            forceDescending).Name;
    }

    /// <summary>
    /// Find the interval distance between two notes by name.
    /// </summary>
    /// <param name="fromNote">The note name to calculate distance from</param>
    /// <param name="toNote">The note name to calculate distance to</param>
    /// <returns>The interval name or empty string if not valid notes</returns>
    public static string Distance(string fromNote, string toNote) =>
        Distance(Note.Get(fromNote), Note.Get(toNote));

    /// <summary>
    /// Get the simplified version of an interval.
    /// </summary>
    /// <param name="name">The interval to simplify</param>
    /// <returns>The simplified interval</returns>
    /// <example>
    /// Interval.Simplify("9M")  // => "2M"
    /// Interval.Simplify("2M")  // => "2M"
    /// Interval.Simplify("-2M") // => "7m"
    /// new[] { "8P", "9M", "10M", "11P", "12P", "13M", "14M", "15P" }.Select(Interval.Simplify)
    /// // => [ "8P", "2M", "3M", "4P", "5P", "6M", "7M", "8P" ]
    /// </example>
    public static string Simplify(string name)
    {
        var i = Get(name);
        return i.Empty ? "" : i.Simple + i.Q;
    }

    /// <summary>
    /// Get the inversion of an interval.
    /// See https://en.wikipedia.org/wiki/Inversion_(music)#Intervals
    /// </summary>
    /// <param name="name">The interval to invert in interval shorthand notation</param>
    /// <returns>The inverted interval</returns>
    /// <example>
    /// Interval.Invert("3m") // => "6M"
    /// Interval.Invert("2M") // => "7m"
    /// </example>
    public static string Invert(string name)
    {
        var i = Get(name);
        if (i.Empty)
            return "";

        int step = (7 - i.Step) % 7;
        int alt = i.Type == "perfectable" ? -i.Alt : -(i.Alt + 1);
        return Get(new PitchInfo { Step = step, Alt = alt, Oct = i.Oct, Dir = i.Dir }).Name;
    }

    /// <summary>
    /// Get interval name from semitones number. Since there are several interval
    /// names for the same number, the name is arbitrary, but deterministic.
    /// </summary>
    /// <param name="semitones">The number of semitones (can be negative)</param>
    /// <returns>The interval name</returns>
    /// <example>
    /// Interval.FromSemitones(7)  // => "5P"
    /// Interval.FromSemitones(-7) // => "-5P"
    /// </example>
    public static string FromSemitones(int semitones)
    {
        int d = semitones < 0 ? -1 : 1;
        int n = Math.Abs(semitones);
        int c = n % 12;
        int o = (int)Math.Floor(n / 12.0);
        return $"{d * (IN[c] + 7 * o)}{IQ[c]}";
    }

    /// <summary>
    /// Adds two intervals.
    /// </summary>
    /// <param name="interval1">The first interval</param>
    /// <param name="interval2">The second interval</param>
    /// <returns>The added interval name</returns>
    /// <example>
    /// Interval.Add("3m", "5P") // => "7m"
    /// </example>
    public static readonly Func<string, string, string?> Add =
        Combinator((a, b) => new NoteCoordinates { Fifths = a.Fifths + b.Fifths, Octaves = a.Octaves + b.Octaves});

    /// <summary>
    /// Returns a function that adds an interval to a given interval.
    /// </summary>
    /// <param name="interval">The base interval</param>
    /// <returns>A function that adds the base interval to another</returns>
    /// <example>
    /// new[] { "1P", "2M", "3M" }.Select(Interval.AddTo("5P")) // => ["5P", "6M", "7M"]
    /// </example>
    public static Func<string, string?> AddTo(string interval) =>
        other => Add(interval, other);

    /// <summary>
    /// Subtracts two intervals.
    /// </summary>
    /// <param name="minuendInterval">The interval to subtract from</param>
    /// <param name="subtrahendInterval">The interval to subtract</param>
    /// <returns>The subtracted interval name</returns>
    /// <example>
    /// Interval.Subtract("5P", "3M") // => "3m"
    /// Interval.Subtract("3M", "5P") // => "-3m"
    /// </example>
    public static readonly Func<string, string, string?> Subtract =
        Combinator((a, b) => new NoteCoordinates { Fifths = a.Fifths - b.Fifths, Octaves = a.Octaves - b.Octaves });

    /// <summary>
    /// Transposes an interval by a number of fifths.
    /// </summary>
    /// <param name="interval">The interval to transpose</param>
    /// <param name="fifths">The number of fifths to transpose by</param>
    /// <returns>The transposed interval name</returns>
    public static string TransposeFifths(string interval, int fifths)
    {
        var ivl = Get(interval);
        if (ivl.Empty) 
            return "";
        int nFifths = ivl.Coord.Fifths;
        int nOcts = ivl.Coord.Octaves;
        int dir = ivl.Coord.Direction == Direction.Descending ? -1 : 1;
        return CoordToInterval(new IntervalCoordinates { Fifths = nFifths + fifths, Octaves = nOcts, Direction = dir == -1 ? Direction.Descending : Direction.Ascending }).Name;
    }

    private delegate NoteCoordinates Operation(IntervalCoordinates a, IntervalCoordinates b);

    private static Func<string, string, string?> Combinator(Operation fn)
    {
        return (a, b) =>
        {
            var coordA = Get(a).Coord;
            var coordB = Get(b).Coord;
            if (coordA != null && coordB != null)
            {
                var coord = fn(coordA, coordB);
                return CoordToInterval(coord).Name;
            }
            return null;
        };
    }
}

public record IntervalInfo
{
    public bool Empty { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Num { get; init; }
    public string Q { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Step { get; init; }
    public int Alt { get; init; }
    public Direction Dir { get; init; }
    public int Simple { get; init; }
    public int Semitones { get; init; }
    public int Chroma { get; init; }
    public IntervalCoordinates Coord { get; init; } = new IntervalCoordinates();
    public int Oct { get; init; }

    public static readonly IntervalInfo NoInterval = new() { Empty = true };
}

public class Quality
{
    private static readonly HashSet<string> ValidQualities =
    [
        "dddd",
        "ddd",
        "dd",
        "d",
        "m",
        "M",
        "P",
        "A",
        "AA",
        "AAA",
        "AAAA"
    ];

    private readonly string _value;

    public Quality(string value)
    {
        if (!ValidQualities.Contains(value))
        {
            throw new ArgumentException($"Invalid quality: {value}");
        }
        _value = value;
    }

    public static implicit operator string(Quality q) => q._value;
}