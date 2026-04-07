namespace Tonal.Core;

/// <summary>
/// Provides access to the chord type dictionary — a catalogue of named chord patterns
/// defined by their intervals. Each entry can be looked up by name, alias, chroma string,
/// or set number.
/// Ported from Tonal.js ChordType module: https://github.com/tonaljs/tonal/blob/main/packages/chord-type/index.ts
/// </summary>
public static class ChordType
{
    // Indexed by name, alias, chroma string, and set number (as string)
    private static readonly Dictionary<string, ChordTypeInfo> _index = [];
    private static readonly List<ChordTypeInfo> _dictionary = [];

    static ChordType()
    {
        foreach (var row in RawData)
        {
            var intervals = row[0].Split(' ');
            var name      = row[1];
            // aliases are space-separated in a single string at row[2]
            var aliases   = row.Length > 2
                ? row[2].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                : [];
            Add(intervals, name, aliases);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the chord type for a given name, alias, chroma string, or set number.
    /// Returns <see cref="ChordTypeInfo.Empty"/> if not found.
    /// </summary>
    /// <example>ChordType.Get("major seventh")  // => { Name: "major seventh", Aliases: ["maj7", ...] }</example>
    /// <example>ChordType.Get("maj7")            // => same (alias lookup)</example>
    /// <example>ChordType.Get("100010010001")    // => same (chroma lookup)</example>
    public static ChordTypeInfo Get(string nameOrAliasOrChroma) =>
        _index.TryGetValue(nameOrAliasOrChroma, out var result) ? result : ChordTypeInfo.Empty;

    /// <inheritdoc cref="Get(string)"/>
    public static ChordTypeInfo Get(int setNum) =>
        _index.TryGetValue(setNum.ToString(), out var result) ? result : ChordTypeInfo.Empty;

    /// <summary>
    /// Returns all canonical chord names (excludes aliases and unnamed legacy chords).
    /// </summary>
    public static IReadOnlyList<string> Names() =>
        _dictionary.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name).ToList();

    /// <summary>
    /// Returns all chord types in the dictionary, including unnamed legacy chords.
    /// </summary>
    public static IReadOnlyList<ChordTypeInfo> All() => _dictionary.AsReadOnly();

    /// <summary>
    /// Returns all keys used to index the dictionary (names, aliases, chromas, set numbers).
    /// </summary>
    public static IReadOnlyList<string> Keys() => [.. _index.Keys];

    /// <summary>
    /// Adds a custom chord type to the dictionary.
    /// </summary>
    /// <param name="intervals">Array of interval names (e.g. ["1P", "3M", "5P"])</param>
    /// <param name="name">The canonical name (may be empty for legacy/alias-only chords)</param>
    /// <param name="aliases">Alternative short symbols (e.g. ["M", "^", "maj"])</param>
    public static ChordTypeInfo Add(string[] intervals, string name, string[]? aliases = null)
    {
        aliases ??= [];
        var pcset   = PitchClassSet.Get(IntervalsToNotes(intervals));
        var quality = DeriveQuality(intervals);

        var chord = new ChordTypeInfo
        {
            Name      = name,
            Intervals = intervals,
            Aliases   = aliases,
            Quality   = quality,
            Chroma    = pcset.Chroma,
            SetNum    = pcset.Num,
            Length    = pcset.Length,
        };

        _dictionary.Add(chord);

        if (!string.IsNullOrEmpty(name))
            _index[name] = chord;

        _index[chord.SetNum.ToString()] = chord;
        _index[chord.Chroma]            = chord;

        foreach (var alias in aliases)
            if (!string.IsNullOrEmpty(alias))
                _index[alias] = chord;

        return chord;
    }

    /// <summary>
    /// Clears all entries from the dictionary.
    /// </summary>
    public static void RemoveAll()
    {
        _dictionary.Clear();
        _index.Clear();
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private static IEnumerable<string> IntervalsToNotes(string[] intervals) =>
        intervals.Select(ivl => Note.Transpose("C", ivl)).Where(n => !string.IsNullOrEmpty(n));

    /// <summary>
    /// Derives the chord quality from its interval set.
    /// Rules: third determines major/minor; fifth determines augmented/diminished.
    /// Chords with no third fall back to the fifth, then Unknown.
    /// </summary>
    private static ChordQuality DeriveQuality(string[] intervals)
    {
        if (intervals.Contains("3M"))
        {
            if (intervals.Contains("5A")) return ChordQuality.Augmented;
            return ChordQuality.Major;
        }
        if (intervals.Contains("3m"))
        {
            if (intervals.Contains("5d")) return ChordQuality.Diminished;
            if (intervals.Contains("5A")) return ChordQuality.Augmented;
            return ChordQuality.Minor;
        }
        if (intervals.Contains("5d")) return ChordQuality.Diminished;
        if (intervals.Contains("5A")) return ChordQuality.Augmented;
        return ChordQuality.Unknown;
    }

    // -------------------------------------------------------------------------
    // Raw data — ported directly from packages/chord-type/data.ts
    // Format: ["intervals", "full name", "alias1 alias2 ..."]
    // -------------------------------------------------------------------------
    private static readonly string[][] RawData =
    [
        // Major
        ["1P 3M 5P",                "major",                             "M ^ maj"],
        ["1P 3M 5P 7M",             "major seventh",                     "maj7 Δ ma7 M7 Maj7 ^7"],
        ["1P 3M 5P 7M 9M",          "major ninth",                       "maj9 Δ9 ^9"],
        ["1P 3M 5P 7M 9M 13M",      "major thirteenth",                  "maj13 Maj13 ^13"],
        ["1P 3M 5P 6M",             "sixth",                             "6 add6 add13 M6"],
        ["1P 3M 5P 6M 9M",          "sixth added ninth",                 "6add9 6/9 69 M69"],
        ["1P 3M 6m 7M",             "major seventh flat sixth",          "M7b6 ^7b6"],
        ["1P 3M 5P 7M 11A",         "major seventh sharp eleventh",      "maj#4 Δ#4 Δ#11 M7#11 ^7#11 maj7#11"],
        // Minor
        ["1P 3m 5P",                "minor",                             "m min -"],
        ["1P 3m 5P 7m",             "minor seventh",                     "m7 min7 mi7 -7"],
        ["1P 3m 5P 7M",             "minor/major seventh",               "m/ma7 m/maj7 mM7 mMaj7 m/M7 -Δ7 mΔ -^7 -maj7"],
        ["1P 3m 5P 6M",             "minor sixth",                       "m6 -6"],
        ["1P 3m 5P 7m 9M",          "minor ninth",                       "m9 -9"],
        ["1P 3m 5P 7M 9M",          "minor/major ninth",                 "mM9 mMaj9 -^9"],
        ["1P 3m 5P 7m 9M 11P",      "minor eleventh",                    "m11 -11"],
        ["1P 3m 5P 7m 9M 13M",      "minor thirteenth",                  "m13 -13"],
        // Diminished
        ["1P 3m 5d",                "diminished",                        "dim ° o"],
        ["1P 3m 5d 7d",             "diminished seventh",                "dim7 °7 o7"],
        ["1P 3m 5d 7m",             "half-diminished",                   "m7b5 ø -7b5 h7 h"],
        // Dominant/Seventh
        ["1P 3M 5P 7m",             "dominant seventh",                  "7 dom"],
        ["1P 3M 5P 7m 9M",          "dominant ninth",                    "9"],
        ["1P 3M 5P 7m 9M 13M",      "dominant thirteenth",               "13"],
        ["1P 3M 5P 7m 11A",         "lydian dominant seventh",           "7#11 7#4"],
        // Altered dominant
        ["1P 3M 5P 7m 9m",          "dominant flat ninth",               "7b9"],
        ["1P 3M 5P 7m 9A",          "dominant sharp ninth",              "7#9"],
        ["1P 3M 7m 9m",             "altered",                           "alt7"],
        // Suspended
        ["1P 4P 5P",                "suspended fourth",                  "sus4 sus"],
        ["1P 2M 5P",                "suspended second",                  "sus2"],
        ["1P 4P 5P 7m",             "suspended fourth seventh",          "7sus4 7sus"],
        ["1P 5P 7m 9M 11P",         "eleventh",                          "11"],
        ["1P 4P 5P 7m 9m",          "suspended fourth flat ninth",       "b9sus phryg 7b9sus 7b9sus4"],
        // Other
        ["1P 5P",                   "fifth",                             "5"],
        ["1P 3M 5A",                "augmented",                         "aug + +5 ^#5"],
        ["1P 3m 5A",                "minor augmented",                   "m#5 -#5 m+"],
        ["1P 3M 5A 7M",             "augmented seventh",                 "maj7#5 maj7+5 +maj7 ^7#5"],
        ["1P 3M 5P 7M 9M 11A",      "major sharp eleventh (lydian)",     "maj9#11 Δ9#11 ^9#11"],
        // Legacy / alias-only (empty name)
        ["1P 2M 4P 5P",             "",  "sus24 sus4add9"],
        ["1P 3M 5A 7M 9M",          "",  "maj9#5 Maj9#5"],
        ["1P 3M 5A 7m",             "",  "7#5 +7 7+ 7aug aug7"],
        ["1P 3M 5A 7m 9A",          "",  "7#5#9 7#9#5 7alt"],
        ["1P 3M 5A 7m 9M",          "",  "9#5 9+"],
        ["1P 3M 5A 7m 9M 11A",      "",  "9#5#11"],
        ["1P 3M 5A 7m 9m",          "",  "7#5b9 7b9#5"],
        ["1P 3M 5A 7m 9m 11A",      "",  "7#5b9#11"],
        ["1P 3M 5A 9A",             "",  "+add#9"],
        ["1P 3M 5A 9M",             "",  "M#5add9 +add9"],
        ["1P 3M 5P 6M 11A",         "",  "M6#11 M6b5 6#11 6b5"],
        ["1P 3M 5P 6M 7M 9M",       "",  "M7add13"],
        ["1P 3M 5P 6M 9M 11A",      "",  "69#11"],
        ["1P 3m 5P 6M 9M",          "",  "m69 -69"],
        ["1P 3M 5P 6m 7m",          "",  "7b6"],
        ["1P 3M 5P 7M 9A 11A",      "",  "maj7#9#11"],
        ["1P 3M 5P 7M 9M 11A 13M",  "",  "M13#11 maj13#11 M13+4 M13#4"],
        ["1P 3M 5P 7M 9m",          "",  "M7b9"],
        ["1P 3M 5P 7m 11A 13m",     "",  "7#11b13 7b5b13"],
        ["1P 3M 5P 7m 13M",         "",  "7add6 67 7add13"],
        ["1P 3M 5P 7m 9A 11A",      "",  "7#9#11 7b5#9 7#9b5"],
        ["1P 3M 5P 7m 9A 11A 13M",  "",  "13#9#11"],
        ["1P 3M 5P 7m 9A 11A 13m",  "",  "7#9#11b13"],
        ["1P 3M 5P 7m 9A 13M",      "",  "13#9"],
        ["1P 3M 5P 7m 9A 13m",      "",  "7#9b13"],
        ["1P 3M 5P 7m 9M 11A",      "",  "9#11 9+4 9#4"],
        ["1P 3M 5P 7m 9M 11A 13M",  "",  "13#11 13+4 13#4"],
        ["1P 3M 5P 7m 9M 11A 13m",  "",  "9#11b13 9b5b13"],
        ["1P 3M 5P 7m 9m 11A",      "",  "7b9#11 7b5b9 7b9b5"],
        ["1P 3M 5P 7m 9m 11A 13M",  "",  "13b9#11"],
        ["1P 3M 5P 7m 9m 11A 13m",  "",  "7b9b13#11 7b9#11b13 7b5b9b13"],
        ["1P 3M 5P 7m 9m 13M",      "",  "13b9"],
        ["1P 3M 5P 7m 9m 13m",      "",  "7b9b13"],
        ["1P 3M 5P 7m 9m 9A",       "",  "7b9#9"],
        ["1P 3M 5P 9M",             "",  "Madd9 2 add9 add2"],
        ["1P 3M 5P 9m",             "",  "Maddb9"],
        ["1P 3M 5d",                "",  "Mb5"],
        ["1P 3M 5d 6M 7m 9M",       "",  "13b5"],
        ["1P 3M 5d 7M",             "",  "M7b5"],
        ["1P 3M 5d 7M 9M",          "",  "M9b5"],
        ["1P 3M 5d 7m",             "",  "7b5"],
        ["1P 3M 5d 7m 9M",          "",  "9b5"],
        ["1P 3M 7m",                "",  "7no5"],
        ["1P 3M 7m 13m",            "",  "7b13"],
        ["1P 3M 7m 9M",             "",  "9no5"],
        ["1P 3M 7m 9M 13M",         "",  "13no5"],
        ["1P 3M 7m 9M 13m",         "",  "9b13"],
        ["1P 3m 4P 5P",             "",  "madd4"],
        ["1P 3m 5P 6m 7M",          "",  "mMaj7b6"],
        ["1P 3m 5P 6m 7M 9M",       "",  "mMaj9b6"],
        ["1P 3m 5P 7m 11P",         "",  "m7add11 m7add4"],
        ["1P 3m 5P 9M",             "",  "madd9"],
        ["1P 3m 5d 6M 7M",          "",  "o7M7"],
        ["1P 3m 5d 7M",             "",  "oM7"],
        ["1P 3m 6m 7M",             "",  "mb6M7"],
        ["1P 3m 6m 7m",             "",  "m7#5"],
        ["1P 3m 6m 7m 9M",          "",  "m9#5"],
        ["1P 3m 5A 7m 9M 11P",      "",  "m11A"],
        ["1P 3m 6m 9m",             "",  "mb6b9"],
        ["1P 2M 3m 5d 7m",          "",  "m9b5"],
        ["1P 4P 5A 7M",             "",  "M7#5sus4"],
        ["1P 4P 5A 7M 9M",          "",  "M9#5sus4"],
        ["1P 4P 5A 7m",             "",  "7#5sus4"],
        ["1P 4P 5P 7M",             "",  "M7sus4"],
        ["1P 4P 5P 7M 9M",          "",  "M9sus4"],
        ["1P 4P 5P 7m 9M",          "",  "9sus4 9sus"],
        ["1P 4P 5P 7m 9M 13M",      "",  "13sus4 13sus"],
        ["1P 4P 5P 7m 9m 13m",      "",  "7sus4b9b13 7b9b13sus4"],
        ["1P 4P 7m 10m",            "",  "4 quartal"],
        ["1P 5P 7m 9m 11P",         "",  "11b9"],
    ];
}

/// <summary>
/// The quality category of a chord.
/// </summary>
public enum ChordQuality
{
    Major,
    Minor,
    Augmented,
    Diminished,
    Unknown
}

/// <summary>
/// Represents a single entry in the chord type dictionary.
/// </summary>
public record ChordTypeInfo
{
    /// <summary>The canonical full name (e.g. "major seventh"). Empty for legacy alias-only entries.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The interval pattern from the root (e.g. ["1P", "3M", "5P", "7M"]).</summary>
    public string[] Intervals { get; init; } = [];

    /// <summary>Short symbol aliases (e.g. ["maj7", "Δ", "M7"]).</summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>The quality category: Major, Minor, Augmented, Diminished, or Unknown.</summary>
    public ChordQuality Quality { get; init; }

    /// <summary>12-character binary chroma string.</summary>
    public string Chroma { get; init; } = string.Empty;

    /// <summary>Integer set number (0–4095) derived from the chroma.</summary>
    public int SetNum { get; init; }

    /// <summary>Number of notes in the chord.</summary>
    public int Length { get; init; }

    /// <summary>True if this is the sentinel empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel value returned when a lookup yields no result.</summary>
    public static readonly ChordTypeInfo Empty = new() { IsEmpty = true };
}
