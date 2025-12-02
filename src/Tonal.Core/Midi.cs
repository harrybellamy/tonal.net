namespace Tonal.Core;

public static partial class Midi
{
    [System.Text.RegularExpressions.GeneratedRegex("^([A-Ga-g])([#b]*)(-?\\d+)$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();

    public static bool IsMidi(int midi)
    {
        return midi >= 0 && midi <= 127;
    }

    public static double MidiToFreq(int midi, double tuning = 440.0)
    {
        return tuning * Math.Pow(2, (midi - 69) / 12.0);
    }

    // Convert an int midi value to nullable midi (null if out of range)
    public static int? ToMidi(int midi)
    {
        return IsMidi(midi) ? midi : (int?)null;
    }

    // Convert a double (could be NaN) to midi or null
    public static int? ToMidi(double value)
    {
        if (double.IsNaN(value)) return null;
        // Round to nearest integer similar to JS behaviour
        var rounded = (int)Math.Round(value);
        return ToMidi(rounded);
    }

    // Convert a string which can be a numeric midi value or a note name (e.g. "C4")
    public static int? ToMidi(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        // Try numeric parse first
        if (int.TryParse(text, out var n)) return ToMidi(n);

        // Parse as note name like C4, C#4, Db3, etc.
        // Pattern: letter + optional accidentals + octave
        var m = MyRegex().Match(text.Trim());
        if (!m.Success) return null;
        var letter = m.Groups[1].Value.ToUpper();
        var acc = m.Groups[2].Value;
        var octStr = m.Groups[3].Value;
        if (!int.TryParse(octStr, out var octave)) return null;

        int baseSemitone = letter switch
        {
            "C" => 0,
            "D" => 2,
            "E" => 4,
            "F" => 5,
            "G" => 7,
            "A" => 9,
            "B" => 11,
            _ => 0
        };
        var alt = acc.Count(c => c == '#') - acc.Count(c => c == 'b');
        var midiFromNote = (octave + 1) * 12 + baseSemitone + alt;
        return ToMidi(midiFromNote);
    }

    /// <summary>
    /// Convert frequency in Hz to MIDI number (may be fractional).
    /// Formula: midi = 69 + 12 * log2(freq / 440)
    /// </summary>
    public static double FreqToMidi(double freq)
    {
        return 69.0 + 12.0 * Math.Log(freq / 440.0, 2.0);
    }

    private static readonly string[] NoteNamesFlats = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
    private static readonly string[] NoteNamesSharps = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    /// <summary>
    /// Convert a MIDI number to a note name string.
    /// </summary>
    /// <param name="midi">MIDI number (0-127)</param>
    /// <param name="sharps">If true, use sharps; otherwise use flats</param>
    /// <param name="pitchClass">If true, return just pitch class (no octave)</param>
    public static string MidiToNoteName(int midi, bool sharps = false, bool pitchClass = false)
    {
        if (!IsMidi(midi)) return string.Empty;

        var names = sharps ? NoteNamesSharps : NoteNamesFlats;
        var chroma = midi % 12;
        var noteName = names[chroma];

        if (pitchClass) return noteName;

        var octave = midi / 12 - 1;
        return $"{noteName}{octave}";
    }

    /// <summary>
    /// Convert a MIDI number (possibly NaN or infinite) to a note name.
    /// Returns empty string for invalid inputs.
    /// </summary>
    public static string MidiToNoteName(double midi, bool sharps = false, bool pitchClass = false)
    {
        if (double.IsNaN(midi) || double.IsInfinity(midi)) return string.Empty;
        var rounded = (int)Math.Round(midi);
        return MidiToNoteName(rounded, sharps, pitchClass);
    }

    private static int Chroma(int midi) => midi % 12;

    private static int[] PcsetFromChroma(string chroma)
    {
        int currentIndex = 0;
        List<int> pcset = [];
        while (currentIndex < chroma.Length && currentIndex < 12)
        {
            if (chroma[currentIndex] == '1')
            {
                pcset.Add(currentIndex);
            }
            currentIndex++;
        }
        
        return [.. pcset];
    }

    private static int[] PcsetFromMidi(int[] midi)
    {
        return [.. midi
            .Select(Chroma)
            .Distinct()
            .OrderBy(n => n)];
    }

    /**
     * Given a list of midi numbers, returns the pitch class set (unique chroma numbers)
     * @param midi
     * @example
     *
     */
    public static int[] Pcset(int[] notes)
    {
        return PcsetFromMidi(notes); 
    }

    public static int[] Pcset(string chroma)
    {
        return PcsetFromChroma(chroma);
    }

    /**
     * Returns a function that finds the nearest midi note of a pitch class set.
     * Can be used to constrain a note to a scale.
     * @param notes - a list of midi numbers or a chroma string (e.g. "100100100101")
     * @example
     * const nearest = Midi.pcsetNearest(Scale.get("D dorian").chroma);
     * [60, 61, 62, 63, 64, 65, 66].map(nearest); // => [60, 62, 62, 63, 65, 65, 67]
     */
    public static Func<int, int?> PcsetNearest(int[] notes)
    {
        var set = Pcset(notes);
        return PcsetNearestInternal(set);
    }

    public static Func<int, int?> PcsetNearest(string chroma)
    {
        var set = Pcset(chroma);
        return PcsetNearestInternal(set);
    }

    private static Func<int, int?> PcsetNearestInternal(int[]? set)
    {
        return midi =>
        {
            if (set == null || set.Length == 0) return null;
            var ch = Chroma(midi);
            for (int i = 0; i < 12; i++)
            {
                var chUp = (ch + i) % 12;
                var chDown = (ch - i + 12) % 12;
                if (set.Contains(chUp)) return midi + i;
                if (set.Contains(chDown)) return midi - i;
            }
            return null;
        };
    }

    /// <summary>
    /// Returns a function that, given a scale step index, returns the MIDI note at that step.
    /// Positive steps go up, negative steps go down.
    /// </summary>
    public static Func<int, int?> PcsetSteps(string chroma, int tonic)
    {
        var set = Pcset(chroma);
        var sortedSet = set.OrderBy(x => x).ToArray();
        
        return step =>
        {
            if (sortedSet.Length == 0) return null;
            var octaveShift = (step / sortedSet.Length) * 12;
            if (step < 0 && step % sortedSet.Length != 0)
            {
                octaveShift -= 12;
            }
            var stepInOctave = ((step % sortedSet.Length) + sortedSet.Length) % sortedSet.Length;
            var pitchClass = sortedSet[stepInOctave];
            var tonicChroma = Chroma(tonic);
            var tonicOctave = tonic / 12;
            var midiNote = tonicOctave * 12 + tonicChroma + pitchClass - tonicChroma + octaveShift;
            return midiNote;
        };
    }

    /// <summary>
    /// Returns a function that, given a scale degree (1-indexed), returns the MIDI note at that degree.
    /// Degree 0 returns null. Negative degrees count downward from 1.
    /// </summary>
    public static Func<int, int?> PcsetDegrees(string chroma, int tonic)
    {
        var steps = PcsetSteps(chroma, tonic);
        
        return degree =>
        {
            if (degree == 0) return null;
            var stepIndex = degree > 0 ? degree - 1 : degree;
            return steps(stepIndex);
        };
    }
}