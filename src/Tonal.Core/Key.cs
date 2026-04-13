namespace Tonal.Core;

/// <summary>
/// Provides functions for working with major and minor keys, including scale degrees,
/// diatonic chords, harmonic functions, secondary dominants, and key signatures.
/// Ported from Tonal.js Key module: https://github.com/tonaljs/tonal/blob/main/packages/key/index.ts
/// </summary>
public static class Key
{
    // -------------------------------------------------------------------------
    // Static scale templates (built once, reused for every key)
    // -------------------------------------------------------------------------

    private static readonly KeyScaleTemplate MajorTemplate = new(
        Grades:             ["I",   "II",  "III",  "IV",   "V",   "VI",   "VII"],
        Triads:             ["",    "m",   "m",    "",     "",    "m",    "dim"],
        ChordTypes:         ["maj7","m7",  "m7",   "maj7", "7",   "m7",   "m7b5"],
        HarmonicFunctions:  ["T",   "SD",  "T",    "SD",   "D",   "T",    "D"],
        ChordScales:        ["major","dorian","phrygian","lydian","mixolydian","minor","locrian"]
    );

    private static readonly KeyScaleTemplate NaturalTemplate = new(
        Grades:             ["I",   "II",  "bIII", "IV",  "V",   "bVI",  "bVII"],
        Triads:             ["m",   "dim", "",     "m",   "m",   "",     ""],
        ChordTypes:         ["m7",  "m7b5","maj7", "m7",  "m7",  "maj7", "7"],
        HarmonicFunctions:  ["T",   "SD",  "T",    "SD",  "D",   "SD",   "SD"],
        ChordScales:        ["minor","locrian","major","dorian","phrygian","lydian","mixolydian"]
    );

    private static readonly KeyScaleTemplate HarmonicTemplate = new(
        Grades:             ["I",   "II",  "bIII", "IV",  "V",   "bVI",  "VII"],
        Triads:             ["m",   "dim", "aug",  "m",   "",    "dim",  ""],
        ChordTypes:         ["mMaj7","m7b5","+maj7","m7", "7",   "maj7", "o7"],
        HarmonicFunctions:  ["T",   "SD",  "T",    "SD",  "D",   "SD",   "D"],
        ChordScales:        ["harmonic minor","locrian 6","major augmented","lydian diminished",
                             "phrygian dominant","lydian #9","ultralocrian"]
    );

    private static readonly KeyScaleTemplate MelodicTemplate = new(
        Grades:             ["I",   "II",  "bIII", "IV",  "V",   "VI",    "VII"],
        Triads:             ["m",   "m",   "aug",  "",    "",    "dim",   "dim"],
        ChordTypes:         ["m6",  "m7",  "+maj7","7",   "7",   "m7b5",  "m7b5"],
        HarmonicFunctions:  ["T",   "SD",  "T",    "SD",  "D",   "",      ""],
        ChordScales:        ["melodic minor","dorian b2","lydian augmented","lydian dominant",
                             "mixolydian b6","locrian #2","altered"]
    );

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all properties of a major key for the given tonic.
    /// </summary>
    /// <example>
    /// Key.MajorKey("C")
    /// // => { Tonic: "C", KeySignature: "", Alteration: 0, MinorRelative: "A", ... }
    /// </example>
    public static MajorKeyInfo MajorKey(string tonic)
    {
        var pc = Note.Get(tonic).PitchClass;
        if (string.IsNullOrEmpty(pc)) return MajorKeyInfo.Empty;

        var alteration  = DistInFifths("C", pc);
        var keyScale    = BuildKeyScale(MajorTemplate, pc);

        return new MajorKeyInfo
        {
            Tonic               = pc,
            Alteration          = alteration,
            KeySignature        = AltToAcc(alteration),
            MinorRelative       = Note.Transpose(pc, "-3m"),
            Grades              = keyScale.Grades,
            Intervals           = keyScale.Intervals,
            Scale               = keyScale.Scale,
            Triads              = keyScale.Triads,
            Chords              = keyScale.Chords,
            ChordsHarmonicFunction = keyScale.ChordsHarmonicFunction,
            ChordScales         = keyScale.ChordScales,
            SecondaryDominants  = keyScale.SecondaryDominants,
            SecondaryDominantSupertonics = keyScale.SecondaryDominantSupertonics,
            SubstituteDominants = keyScale.SubstituteDominants,
            SubstituteDominantSupertonics = keyScale.SubstituteDominantSupertonics,
        };
    }

    /// <summary>
    /// Returns all properties of a minor key for the given tonic,
    /// including natural, harmonic, and melodic sub-scales.
    /// </summary>
    /// <example>
    /// Key.MinorKey("A")
    /// // => { Tonic: "A", KeySignature: "", Alteration: 0, RelativeMajor: "C", ... }
    /// </example>
    public static MinorKeyInfo MinorKey(string tonic)
    {
        var pc = Note.Get(tonic).PitchClass;
        if (string.IsNullOrEmpty(pc)) return MinorKeyInfo.Empty;

        var alteration = DistInFifths("C", pc) - 3;

        return new MinorKeyInfo
        {
            Tonic          = pc,
            Alteration     = alteration,
            KeySignature   = AltToAcc(alteration),
            RelativeMajor  = Note.Transpose(pc, "3m"),
            Natural        = BuildKeyScale(NaturalTemplate,  pc),
            Harmonic       = BuildKeyScale(HarmonicTemplate, pc),
            Melodic        = BuildKeyScale(MelodicTemplate,  pc),
        };
    }

    /// <summary>
    /// Given a key signature string (e.g. "###", "bb") or alteration number,
    /// returns the tonic of the corresponding major key.
    /// </summary>
    /// <example>Key.MajorTonicFromKeySignature("###") // => "A"</example>
    /// <example>Key.MajorTonicFromKeySignature("bb")  // => "Bb"</example>
    /// <example>Key.MajorTonicFromKeySignature(3)     // => "A"</example>
    public static string? MajorTonicFromKeySignature(string sig)
    {
        if (string.IsNullOrEmpty(sig)) return "C";
        if (!System.Text.RegularExpressions.Regex.IsMatch(sig, @"^b+$|^#+$"))
            return null;
        var alt = sig.Count(c => c == '#') - sig.Count(c => c == 'b');
        return Note.TransposeFifths("C", alt);
    }

    /// <inheritdoc cref="MajorTonicFromKeySignature(string)"/>
    public static string MajorTonicFromKeySignature(int alteration) =>
        Note.TransposeFifths("C", alteration);

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a fully-populated <see cref="KeyScaleInfo"/> from a template and tonic.
    /// </summary>
    private static KeyScaleInfo BuildKeyScale(KeyScaleTemplate t, string tonic)
    {
        // Resolve intervals from Roman numeral grades
        var intervals = t.Grades
            .Select(grade => RomanNumeralToInterval(grade))
            .ToArray();

        // Transpose tonic by each interval to get scale notes
        var scale = intervals
            .Select(ivl => string.IsNullOrEmpty(ivl) ? "" : Note.Transpose(tonic, ivl))
            .ToArray();

        // Build chord names: note + chord type suffix
        var chords = scale.Zip(t.ChordTypes, (n, c) => n + c).ToArray();

        // Secondary dominants: the dominant-of-the-dominant for each degree
        var secondaryDominants = BuildSecondaryDominants(scale, chords);

        // Supertonic of each secondary dominant (ii of V/x)
        var secDomSupertonics = BuildSupertonics(secondaryDominants, t.Triads);

        // Substitute dominants (tritone substitution of each secondary dominant)
        var substituteDominants = BuildSubstituteDominants(secondaryDominants);

        // Supertonic of each substitute dominant
        var subDomSupertonics = BuildSupertonics(substituteDominants, t.Triads);

        return new KeyScaleInfo
        {
            Tonic                        = tonic,
            Grades                       = t.Grades,
            Intervals                    = intervals,
            Scale                        = scale,
            Triads                       = scale.Zip(t.Triads, (n, tr) => n + tr).ToArray(),
            Chords                       = chords,
            ChordsHarmonicFunction       = t.HarmonicFunctions,
            ChordScales                  = scale.Zip(t.ChordScales, (n, cs) => n + " " + cs).ToArray(),
            SecondaryDominants           = secondaryDominants,
            SecondaryDominantSupertonics = secDomSupertonics,
            SubstituteDominants          = substituteDominants,
            SubstituteDominantSupertonics = subDomSupertonics,
        };
    }

    /// <summary>
    /// Converts a Roman numeral grade string to an interval name.
    /// Supports I–VII with optional flat prefix (e.g. "bIII", "bVI", "bVII").
    /// </summary>
    private static string RomanNumeralToInterval(string grade)
    {
        // Map of Roman numeral (without accidental) to diatonic interval
        var map = new Dictionary<string, string>
        {
            ["I"]   = "1P",
            ["II"]  = "2M",
            ["III"] = "3M",
            ["IV"]  = "4P",
            ["V"]   = "5P",
            ["VI"]  = "6M",
            ["VII"] = "7M",
        };

        if (grade.StartsWith('b'))
        {
            var roman = grade[1..];
            if (!map.TryGetValue(roman, out var base_ivl)) return "";
            // Flatten the interval by one semitone using the minor version
            var minorMap = new Dictionary<string, string>
            {
                ["2M"] = "2m",
                ["3M"] = "3m",
                ["6M"] = "6m",
                ["7M"] = "7m",
            };
            return minorMap.TryGetValue(base_ivl, out var flat) ? flat : base_ivl;
        }

        return map.TryGetValue(grade, out var ivl) ? ivl : "";
    }

    /// <summary>
    /// Builds secondary dominants for each scale degree.
    /// A secondary dominant is a V7 chord whose root is a P5 above a diatonic scale degree,
    /// is not itself diatonic to the key, and whose root is in the scale.
    /// </summary>
    private static string[] BuildSecondaryDominants(string[] scale, string[] chords)
    {
        return scale.Select(note =>
        {
            if (string.IsNullOrEmpty(note)) return "";
            var p5 = Note.Transpose(note, "5P");
            // The secondary dominant is a 7 chord built on the scale note,
            // provided the P5 above that note is in the scale (making it a legitimate V of something)
            // and the note+7 chord isn't already diatonic
            if (!scale.Contains(p5)) return "";
            var dom7 = note + "7";
            return chords.Contains(dom7) ? "" : dom7;
        }).ToArray();
    }

    /// <summary>
    /// Builds substitute dominants (tritone substitution: down a diminished 5th from each dominant root).
    /// </summary>
    private static string[] BuildSubstituteDominants(string[] secondaryDominants)
    {
        return secondaryDominants.Select(chord =>
        {
            if (string.IsNullOrEmpty(chord)) return "";
            var domRoot = chord[..^1]; // strip the "7"
            var subRoot = Note.Transpose(domRoot, "5d");
            subRoot = Note.Simplify(subRoot);
            return string.IsNullOrEmpty(subRoot) ? "" : subRoot + "7";
        }).ToArray();
    }

    /// <summary>
    /// Builds supertonics (ii chords) for a set of dominant chords.
    /// For a minor target triad: supertonic is m7; otherwise m7b5.
    /// </summary>
    private static string[] BuildSupertonics(string[] dominants, string[] targetTriads)
    {
        return dominants.Select((chord, i) =>
        {
            if (string.IsNullOrEmpty(chord)) return "";
            var domRoot   = chord[..^1];
            var minorRoot = Note.Transpose(domRoot, "5P");
            if (string.IsNullOrEmpty(minorRoot)) return "";
            var target    = i < targetTriads.Length ? targetTriads[i] : "";
            var isMinor   = target.EndsWith('m');
            return isMinor ? minorRoot + "m7" : minorRoot + "m7b5";
        }).ToArray();
    }

    /// <summary>
    /// Distance in fifths between two pitch classes (used for key signature calculation).
    /// </summary>
    private static int DistInFifths(string from, string to)
    {
        var f = Note.Get(from);
        var t = Note.Get(to);
        if (f.Empty || t.Empty) return 0;

        var fCoord = f.Coord as PitchClassCoordinates;
        var tCoord = t.Coord as PitchClassCoordinates;
        if (fCoord is null || tCoord is null) return 0;

        return tCoord.Fifths - fCoord.Fifths;
    }

    /// <summary>
    /// Converts an alteration number to an accidental string (e.g. 3 -> "###", -2 -> "bb").
    /// </summary>
    private static string AltToAcc(int alt) =>
        alt == 0 ? "" : alt > 0 ? new string('#', alt) : new string('b', -alt);
}

// -------------------------------------------------------------------------
// Internal template record (private implementation detail)
// -------------------------------------------------------------------------

/// <summary>
/// Immutable template data that defines a key scale pattern (major, natural minor, etc.).
/// </summary>
internal record KeyScaleTemplate(
    string[] Grades,
    string[] Triads,
    string[] ChordTypes,
    string[] HarmonicFunctions,
    string[] ChordScales
);

// -------------------------------------------------------------------------
// Public result types
// -------------------------------------------------------------------------

/// <summary>
/// All properties of a diatonic scale within a key (shared by major and all three minor forms).
/// </summary>
public record KeyScaleInfo
{
    /// <summary>The tonic note of this scale.</summary>
    public string Tonic { get; init; } = string.Empty;

    /// <summary>Roman numeral grade labels (e.g. ["I","II","III","IV","V","VI","VII"]).</summary>
    public string[] Grades { get; init; } = [];

    /// <summary>Interval from the tonic for each degree (e.g. ["1P","2M","3M","4P","5P","6M","7M"]).</summary>
    public string[] Intervals { get; init; } = [];

    /// <summary>Scale note names (e.g. ["C","D","E","F","G","A","B"] for C major).</summary>
    public string[] Scale { get; init; } = [];

    /// <summary>Triad chord symbols for each degree (e.g. ["C","Dm","Em","F","G","Am","Bdim"]).</summary>
    public string[] Triads { get; init; } = [];

    /// <summary>Seventh chord symbols for each degree (e.g. ["Cmaj7","Dm7","Em7","Fmaj7","G7","Am7","Bm7b5"]).</summary>
    public string[] Chords { get; init; } = [];

    /// <summary>Harmonic function labels for each degree (e.g. ["T","SD","T","SD","D","T","D"]).</summary>
    public string[] ChordsHarmonicFunction { get; init; } = [];

    /// <summary>Scale name recommended for improvisation over each chord (e.g. ["C major","D dorian",...]).</summary>
    public string[] ChordScales { get; init; } = [];

    /// <summary>Secondary dominant (V7) chord for each degree, or empty string if none.</summary>
    public string[] SecondaryDominants { get; init; } = [];

    /// <summary>Supertonic (ii7) of each secondary dominant, or empty string if none.</summary>
    public string[] SecondaryDominantSupertonics { get; init; } = [];

    /// <summary>Tritone-substitute dominant for each degree, or empty string if none.</summary>
    public string[] SubstituteDominants { get; init; } = [];

    /// <summary>Supertonic (ii7) of each substitute dominant, or empty string if none.</summary>
    public string[] SubstituteDominantSupertonics { get; init; } = [];

    internal static readonly KeyScaleInfo Empty = new();
}

/// <summary>
/// All properties of a major key.
/// </summary>
public record MajorKeyInfo : KeyScaleInfo
{
    /// <summary>The tonic note of the key.</summary>
    public new string Tonic { get; init; } = string.Empty;

    /// <summary>Number of sharps (positive) or flats (negative) in the key signature.</summary>
    public int Alteration { get; init; }

    /// <summary>Key signature as an accidental string (e.g. "###", "bb", "").</summary>
    public string KeySignature { get; init; } = string.Empty;

    /// <summary>The relative minor tonic (e.g. "A" for C major).</summary>
    public string MinorRelative { get; init; } = string.Empty;

    /// <summary>True if this is the sentinel empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel returned for invalid tonic input.</summary>
    public static new readonly MajorKeyInfo Empty = new() { IsEmpty = true };
}

/// <summary>
/// All properties of a minor key, including its three modal forms.
/// </summary>
public record MinorKeyInfo
{
    /// <summary>The tonic note of the key.</summary>
    public string Tonic { get; init; } = string.Empty;

    /// <summary>Number of sharps (positive) or flats (negative) in the key signature.</summary>
    public int Alteration { get; init; }

    /// <summary>Key signature as an accidental string.</summary>
    public string KeySignature { get; init; } = string.Empty;

    /// <summary>The relative major tonic (e.g. "C" for A minor).</summary>
    public string RelativeMajor { get; init; } = string.Empty;

    /// <summary>Natural minor scale and chord data.</summary>
    public KeyScaleInfo Natural { get; init; } = KeyScaleInfo.Empty;

    /// <summary>Harmonic minor scale and chord data.</summary>
    public KeyScaleInfo Harmonic { get; init; } = KeyScaleInfo.Empty;

    /// <summary>Melodic minor scale and chord data.</summary>
    public KeyScaleInfo Melodic { get; init; } = KeyScaleInfo.Empty;

    /// <summary>True if this is the sentinel empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel returned for invalid tonic input.</summary>
    public static readonly MinorKeyInfo Empty = new() { IsEmpty = true };
}
