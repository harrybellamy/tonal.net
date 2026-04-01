namespace Tonal.Core;

/// <summary>
/// Provides functions for creating, analysing, and transforming musical scales.
/// A scale combines a <see cref="ScaleTypeInfo"/> (interval pattern) with an optional tonic note.
/// Ported from Tonal.js Scale module: https://github.com/tonaljs/tonal/blob/main/packages/scale/index.ts
/// </summary>
public static class Scale
{
    // -------------------------------------------------------------------------
    // Get
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a <see cref="ScaleInfo"/> for the given scale name (e.g. "C major", "D dorian").
    /// When no tonic is present (e.g. "major"), notes will be empty but intervals are still populated.
    /// Returns <see cref="ScaleInfo.Empty"/> for unrecognised scale types.
    /// </summary>
    /// <example>Scale.Get("C major")  // => { Name: "C major", Tonic: "C", Notes: ["C","D","E","F","G","A","B"] }</example>
    /// <example>Scale.Get("major")    // => { Name: "major",   Tonic: null, Notes: [] }</example>
    public static ScaleInfo Get(string scaleName)
    {
        var (tonic, type) = Tokenize(scaleName);
        return Build(tonic, type);
    }

    /// <summary>
    /// Returns a <see cref="ScaleInfo"/> from pre-tokenised [tonic, type] tokens.
    /// </summary>
    /// <example>Scale.Get(["C", "major"]) // => { Name: "C major", ... }</example>
    public static ScaleInfo Get(string tonic, string type) => Build(tonic, type);

    // -------------------------------------------------------------------------
    // Names
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all canonical scale type names from the dictionary.
    /// </summary>
    /// <example>Scale.Names() // => ["major pentatonic", "major", "minor", ...]</example>
    public static IReadOnlyList<string> Names() => ScaleType.Names();

    // -------------------------------------------------------------------------
    // Tokenize
    // -------------------------------------------------------------------------

    /// <summary>
    /// Splits a scale name string into a (tonic, type) tuple.
    /// If no valid tonic is found at the start, the entire string is treated as the type.
    /// </summary>
    /// <example>Scale.Tokenize("C mixolydian") // => ("C", "mixolydian")</example>
    /// <example>Scale.Tokenize("major")        // => ("",  "major")</example>
    public static (string Tonic, string Type) Tokenize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ("", "");

        var spaceIndex = name.IndexOf(' ');
        if (spaceIndex < 0)
        {
            // No space — could be just a tonic ("C") or just a type ("major")
            var singleNote = Note.Get(name);
            return singleNote.Empty
                ? ("", name.ToLowerInvariant())
                : (singleNote.Name, "");
        }

        var tonicPart = name[..spaceIndex];
        var tonicNote = Note.Get(tonicPart);
        if (tonicNote.Empty)
            return ("", name.ToLowerInvariant());

        var typePart = name[(tonicNote.Name.Length + 1)..].ToLowerInvariant().Trim();
        return (tonicNote.Name, typePart);
    }

    // -------------------------------------------------------------------------
    // Detect
    // -------------------------------------------------------------------------

    /// <summary>
    /// Given a list of notes, detects scale names that match.
    /// By default returns exact matches plus extended scales (supersets).
    /// Pass <c>matchExact: true</c> to return only exact matches.
    /// Pass a <c>tonic</c> to fix the root; otherwise the first note is used.
    /// </summary>
    /// <example>Scale.Detect(["C","D","E","F","G","A","B"])           // => ["C major", "C bebop major", ...]</example>
    /// <example>Scale.Detect(["C","D","E","F","G","A","B"], "C", true) // => ["C major"]</example>
    public static IReadOnlyList<string> Detect(
        IEnumerable<string> notes,
        string? tonic = null,
        bool matchExact = false)
    {
        var noteList = notes.ToList();
        if (noteList.Count == 0) return [];

        var notesChroma = PitchClassSet.Get(noteList).Chroma;
        var tonicNote   = Note.Get(tonic ?? noteList[0]);
        if (tonicNote.Empty) return [];

        var tonicChroma = tonicNote.Chroma;

        // Rotate the chroma so the tonic is at position 0, and force it to 1
        var bits = notesChroma.ToCharArray();
        bits[tonicChroma] = '1';
        var rotated = RotateChroma(bits, tonicChroma);

        var results = new List<string>();

        // Exact match: find a scale type whose chroma equals the rotated input
        var exactMatch = ScaleType.All().FirstOrDefault(st => st.Chroma == rotated);
        if (exactMatch is not null)
            results.Add(tonicNote.Name + " " + exactMatch.Name);

        if (matchExact)
            return results;

        // Fit matches: find all scale types that are supersets of the rotated chroma
        var isSuperSet = PitchClassSet.IsSupersetOf(rotated);
        foreach (var st in ScaleType.All())
        {
            if (st.Chroma != rotated && isSuperSet(st.Chroma))
                results.Add(tonicNote.Name + " " + st.Name);
        }

        return results;
    }

    // -------------------------------------------------------------------------
    // Scale relationships
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all scale names that are strict supersets of the given scale
    /// (contain all its notes plus at least one more).
    /// </summary>
    /// <example>Scale.Extended("major") // => ["bebop", "bebop major", "chromatic", ...]</example>
    public static IReadOnlyList<string> Extended(string scaleName)
    {
        var scale = Get(scaleName);
        if (scale.IsEmpty) return [];
        var isSuperset = PitchClassSet.IsSupersetOf(scale.Chroma);
        return ScaleType.All()
            .Where(st => st.Chroma != scale.Chroma && isSuperset(st.Chroma))
            .Select(st => st.Name)
            .ToList();
    }

    /// <summary>
    /// Returns all scale names that are strict subsets of the given scale
    /// (have fewer notes, all of which are in the given scale).
    /// </summary>
    /// <example>Scale.Reduced("major") // => ["ionian pentatonic", "major pentatonic", "ritusen"]</example>
    public static IReadOnlyList<string> Reduced(string scaleName)
    {
        var scale = Get(scaleName);
        if (scale.IsEmpty) return [];
        var isSubset = PitchClassSet.IsSubsetOf(scale.Chroma);
        return ScaleType.All()
            .Where(st => st.Chroma != scale.Chroma && isSubset(st.Chroma))
            .Select(st => st.Name)
            .ToList();
    }

    // -------------------------------------------------------------------------
    // Mode names
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all named modes of the given scale as (tonic, modeName) pairs.
    /// When the scale has a tonic, the mode tonics are real note names.
    /// When there is no tonic, the mode tonics are interval names.
    /// </summary>
    /// <example>
    /// Scale.ModeNames("C pentatonic")
    /// // => [("C","major pentatonic"),("D","egyptian"),("E","malkos raga"),("G","ritusen"),("A","minor pentatonic")]
    /// </example>
    public static IReadOnlyList<(string Tonic, string Name)> ModeNames(string scaleName)
    {
        var scale = Get(scaleName);
        if (scale.IsEmpty) return [];

        // Use real note names when we have a tonic, otherwise interval names
        var tonics = scale.Tonic is not null ? scale.Notes : scale.Intervals;
        var results = new List<(string, string)>();

        var bits = scale.Chroma.ToCharArray();
        for (int i = 0; i < 12; i++)
        {
            if (bits[i] != '1') continue;

            var rotated   = RotateChroma(bits, i);
            var modeType  = ScaleType.Get(rotated);
            if (modeType.IsEmpty) continue;

            // Find which scale degree this rotation corresponds to
            var degreeIndex = Array.IndexOf(
                scale.Chroma.Select((c, idx) => (c, idx))
                             .Where(t => t.c == '1')
                             .Select(t => t.idx)
                             .ToArray(), i);

            if (degreeIndex < 0 || degreeIndex >= tonics.Length) continue;
            results.Add((tonics[degreeIndex], modeType.Name));
        }

        return results;
    }

    // -------------------------------------------------------------------------
    // Scale notes helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Given an array of notes, returns the unique pitch classes sorted starting
    /// from the first note of the array.
    /// </summary>
    /// <example>Scale.ScaleNotes(["D4","C#5","A5","F#6"]) // => ["D","F#","A","C#"]</example>
    public static IReadOnlyList<string> ScaleNotes(IEnumerable<string> notes)
    {
        var pitchClasses = notes
            .Select(n => Note.Get(n).PitchClass)
            .Where(pc => !string.IsNullOrEmpty(pc))
            .ToList();

        if (pitchClasses.Count == 0) return [];

        var tonic  = pitchClasses[0];
        var sorted = Note.SortedUniqNames(pitchClasses).ToList();

        // Rotate so the tonic is first
        var tonicIndex = sorted.IndexOf(tonic);
        if (tonicIndex <= 0) return sorted;

        return sorted.Skip(tonicIndex).Concat(sorted.Take(tonicIndex)).ToList();
    }

    // -------------------------------------------------------------------------
    // Degrees / Steps
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a function that maps a 1-based scale degree to a note name.
    /// Degree 1 = tonic, 2 = second scale note, etc. Negative degrees count downward.
    /// Works with both pitch-class scales (no octave) and octave-specific scales.
    /// </summary>
    /// <example>
    /// var degree = Scale.Degrees("C major");
    /// new[] {1,2,3}.Select(degree) // => ["C","D","E"]
    /// </example>
    public static Func<int, string> Degrees(string scaleName)
    {
        var scale = Get(scaleName);
        if (scale.IsEmpty) return _ => "";
        return degree =>
        {
            if (degree == 0) return "";
            var step = degree > 0 ? degree - 1 : degree;
            return TransposeByStep(scale, step);
        };
    }

    /// <summary>
    /// Returns a function that maps a 0-based step index to a note name.
    /// Step 0 = tonic, step 1 = second note, etc. Negative steps count downward.
    /// </summary>
    /// <example>
    /// var step = Scale.Steps("C4 major");
    /// new[] {0,1,2}.Select(step) // => ["C4","D4","E4"]
    /// </example>
    public static Func<int, string> Steps(string scaleName)
    {
        var scale = Get(scaleName);
        if (scale.IsEmpty) return _ => "";
        return step => TransposeByStep(scale, step);
    }

    // -------------------------------------------------------------------------
    // Range
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all notes of a scale within a given MIDI range (inclusive), preserving
    /// the correct enharmonic spelling of the scale.
    /// </summary>
    /// <example>Scale.RangeOf("C major", "C4", "C5") // => ["C4","D4","E4","F4","G4","A4","B4","C5"]</example>
    public static IReadOnlyList<string> RangeOf(string scaleName, string fromNote, string toNote)
    {
        var scale = Get(scaleName);
        if (scale.IsEmpty || scale.Tonic is null) return [];

        var from = Note.Get(fromNote);
        var to   = Note.Get(toNote);
        if (from.Empty || to.Empty || from.Midi is null || to.Midi is null) return [];

        var scaleNoteNames = scale.Notes;
        var scaleChromaMap = scaleNoteNames
            .Select(n => Note.Get(n))
            .Where(n => !n.Empty)
            .GroupBy(n => n.Chroma)
            .ToDictionary(g => g.Key, g => g.First().PitchClass);

        var results = new List<string>();
        int step    = from.Midi.Value <= to.Midi.Value ? 1 : -1;

        for (int midi = from.Midi.Value; step > 0 ? midi <= to.Midi.Value : midi >= to.Midi.Value; midi += step)
        {
            var chroma = midi % 12;
            if (scaleChromaMap.TryGetValue(chroma, out var pc))
            {
                var octave = midi / 12 - 1;
                results.Add(pc + octave);
            }
        }

        return results;
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private static ScaleInfo Build(string tonic, string type)
    {
        var st = ScaleType.Get(type);
        if (st.IsEmpty) return ScaleInfo.Empty;

        var tonicNote = string.IsNullOrEmpty(tonic) ? null : Note.Get(tonic);
        var hasTonic  = tonicNote is { Empty: false };
        var tonicName = hasTonic ? tonicNote!.Name : null;

        var notes = hasTonic
            ? st.Intervals
                .Select(ivl => Note.Transpose(tonicName!, ivl))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray()
            : [];

        return new ScaleInfo
        {
            Name      = hasTonic ? tonicName + " " + st.Name : st.Name,
            Type      = st.Name,
            Tonic     = tonicName,
            Notes     = notes,
            Intervals = st.Intervals,
            Aliases   = st.Aliases,
            Chroma    = st.Chroma,
            SetNum    = st.SetNum,
            Length    = st.Length,
        };
    }

    /// <summary>
    /// Rotates a chroma bit array left by <paramref name="n"/> positions.
    /// </summary>
    private static string RotateChroma(char[] bits, int n)
    {
        var rotated = new char[12];
        for (int i = 0; i < 12; i++)
            rotated[i] = bits[(i + n) % 12];
        return new string(rotated);
    }

    /// <summary>
    /// Transposes the scale tonic by a given 0-based step, wrapping across octaves.
    /// Handles both pitch-class (no octave) and octave-specific scales.
    /// </summary>
    private static string TransposeByStep(ScaleInfo scale, int step)
    {
        if (scale.Tonic is null || scale.Intervals.Length == 0) return "";

        var count      = scale.Intervals.Length;
        var octaveShift = (int)Math.Floor((double)step / count);
        if (step < 0 && step % count != 0) octaveShift--;

        var stepInScale = ((step % count) + count) % count;
        var interval    = scale.Intervals[stepInScale];

        var transposed = Note.Transpose(scale.Tonic, interval);
        if (string.IsNullOrEmpty(transposed)) return "";

        // If the scale tonic has an octave, apply the octave shift
        var tonicInfo = Note.Get(scale.Tonic);
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
/// Represents a scale — a <see cref="ScaleTypeInfo"/> bound to an optional tonic note.
/// </summary>
public record ScaleInfo
{
    /// <summary>Full name, e.g. "C major". Just the type name when no tonic (e.g. "major").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The scale type name, e.g. "major", "dorian".</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>The tonic note name (e.g. "C", "F#"), or null when no tonic was specified.</summary>
    public string? Tonic { get; init; }

    /// <summary>The notes of the scale (e.g. ["C","D","E","F","G","A","B"]). Empty when no tonic.</summary>
    public string[] Notes { get; init; } = [];

    /// <summary>The interval pattern from the tonic (e.g. ["1P","2M","3M","4P","5P","6M","7M"]).</summary>
    public string[] Intervals { get; init; } = [];

    /// <summary>Alternative names for this scale type.</summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>12-character binary chroma string.</summary>
    public string Chroma { get; init; } = string.Empty;

    /// <summary>Integer set number (0–4095).</summary>
    public int SetNum { get; init; }

    /// <summary>Number of notes in the scale.</summary>
    public int Length { get; init; }

    /// <summary>True if this is the sentinel empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel value returned when a scale name cannot be resolved.</summary>
    public static readonly ScaleInfo Empty = new() { IsEmpty = true };
}
