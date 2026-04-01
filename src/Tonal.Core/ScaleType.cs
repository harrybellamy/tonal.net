namespace Tonal.Core;

/// <summary>
/// Provides access to the scale type dictionary — a catalogue of named scale patterns
/// defined by their intervals. Each entry can be looked up by name, alias, chroma string,
/// or set number.
/// Ported from Tonal.js ScaleType module: https://github.com/tonaljs/tonal/blob/main/packages/scale-type/index.ts
/// </summary>
public static class ScaleType
{
    // Keyed by name, alias, chroma string, and set number (as string)
    private static readonly Dictionary<string, ScaleTypeInfo> _index = [];
    private static readonly List<ScaleTypeInfo> _dictionary = [];

    static ScaleType()
    {
        foreach (var row in RawData)
        {
            var intervals = row[0].Split(' ');
            var name      = row[1];
            var aliases   = row.Skip(2).ToArray();
            Add(intervals, name, aliases);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the scale type for a given name, alias, chroma string, or set number.
    /// Returns <see cref="ScaleTypeInfo.Empty"/> if not found.
    /// </summary>
    /// <example>ScaleType.Get("major")           // => { Name: "major", Intervals: ["1P","2M",...] }</example>
    /// <example>ScaleType.Get("ionian")           // => same as "major" (alias)</example>
    /// <example>ScaleType.Get("101011010101")     // => same as "major" (chroma)</example>
    /// <example>ScaleType.Get(2773)               // => same as "major" (set number)</example>
    public static ScaleTypeInfo Get(string nameOrChromaOrAlias) =>
        _index.TryGetValue(nameOrChromaOrAlias, out var result) ? result : ScaleTypeInfo.Empty;

    /// <inheritdoc cref="Get(string)"/>
    public static ScaleTypeInfo Get(int setNum) =>
        _index.TryGetValue(setNum.ToString(), out var result) ? result : ScaleTypeInfo.Empty;

    /// <summary>
    /// Returns all canonical scale names (excludes aliases).
    /// </summary>
    /// <example>ScaleType.Names() // => ["major pentatonic", "major", "minor", ...]</example>
    public static IReadOnlyList<string> Names() =>
        _dictionary.Select(s => s.Name).ToList();

    /// <summary>
    /// Returns all scale types in the dictionary.
    /// </summary>
    public static IReadOnlyList<ScaleTypeInfo> All() => _dictionary.AsReadOnly();

    /// <summary>
    /// Returns all keys used to index the dictionary (names, aliases, chromas, set numbers).
    /// </summary>
    public static IReadOnlyList<string> Keys() => [.. _index.Keys];

    /// <summary>
    /// Adds a custom scale type to the dictionary.
    /// </summary>
    /// <param name="intervals">Array of interval names (e.g. ["1P", "2M", "3M", "4P", "5P", "6M", "7M"])</param>
    /// <param name="name">The canonical name for the scale</param>
    /// <param name="aliases">Optional alternative names</param>
    /// <returns>The created <see cref="ScaleTypeInfo"/></returns>
    public static ScaleTypeInfo Add(string[] intervals, string name, string[]? aliases = null)
    {
        aliases ??= [];
        var pcset  = PitchClassSet.Get(IntervalsToNotes(intervals));
        var scale  = new ScaleTypeInfo
        {
            Name      = name,
            Intervals = intervals,
            Aliases   = aliases,
            Chroma    = pcset.Chroma,
            SetNum    = pcset.Num,
            Length    = pcset.Length,
        };

        _dictionary.Add(scale);
        _index[name]               = scale;
        _index[scale.SetNum.ToString()] = scale;
        _index[scale.Chroma]       = scale;
        foreach (var alias in aliases)
            _index[alias] = scale;

        return scale;
    }

    /// <summary>
    /// Clears all entries from the dictionary. Useful for testing or custom configurations.
    /// </summary>
    public static void RemoveAll()
    {
        _dictionary.Clear();
        _index.Clear();
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts an array of interval names to note names (by transposing from C),
    /// so we can derive the chroma via PitchClassSet.
    /// </summary>
    private static IEnumerable<string> IntervalsToNotes(string[] intervals) =>
        intervals.Select(ivl => Note.Transpose("C", ivl)).Where(n => !string.IsNullOrEmpty(n));

    // -------------------------------------------------------------------------
    // Raw data — ported directly from packages/scale-type/data.ts
    // Format: [ "intervals", "name", "alias1", "alias2", ... ]
    // -------------------------------------------------------------------------
    private static readonly string[][] RawData =
    [
        // Basic scales
        ["1P 2M 3M 5P 6M",             "major pentatonic",         "pentatonic"],
        ["1P 2M 3M 4P 5P 6M 7M",       "major",                    "ionian"],
        ["1P 2M 3m 4P 5P 6m 7m",       "minor",                    "aeolian"],
        // Jazz common scales
        ["1P 2M 3m 3M 5P 6M",          "major blues"],
        ["1P 3m 4P 5d 5P 7m",          "minor blues",              "blues"],
        ["1P 2M 3m 4P 5P 6M 7M",       "melodic minor"],
        ["1P 2M 3m 4P 5P 6m 7M",       "harmonic minor"],
        ["1P 2M 3M 4P 5P 6M 7m 7M",    "bebop"],
        ["1P 2M 3m 4P 5d 6m 6M 7M",    "diminished",               "whole-half diminished"],
        // Modes
        ["1P 2M 3m 4P 5P 6M 7m",       "dorian"],
        ["1P 2M 3M 4A 5P 6M 7M",       "lydian"],
        ["1P 2M 3M 4P 5P 6M 7m",       "mixolydian",               "dominant"],
        ["1P 2m 3m 4P 5P 6m 7m",       "phrygian"],
        ["1P 2m 3m 4P 5d 6m 7m",       "locrian"],
        // 5-note scales
        ["1P 3M 4P 5P 7M",             "ionian pentatonic"],
        ["1P 3M 4P 5P 7m",             "mixolydian pentatonic",    "indian"],
        ["1P 2M 4P 5P 6M",             "ritusen"],
        ["1P 2M 4P 5P 7m",             "egyptian"],
        ["1P 3M 4P 5d 7m",             "neopolitan major pentatonic"],
        ["1P 3m 4P 5P 6m",             "vietnamese 1"],
        ["1P 2m 3m 5P 6m",             "pelog"],
        ["1P 2m 4P 5P 6m",             "kumoijoshi"],
        ["1P 2M 3m 5P 6m",             "hirajoshi"],
        ["1P 2m 4P 5d 7m",             "iwato"],
        ["1P 2m 4P 5P 7m",             "in-sen"],
        ["1P 3M 4A 5P 7M",             "lydian pentatonic",        "chinese"],
        ["1P 3m 4P 6m 7m",             "malkos raga"],
        ["1P 3m 4P 5d 7m",             "locrian pentatonic",       "minor seven flat five pentatonic"],
        ["1P 3m 4P 5P 7m",             "minor pentatonic",         "vietnamese 2"],
        ["1P 3m 4P 5P 6M",             "minor six pentatonic"],
        ["1P 2M 3m 5P 6M",             "flat three pentatonic",    "kumoi"],
        ["1P 2M 3M 5P 6m",             "flat six pentatonic"],
        ["1P 2m 3M 5P 6M",             "scriabin"],
        ["1P 3M 5d 6m 7m",             "whole tone pentatonic"],
        ["1P 3M 4A 5A 7M",             "lydian #5P pentatonic"],
        ["1P 3M 4A 5P 7m",             "lydian dominant pentatonic"],
        ["1P 3m 4P 5P 7M",             "minor #7M pentatonic"],
        ["1P 3m 4d 5d 7m",             "super locrian pentatonic"],
        // 6-note scales
        ["1P 2M 3m 4P 5P 7M",          "minor hexatonic"],
        ["1P 2A 3M 5P 5A 7M",          "augmented"],
        ["1P 2M 4P 5P 6M 7m",          "piongio"],
        ["1P 2m 3M 4A 6M 7m",          "prometheus neopolitan"],
        ["1P 2M 3M 4A 6M 7m",          "prometheus"],
        ["1P 2m 3M 5d 6m 7m",          "mystery #1"],
        ["1P 2m 3M 4P 5A 6M",          "six tone symmetric"],
        ["1P 2M 3M 4A 5A 6A",          "whole tone",               "messiaen's mode #1"],
        ["1P 2m 4P 4A 5P 7M",          "messiaen's mode #5"],
        // 7-note scales
        ["1P 2M 3M 4P 5d 6m 7m",       "locrian major",            "arabian"],
        ["1P 2m 3M 4A 5P 6m 7M",       "double harmonic lydian"],
        ["1P 2m 2A 3M 4A 6m 7m",       "altered",                  "super locrian", "diminished whole tone", "pomeroy"],
        ["1P 2M 3m 4P 5d 6m 7m",       "locrian #2",               "half-diminished", "aeolian b5"],
        ["1P 2M 3M 4P 5P 6m 7m",       "mixolydian b6",            "melodic minor fifth mode", "hindu"],
        ["1P 2M 3M 4A 5P 6M 7m",       "lydian dominant",          "lydian b7", "overtone"],
        ["1P 2M 3M 4A 5A 6M 7M",       "lydian augmented"],
        ["1P 2m 3m 4P 5P 6M 7m",       "dorian b2",                "phrygian #6", "melodic minor second mode"],
        ["1P 2m 3m 4d 5d 6m 7d",       "ultralocrian",             "superlocrian bb7", "superlocrian diminished"],
        ["1P 2m 3m 4P 5d 6M 7m",       "locrian 6",                "locrian natural 6", "locrian sharp 6"],
        ["1P 2A 3M 4P 5P 5A 7M",       "augmented heptatonic"],
        ["1P 2M 3m 4A 5P 6M 7m",       "dorian #4",                "ukrainian dorian", "romanian minor", "altered dorian"],
        ["1P 2M 3m 4A 5P 6M 7M",       "lydian diminished"],
        ["1P 2M 3M 4A 5A 7m 7M",       "leading whole tone"],
        ["1P 2M 3M 4A 5P 6m 7m",       "lydian minor"],
        ["1P 2m 3M 4P 5P 6m 7m",       "phrygian dominant",        "spanish", "phrygian major"],
        ["1P 2m 3m 4P 5P 6m 7M",       "balinese"],
        ["1P 2m 3m 4P 5P 6M 7M",       "neopolitan major"],
        ["1P 2M 3M 4P 5P 6m 7M",       "harmonic major"],
        ["1P 2m 3M 4P 5P 6m 7M",       "double harmonic major",    "gypsy"],
        ["1P 2M 3m 4A 5P 6m 7M",       "hungarian minor"],
        ["1P 2A 3M 4A 5P 6M 7m",       "hungarian major"],
        ["1P 2m 3M 4P 5d 6M 7m",       "oriental"],
        ["1P 2m 3m 3M 4A 5P 7m",       "flamenco"],
        ["1P 2m 3m 4A 5P 6m 7M",       "todi raga"],
        ["1P 2m 3M 4P 5d 6m 7M",       "persian"],
        ["1P 2m 3M 5d 6m 7m 7M",       "enigmatic"],
        ["1P 2M 3M 4P 5A 6M 7M",       "major augmented",          "major #5", "ionian augmented", "ionian #5"],
        ["1P 2A 3M 4A 5P 6M 7M",       "lydian #9"],
        // 8-note scales
        ["1P 2m 2M 4P 4A 5P 6m 7M",    "messiaen's mode #4"],
        ["1P 2m 3M 4P 4A 5P 6m 7M",    "purvi raga"],
        ["1P 2m 3m 3M 4P 5P 6m 7m",    "spanish heptatonic"],
        ["1P 2M 3m 3M 4P 5P 6M 7m",    "bebop minor"],
        ["1P 2M 3M 4P 5P 5A 6M 7M",    "bebop major"],
        ["1P 2m 3m 4P 5d 5P 6m 7m",    "bebop locrian"],
        ["1P 2M 3m 4P 5P 6m 7m 7M",    "minor bebop"],
        ["1P 2M 3M 4P 5d 5P 6M 7M",    "ichikosucho"],
        ["1P 2M 3m 4P 5P 6m 6M 7M",    "minor six diminished"],
        ["1P 2m 3m 3M 4A 5P 6M 7m",    "half-whole diminished",    "dominant diminished", "messiaen's mode #2"],
        ["1P 3m 3M 4P 5P 6M 7m 7M",    "kafi raga"],
        ["1P 2M 3M 4P 4A 5A 6A 7M",    "messiaen's mode #6"],
        // 9-note scales
        ["1P 2M 3m 3M 4P 5d 5P 6M 7m", "composite blues"],
        ["1P 2M 3m 3M 4A 5P 6m 7m 7M", "messiaen's mode #3"],
        // 10-note scales
        ["1P 2m 2M 3m 4P 4A 5P 6m 6M 7M", "messiaen's mode #7"],
        // 12-note scales
        ["1P 2m 2M 3m 3M 4P 5d 5P 6m 6M 7m 7M", "chromatic"],
    ];
}

/// <summary>
/// Represents a single entry in the scale type dictionary.
/// </summary>
public record ScaleTypeInfo
{
    /// <summary>The canonical name of the scale (e.g. "major", "dorian").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The interval pattern from the root (e.g. ["1P", "2M", "3M", "4P", "5P", "6M", "7M"]).</summary>
    public string[] Intervals { get; init; } = [];

    /// <summary>Alternative names for the scale (e.g. ["ionian"] for "major").</summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>12-character binary chroma string (e.g. "101011010101" for major).</summary>
    public string Chroma { get; init; } = string.Empty;

    /// <summary>Integer set number (0–4095) derived from the chroma.</summary>
    public int SetNum { get; init; }

    /// <summary>Number of notes in the scale.</summary>
    public int Length { get; init; }

    /// <summary>True if this is the sentinel empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel value returned when a lookup yields no result.</summary>
    public static readonly ScaleTypeInfo Empty = new() { IsEmpty = true };
}