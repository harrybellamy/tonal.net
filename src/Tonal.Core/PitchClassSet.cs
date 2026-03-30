namespace Tonal.Core;

/// <summary>
/// Provides functions for working with Pitch Class Sets — unordered collections of
/// pitch classes (notes without octave) represented as chroma strings, set numbers,
/// or note arrays.
/// Ported from Tonal.js Pcset module: https://tonaljs.github.io/tonal/docs/groups/pitch-class-sets
/// </summary>
public static class PitchClassSet
{
    private static readonly Dictionary<string, PitchClassSetInfo> _cache = [];

    /// <summary>
    /// Returns a <see cref="PitchClassSetInfo"/> from a list of note names.
    /// </summary>
    /// <example>PitchClassSet.Get(["C", "D", "E"]) // => { Num: 2688, Chroma: "101010000000", ... }</example>
    public static PitchClassSetInfo Get(IEnumerable<string> notes)
    {
        var chroma = NotesToChroma(notes);
        return GetFromChroma(chroma);
    }

    /// <summary>
    /// Returns a <see cref="PitchClassSetInfo"/> from a chroma string (12-char binary string)
    /// or a set number (0–4095).
    /// </summary>
    /// <example>PitchClassSet.Get("101010000000") // => { Num: 2688, ... }</example>
    /// <example>PitchClassSet.Get(2688) // => { Chroma: "101010000000", ... }</example>
    public static PitchClassSetInfo Get(string chroma)
    {
        if (!IsValidChroma(chroma))
            return PitchClassSetInfo.Empty;
        return GetFromChroma(chroma);
    }

    /// <summary>
    /// Returns a <see cref="PitchClassSetInfo"/> from a set number (0–4095).
    /// </summary>
    public static PitchClassSetInfo Get(int num)
    {
        if (num < 0 || num > 4095)
            return PitchClassSetInfo.Empty;
        var chroma = NumToChroma(num);
        return GetFromChroma(chroma);
    }

    private static PitchClassSetInfo GetFromChroma(string chroma)
    {
        if (_cache.TryGetValue(chroma, out var cached))
            return cached;

        var result = Build(chroma);
        _cache[chroma] = result;
        return result;
    }

    private static PitchClassSetInfo Build(string chroma)
    {
        var num = ChromaToNum(chroma);
        var intervals = ChromaToIntervals(chroma);
        var length = chroma.Count(c => c == '1');

        return new PitchClassSetInfo
        {
            Chroma = chroma,
            Num = num,
            Intervals = intervals,
            Length = length
        };
    }

    // -------------------------------------------------------------------------
    // Shorthands
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the chroma string (12-char binary) for the given notes, chroma, or number.
    /// </summary>
    /// <example>PitchClassSet.Chroma(["C", "D", "E"]) // => "101010000000"</example>
    public static string Chroma(IEnumerable<string> notes) => Get(notes).Chroma;

    /// <inheritdoc cref="Chroma(IEnumerable{string})"/>
    public static string Chroma(string chroma) => Get(chroma).Chroma;

    /// <inheritdoc cref="Chroma(IEnumerable{string})"/>
    public static string Chroma(int num) => Get(num).Chroma;

    /// <summary>
    /// Returns the set number (0–4095) for the given notes, chroma, or number.
    /// </summary>
    /// <example>PitchClassSet.Num(["C", "D", "E"]) // => 2688</example>
    public static int Num(IEnumerable<string> notes) => Get(notes).Num;

    /// <inheritdoc cref="Num(IEnumerable{string})"/>
    public static int Num(string chroma) => Get(chroma).Num;

    /// <inheritdoc cref="Num(IEnumerable{string})"/>
    public static int Num(int num) => Get(num).Num;

    /// <summary>
    /// Returns the intervals from C for the given notes, chroma, or number.
    /// </summary>
    /// <example>PitchClassSet.Intervals(["C", "D", "E"]) // => ["1P", "2M", "3M"]</example>
    public static string[] Intervals(IEnumerable<string> notes) => Get(notes).Intervals;

    /// <inheritdoc cref="Intervals(IEnumerable{string})"/>
    public static string[] Intervals(string chroma) => Get(chroma).Intervals;

    /// <inheritdoc cref="Intervals(IEnumerable{string})"/>
    public static string[] Intervals(int num) => Get(num).Intervals;

    // -------------------------------------------------------------------------
    // Notes
    // -------------------------------------------------------------------------

    /// <summary>
    /// Given a pcset (chroma string, number, or note list), returns the sorted
    /// pitch class note names starting from C.
    /// </summary>
    /// <example>PitchClassSet.Notes(["D3", "A3", "C4"]) // => ["C", "D", "A"]</example>
    /// <example>PitchClassSet.Notes("101010000000") // => ["C", "D", "E"]</example>
    public static string[] Notes(IEnumerable<string> notes) => ChromaToNotes(Get(notes).Chroma);

    /// <inheritdoc cref="Notes(IEnumerable{string})"/>
    public static string[] Notes(string chroma) => ChromaToNotes(Get(chroma).Chroma);

    /// <inheritdoc cref="Notes(IEnumerable{string})"/>
    public static string[] Notes(int num) => ChromaToNotes(Get(num).Chroma);

    // -------------------------------------------------------------------------
    // Querying — curried functions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a function that tests whether a given note is included in the set.
    /// Enharmonic equivalents are treated as included.
    /// </summary>
    /// <example>
    /// var inCTriad = PitchClassSet.IsIncludedIn(["C", "E", "G"]);
    /// inCTriad("C4")  // => true
    /// inCTriad("C#4") // => false
    /// inCTriad("Fb")  // => true  (enharmonic of E)
    /// </example>
    public static Func<string, bool> IsIncludedIn(IEnumerable<string> set)
    {
        var chroma = Get(set).Chroma;
        return note =>
        {
            var noteInfo = Note.Get(note);
            if (noteInfo.Empty) return false;
            return chroma[noteInfo.Chroma] == '1';
        };
    }

    /// <inheritdoc cref="IsIncludedIn(IEnumerable{string})"/>
    public static Func<string, bool> IsIncludedIn(string chroma)
    {
        var info = Get(chroma);
        return note =>
        {
            var noteInfo = Note.Get(note);
            if (noteInfo.Empty) return false;
            return info.Chroma[noteInfo.Chroma] == '1';
        };
    }

    /// <summary>
    /// Returns a function that tests whether a given set is a subset of the parent set.
    /// A set S is a subset of P if every note in S is also in P.
    /// </summary>
    /// <example>
    /// var subsetOfMajor = PitchClassSet.IsSubsetOf(["C", "D", "E", "F", "G", "A", "B"]);
    /// subsetOfMajor(["C", "E", "G"]) // => true  (C major triad ⊆ C major scale)
    /// subsetOfMajor(["C", "D", "Eb"]) // => false (Eb not in C major)
    /// </example>
    public static Func<IEnumerable<string>, bool> IsSubsetOf(IEnumerable<string> parent)
    {
        var parentChroma = Get(parent).Chroma;
        return subset =>
        {
            var subsetChroma = Get(subset).Chroma;
            return IsSubsetChroma(subsetChroma, parentChroma);
        };
    }

    /// <inheritdoc cref="IsSubsetOf(IEnumerable{string})"/>
    public static Func<string, bool> IsSubsetOf(string parentChroma)
    {
        return subsetChroma =>
        {
            var normalised = Get(subsetChroma).Chroma;
            return IsSubsetChroma(normalised, parentChroma);
        };
    }

    /// <summary>
    /// Returns a function that tests whether a given set is a superset of the child set.
    /// A set S is a superset of C if every note in C is also in S.
    /// </summary>
    /// <example>
    /// var supersetOfTriad = PitchClassSet.IsSupersetOf(["C", "E", "G"]);
    /// supersetOfTriad(["C", "D", "E", "F", "G", "A", "B"]) // => true
    /// supersetOfTriad(["C", "E"])                           // => false
    /// </example>
    public static Func<IEnumerable<string>, bool> IsSupersetOf(IEnumerable<string> subset)
    {
        var subsetChroma = Get(subset).Chroma;
        return parent =>
        {
            var parentChroma = Get(parent).Chroma;
            return IsSubsetChroma(subsetChroma, parentChroma);
        };
    }

    /// <inheritdoc cref="IsSupersetOf(IEnumerable{string})"/>
    public static Func<string, bool> IsSupersetOf(string subsetChroma)
    {
        return parentChroma =>
        {
            var normalisedParent = Get(parentChroma).Chroma;
            return IsSubsetChroma(subsetChroma, normalisedParent);
        };
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    // The 12 chromatic pitch classes starting from C
    private static readonly string[] ChromaticNotes =
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    private static string NotesToChroma(IEnumerable<string> notes)
    {
        var bits = new char[12];
        Array.Fill(bits, '0');

        foreach (var note in notes)
        {
            var info = Note.Get(note);
            if (!info.Empty)
                bits[info.Chroma] = '1';
        }

        return new string(bits);
    }

    private static string NumToChroma(int num)
    {
        // num is stored MSB = C (bit 11), so we convert to a 12-char string
        var bits = new char[12];
        for (int i = 0; i < 12; i++)
            bits[i] = ((num >> (11 - i)) & 1) == 1 ? '1' : '0';
        return new string(bits);
    }

    private static int ChromaToNum(string chroma)
    {
        int num = 0;
        for (int i = 0; i < 12; i++)
            if (chroma[i] == '1')
                num |= (1 << (11 - i));
        return num;
    }

    private static string[] ChromaToIntervals(string chroma)
    {
        var intervals = new List<string>();
        for (int i = 0; i < 12; i++)
        {
            if (chroma[i] == '1')
            {
                var interval = Interval.FromSemitones(i);
                if (!string.IsNullOrEmpty(interval))
                    intervals.Add(interval);
            }
        }
        return [.. intervals];
    }

    private static string[] ChromaToNotes(string chroma)
    {
        var notes = new List<string>();
        for (int i = 0; i < 12; i++)
            if (chroma[i] == '1')
                notes.Add(ChromaticNotes[i]);
        return [.. notes];
    }

    private static bool IsSubsetChroma(string subset, string parent)
    {
        // Every '1' in subset must also be '1' in parent
        for (int i = 0; i < 12; i++)
            if (subset[i] == '1' && parent[i] != '1')
                return false;
        return true;
    }

    private static bool IsValidChroma(string chroma) =>
        chroma.Length == 12 && chroma.All(c => c == '0' || c == '1');
}

/// <summary>
/// Represents the properties of a Pitch Class Set.
/// </summary>
public record PitchClassSetInfo
{
    /// <summary>The set as a 12-character binary string, one bit per semitone starting from C.</summary>
    public string Chroma { get; init; } = string.Empty;

    /// <summary>The unique integer representation of the set (0–4095).</summary>
    public int Num { get; init; }

    /// <summary>The intervals from C for each pitch class present in the set.</summary>
    public string[] Intervals { get; init; } = [];

    /// <summary>The number of pitch classes in the set.</summary>
    public int Length { get; init; }

    /// <summary>True if this represents an invalid or empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel value returned for invalid inputs.</summary>
    public static readonly PitchClassSetInfo Empty = new() { IsEmpty = true };
}
