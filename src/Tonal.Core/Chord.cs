using System.Text.RegularExpressions;

namespace Tonal.Core;

/// <summary>
/// Provides functions for creating, analysing, and transforming musical chords.
/// A chord combines a <see cref="ChordTypeInfo"/> (interval pattern) with a tonic note,
/// and optionally a bass note for slash chords.
/// Ported from Tonal.js Chord module: https://github.com/tonaljs/tonal/blob/main/packages/chord/index.ts
/// </summary>
public static partial class Chord
{
    [GeneratedRegex(@"^([A-Ga-g][#b]*)(.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex TonicRegex();

    // -------------------------------------------------------------------------
    // Get
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a <see cref="ChordInfo"/> for the given chord symbol.
    /// Supports slash notation (e.g. "Cmaj7/E") and tonic-less lookups (e.g. "maj7").
    /// Returns <see cref="ChordInfo.Empty"/> for unrecognised chord types.
    /// </summary>
    /// <example>Chord.Get("Cmaj7")   // => { Name: "C major seventh", Tonic: "C", Notes: ["C","E","G","B"] }</example>
    /// <example>Chord.Get("Cmaj7/E") // => { Bass: "E", RootDegree: 2, ... }</example>
    /// <example>Chord.Get("maj7")    // => { Tonic: null, Notes: [] }</example>
    public static ChordInfo Get(string chordSymbol)
    {
        var (tonic, type, bass) = Tokenize(chordSymbol);
        return Build(tonic, type, bass);
    }

    /// <summary>
    /// Returns a <see cref="ChordInfo"/> from pre-tokenised components.
    /// </summary>
    public static ChordInfo Get(string tonic, string type, string bass = "") =>
        Build(tonic, type, bass);

    // -------------------------------------------------------------------------
    // Tokenize
    // -------------------------------------------------------------------------

    /// <summary>
    /// Splits a chord symbol into (tonic, type, bass) components.
    /// </summary>
    /// <example>Chord.Tokenize("Cmaj7/E") // => ("C", "maj7", "E")</example>
    /// <example>Chord.Tokenize("Dm7")     // => ("D", "m7",   "")</example>
    /// <example>Chord.Tokenize("maj7")    // => ("",  "maj7", "")</example>
    public static (string Tonic, string Type, string Bass) Tokenize(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return ("", "", "");

        // Strip slash bass first
        var bass = "";
        var core = symbol;
        var slashIdx = symbol.LastIndexOf('/');
        if (slashIdx > 0)
        {
            var potentialBass = symbol[(slashIdx + 1)..];
            if (!Note.Get(potentialBass).Empty)
            {
                bass = Note.Get(potentialBass).Name;
                core = symbol[..slashIdx];
            }
        }

        // Try to parse a tonic note from the start
        var match = TonicRegex().Match(core);
        if (!match.Success)
            return ("", core, bass);

        var tonicStr  = match.Groups[1].Value;
        var tonicNote = Note.Get(tonicStr);

        if (tonicNote.Empty)
            return ("", core, bass);

        var type = match.Groups[2].Value;

        // If type is empty, the whole symbol may just be a bare note name
        return (tonicNote.Name, type, bass);
    }

    // -------------------------------------------------------------------------
    // Names
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all canonical chord type names from the dictionary.
    /// </summary>
    public static IReadOnlyList<string> Names() => ChordType.Names();

    // -------------------------------------------------------------------------
    // Transpose
    // -------------------------------------------------------------------------

    /// <summary>
    /// Transposes a chord symbol by the given interval.
    /// </summary>
    /// <example>Chord.Transpose("Cmaj7", "2M") // => "Dmaj7"</example>
    public static string Transpose(string chordSymbol, string interval)
    {
        var (tonic, type, bass) = Tokenize(chordSymbol);
        if (string.IsNullOrEmpty(tonic)) return chordSymbol;

        var newTonic = Note.Transpose(tonic, interval);
        if (string.IsNullOrEmpty(newTonic)) return chordSymbol;

        var newBass = string.IsNullOrEmpty(bass) ? "" : Note.Transpose(bass, interval);
        var result  = newTonic + type;
        return string.IsNullOrEmpty(newBass) ? result : result + "/" + newBass;
    }

    // -------------------------------------------------------------------------
    // Degrees / Steps
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a function that maps a 1-based chord degree to a note name.
    /// Degree 1 = root, 2 = second chord tone, etc.
    /// </summary>
    /// <example>
    /// var triad = Chord.Degrees("Cm");
    /// new[] {1,2,3}.Select(triad)   // => ["C","Eb","G"]
    /// new[] {2,3,1}.Select(triad)   // => ["Eb","G","C"]  (first inversion)
    /// </example>
    public static Func<int, string> Degrees(string chordSymbol)
    {
        var chord = Get(chordSymbol);
        if (chord.IsEmpty || chord.Tonic is null) return _ => "";
        return degree =>
        {
            if (degree == 0) return "";
            var step = degree > 0 ? degree - 1 : degree;
            return StepToNote(chord, step);
        };
    }

    /// <summary>
    /// Returns a function that maps a 0-based step index to a note name.
    /// </summary>
    /// <example>
    /// var triad = Chord.Steps("C");
    /// new[] {0,1,2}.Select(triad) // => ["C","E","G"]
    /// </example>
    public static Func<int, string> Steps(string chordSymbol)
    {
        var chord = Get(chordSymbol);
        if (chord.IsEmpty || chord.Tonic is null) return _ => "";
        return step => StepToNote(chord, step);
    }

    // -------------------------------------------------------------------------
    // Scale / chord relationships
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all scale names that contain all the notes of the given chord.
    /// </summary>
    /// <example>Chord.ChordScales("Cmaj7") // => ["major", "lydian", "major pentatonic", ...]</example>
    public static IReadOnlyList<string> ChordScales(string chordSymbol)
    {
        var chord = Get(chordSymbol);
        if (chord.IsEmpty) return [];
        var isSuperset = PitchClassSet.IsSupersetOf(chord.Chroma);
        return ScaleType.All()
            .Where(st => isSuperset(st.Chroma))
            .Select(st => st.Name)
            .ToList();
    }

    /// <summary>
    /// Returns all chord names that are extended versions of the given chord
    /// (same notes plus at least one more).
    /// </summary>
    /// <example>Chord.Extended("Cmaj7") // => ["Cmaj9", "Cmaj13", ...]</example>
    public static IReadOnlyList<string> Extended(string chordSymbol)
    {
        var chord = Get(chordSymbol);
        if (chord.IsEmpty || chord.Tonic is null) return [];
        var isSuperset = PitchClassSet.IsSupersetOf(chord.Chroma);
        return ChordType.All()
            .Where(ct => ct.Chroma != chord.Chroma && isSuperset(ct.Chroma) && ct.Aliases.Length > 0)
            .Select(ct => chord.Tonic + ct.Aliases[0])
            .ToList();
    }

    /// <summary>
    /// Returns all chord names that are reduced versions of the given chord
    /// (fewer notes, all contained in the given chord).
    /// </summary>
    /// <example>Chord.Reduced("Cmaj7") // => ["C", "Csus2", ...]</example>
    public static IReadOnlyList<string> Reduced(string chordSymbol)
    {
        var chord = Get(chordSymbol);
        if (chord.IsEmpty || chord.Tonic is null) return [];
        var isSubset = PitchClassSet.IsSubsetOf(chord.Chroma);
        return ChordType.All()
            .Where(ct => ct.Chroma != chord.Chroma && isSubset(ct.Chroma) && ct.Aliases.Length > 0)
            .Select(ct => chord.Tonic + ct.Aliases[0])
            .ToList();
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private static ChordInfo Build(string tonic, string type, string bass)
    {
        var lookupType = string.IsNullOrEmpty(type) ? "major" : type;
        var ct         = ChordType.Get(lookupType);
        if (ct.IsEmpty) return ChordInfo.Empty;

        var tonicNote = string.IsNullOrEmpty(tonic) ? null : Note.Get(tonic);
        var hasTonic  = tonicNote is { Empty: false };
        var tonicName = hasTonic ? tonicNote!.Name : null;

        var notes = hasTonic
            ? ct.Intervals
                .Select(ivl => Note.Transpose(tonicName!, ivl))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray()
            : [];

        // Resolve bass and rootDegree for slash chords
        var bassNote   = string.IsNullOrEmpty(bass) ? null : Note.Get(bass);
        var hasBass    = bassNote is { Empty: false };
        var bassName   = hasBass ? bassNote!.Name : null;
        var rootDegree = 1;

        if (hasBass && hasTonic && notes.Length > 0)
        {
            var bassChroma = bassNote!.Chroma;
            for (int i = 0; i < notes.Length; i++)
            {
                if (Note.Get(notes[i]).Chroma == bassChroma)
                {
                    rootDegree = i + 1;
                    break;
                }
            }
        }

        // Full chord name: e.g. "C major seventh"
        var name = hasTonic
            ? (tonicName + (string.IsNullOrEmpty(ct.Name) ? "" : " " + ct.Name)).Trim()
            : ct.Name;

        // Symbol: tonic alone for bare notes, otherwise tonic + first alias (e.g. "Cmaj7")
        var symbol = hasTonic
            ? string.IsNullOrEmpty(type) ? tonicName! : (ct.Aliases.Length > 0 ? tonicName + ct.Aliases[0] : tonicName!)
            : ct.Aliases.FirstOrDefault() ?? "";

        return new ChordInfo
        {
            Name       = name,
            Symbol     = symbol,
            Type       = ct.Name,
            Tonic      = tonicName,
            Bass       = bassName,
            RootDegree = rootDegree,
            Notes      = notes,
            Intervals  = ct.Intervals,
            Aliases    = ct.Aliases,
            Quality    = ct.Quality,
            Chroma     = ct.Chroma,
            SetNum     = ct.SetNum,
            Length     = ct.Length,
        };
    }

    private static string StepToNote(ChordInfo chord, int step)
    {
        if (chord.Tonic is null || chord.Intervals.Length == 0) return "";

        var count       = chord.Intervals.Length;
        var octaveShift = (int)Math.Floor((double)step / count);
        if (step < 0 && step % count != 0) octaveShift--;

        var stepInChord = ((step % count) + count) % count;
        var interval    = chord.Intervals[stepInChord];
        var transposed  = Note.Transpose(chord.Tonic, interval);

        if (string.IsNullOrEmpty(transposed)) return "";

        // If tonic has an octave, apply octave shift
        var tonicInfo = Note.Get(chord.Tonic);
        if (tonicInfo.Octave.HasValue && octaveShift != 0)
        {
            var noteInfo = Note.Get(transposed);
            if (!noteInfo.Empty && noteInfo.Octave.HasValue)
                transposed = noteInfo.PitchClass + (noteInfo.Octave.Value + octaveShift);
        }

        return transposed;
    }
}

/// <summary>
/// Represents a chord — a <see cref="ChordTypeInfo"/> bound to a tonic note,
/// with optional bass note for slash chords.
/// </summary>
public record ChordInfo
{
    /// <summary>Full name, e.g. "C major seventh".</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Short symbol, e.g. "Cmaj7".</summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>The chord type name, e.g. "major seventh".</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>The tonic note name (e.g. "C"), or null when no tonic was specified.</summary>
    public string? Tonic { get; init; }

    /// <summary>The bass note for slash chords (e.g. "E" in "Cmaj7/E"), or null.</summary>
    public string? Bass { get; init; }

    /// <summary>1-based degree of the bass note within the chord (1 = root position).</summary>
    public int RootDegree { get; init; } = 1;

    /// <summary>The notes of the chord (e.g. ["C","E","G","B"]). Empty when no tonic.</summary>
    public string[] Notes { get; init; } = [];

    /// <summary>The interval pattern from the tonic.</summary>
    public string[] Intervals { get; init; } = [];

    /// <summary>Short symbol aliases (e.g. ["maj7", "Δ", "M7"]).</summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>The quality category: Major, Minor, Augmented, Diminished, or Unknown.</summary>
    public ChordQuality Quality { get; init; }

    /// <summary>12-character binary chroma string.</summary>
    public string Chroma { get; init; } = string.Empty;

    /// <summary>Integer set number (0–4095).</summary>
    public int SetNum { get; init; }

    /// <summary>Number of notes in the chord.</summary>
    public int Length { get; init; }

    /// <summary>True if this is the sentinel empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel value returned when a chord symbol cannot be resolved.</summary>
    public static readonly ChordInfo Empty = new() { IsEmpty = true };
}
