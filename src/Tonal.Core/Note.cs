using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Tonal.Core;

/// <summary>
/// Provides functions for parsing, transforming, and analyzing musical note names.
/// Ported from Tonal.js Note module: https://tonaljs.github.io/tonal/docs/basics/notes
/// </summary>
public static partial class Note
{
    private static readonly ConcurrentDictionary<string, NoteInfo> noteNameCache = [];
    [GeneratedRegex(@"^([a-gA-G]?)([#bx]*)(-?\d*)\s*(.*)$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
    private static readonly Regex NoteRegex = MyRegex();

    /// <summary>
    /// Parses a note name string and returns a NoteInfo object with properties
    /// like Name, PitchClass, Letter, Step, Accidentals, Alteration, Octave,
    /// Chroma, Midi, and Frequency.
    /// </summary>
    /// <example>Note.Get("C4") → new NoteInfo { Name = "C4", Midi = 60 }</example>
    public static NoteInfo Get(string noteName)
    {
        if (noteNameCache.TryGetValue(noteName, out NoteInfo? cachedValue))
        {
            return cachedValue;
        }

        var value = Parse(noteName);
        noteNameCache.TryAdd(noteName, value);
        return value;
    }

    /// <summary>
    /// Get note from coordinate objects.
    /// </summary>
    /// <param name="noteCoord"></param>
    /// <returns></returns>
    public static NoteInfo Get(Coordinates noteCoord)
    {
        var pitch = Pitch.Get(noteCoord);
        return Get(pitch.GetName());
    }

    private static NoteInfo Parse(string noteName)
    {
        var tokens = TokenizeNote(noteName);
        if (string.IsNullOrEmpty(tokens[0]) || !string.IsNullOrEmpty(tokens[3]))
            return NoNote;

        var letter = tokens[0];
        var acc = tokens[1];
        var octStr = tokens.Length > 2 ? tokens[2] : null;

        // SEMI contains semitone offsets: C=0, D=2, E=4, F=5, G=7, A=9, B=11
        Dictionary<string, int> SEMI = new() { ["C"] = 0, ["D"] = 2, ["E"] = 4, ["F"] = 5, ["G"] = 7, ["A"] = 9, ["B"] = 11 };

        int offset = SEMI[letter];
        int step = (letter[0] - 'C' + 7) % 7;
        int alt = AccToAlt(acc);
        int? oct = string.IsNullOrEmpty(octStr) ? null : int.Parse(octStr);
        var coord = new PitchInfo { Step = step, Alt = alt, Oct = oct, Dir = null }.ToCoordinates();

        int chroma = (offset + alt + 120) % 12;
        int height = oct == null
            ? Mod(offset + alt, 12) - 12 * 99
            : offset + alt + 12 * (oct.Value + 1);

        int? midi = (height >= 0 && height <= 127) ? height : null;
        double? freq = oct == null ? null : Math.Pow(2, (height - 69) / 12.0) * 440; 

        return new NoteInfo
        {
            Name = letter + acc.Replace("x", "##") + octStr,
            PitchClass = letter + acc.Replace("x", "##"),
            Letter = letter,
            Step = step,
            Accidentals = acc,
            Alteration = alt,
            Octave = oct,
            Chroma = chroma,
            Midi = midi,
            Height = height,
            Frequency = freq,
            Coord = coord
        };
    }

    private static string[] TokenizeNote(string str)
    {
        var match = NoteRegex.Match(str);
        if (!match.Success)
            return ["", "", "", ""];

        var letter = match.Groups[1].Value.ToUpper();
        var acc = match.Groups[2].Value;
        var octave = match.Groups[3].Value;
        var rest = match.Groups[4].Value;

        return [letter, acc, octave, rest];
    }

    private static int AccToAlt(string acc)
    {
        int sharps = acc.Count(c => c == '#');
        int flats = acc.Count(c => c == 'b');
        int doubleSharps = acc.Count(c => c == 'x') * 2;
        return sharps + doubleSharps - flats;
    }

    private static int Mod(int a, int b)
    {
        return ((a % b) + b) % b;
    }

    private static readonly NoteInfo NoNote = new() { Name = "", Empty = true };

    /// <summary>
    /// Normalizes a note name string (e.g., converts enharmonic spellings) to a canonical form.
    /// </summary>
    /// <example>Note.Name("fx4") → "F##4"</example>
    public static string Name(string noteName) => Get(noteName).Name;

    /// <summary>
    /// Returns the pitch class portion of a note (the letter + accidentals, no octave).
    /// </summary>
    /// <example>Note.PitchClass("Ab5") → "Ab"</example>
    public static string PitchClass(string noteName) => Get(noteName).PitchClass;

    /// <summary>
    /// Returns the accidental string part of a note (e.g. "#", "b", "##", or "" if none).
    /// </summary>
    /// <example>Note.Accidentals("Eb") → "b"</example>
    public static string Accidentals(string noteName) => Get(noteName).Accidentals;

    /// <summary>
    /// Returns the octave number from a note name, or null if no octave is included.
    /// </summary>
    /// <example>Note.Octave("C4") → 4</example>
    public static int? Octave(string noteName) => Get(noteName).Octave;
    /// <summary>
    /// Converts a note name to a MIDI number (if octave present), else returns null.
    /// </summary>
    /// <example>Note.Midi("A4") → 69</example>
    public static int? Midi(string noteName) => Get(noteName).Midi;

    /// <summary>
    /// Converts a note name to its frequency in Hertz (if octave present), else null.
    /// </summary>
    /// <example>Note.Freq("A4") → 440.0</example>
    public static double? Freq(string noteName) => Get(noteName).Frequency;

    /// <summary>
    /// Returns the chroma (0–11) for the note/pitch class (ignoring octave) or null if invalid.
    /// </summary>
    /// <example>Note.Chroma("C") → 0</example>
    public static int? Chroma(string noteName) => Get(noteName).Chroma;

    /// <summary>
    /// Given a MIDI number (integer or fractional), returns a note name (flat spelling by default).
    /// </summary>
    /// <example>Note.FromMidi(61) → "Db4"</example>
    public static string FromMidi(int midi) => Tonal.Core.Midi.MidiToNoteName(midi, sharps: false, pitchClass: false);

    /// <summary>
    /// Given a MIDI number, returns a note name using sharps instead of flats.
    /// </summary>
    /// <example>Note.FromMidiSharps(61) → "C#4"</example>
    public static string FromMidiSharps(int midi) => Tonal.Core.Midi.MidiToNoteName(midi, sharps: true, pitchClass: false);

    /// <summary>
    /// Given a frequency in Hz, returns the nearest note name (flat spelling by default).
    /// </summary>
    /// <example>Note.FromFreq(440.0) → "A4"</example>
    public static string FromFreq(double frequency)
    {
        if (frequency <= 0 || double.IsNaN(frequency) || double.IsInfinity(frequency))
            return string.Empty;
        var midi = Tonal.Core.Midi.FreqToMidi(frequency);
        return Tonal.Core.Midi.MidiToNoteName(midi, sharps: false, pitchClass: false);
    }

    /// <summary>
    /// Given a frequency in Hz, returns the nearest note name using sharps.
    /// </summary>
    /// <example>Note.FromFreqSharps(550.0) → "C#5"</example>
    public static string FromFreqSharps(double frequency)
    {
        if (frequency <= 0 || double.IsNaN(frequency) || double.IsInfinity(frequency))
            return string.Empty;
        var midi = Tonal.Core.Midi.FreqToMidi(frequency);
        return Tonal.Core.Midi.MidiToNoteName(midi, sharps: true, pitchClass: false);
    }

    /// <summary>
    /// Transposes a note by an interval symbol and returns the resulting note name,
    /// or an empty string if invalid.
    /// </summary>
    /// <example>Note.Transpose("D", "3M") → "F#"</example>
    public static string Transpose(string noteName, string interval) 
    {
        var note = Get(noteName);
        var intervalCoord = Interval.Get(interval).Coord;
        if (note.Empty || intervalCoord == null) {
            return "";
        }
        var noteCoord = note.Coord;
        Coordinates tr;
        if (noteCoord is NoteCoordinates noteNCoord)
        {
            var fifths = noteNCoord.Fifths + intervalCoord.Fifths;
            var octaves = noteNCoord.Octaves + intervalCoord.Octaves;
            tr = new NoteCoordinates { Fifths = fifths, Octaves = octaves };
        }
        else
        {
            var notePcCoord = noteCoord as PitchClassCoordinates;
            var fifths = notePcCoord.Fifths + intervalCoord.Fifths;
            tr = new PitchClassCoordinates { Fifths = fifths };
        }

        return Note.Get(tr).Name;
    }
    
    /// <summary>
    /// Returns a function that transposes any given note by the specified interval.
    /// </summary>
    /// <example>var upFifth = Note.TransposeBy("5P"); upFifth("C") → "G"</example>
    public static Func<string, string> TransposeBy(string interval) => note => Transpose(note, interval);

    /// <summary>
    /// Returns a function that transposes from a fixed note by any given interval.
    /// </summary>
    /// <example>var fromC = Note.TransposeFrom("C"); fromC("5P") → "G"</example>
    public static Func<string, string> TransposeFrom(string noteName) => interval => Transpose(noteName, interval);

    /// <summary>
    /// Transposes a note by a number of perfect fifths up or down.
    /// </summary>
    /// <example>Note.TransposeFifths("G", 3) → "E"</example>
    public static string TransposeFifths(string noteName, int fifths)
    {
        var note = Get(noteName);
        if (note.Empty) return string.Empty;

        var coord = note.Coord;
        Coordinates newCoord;
        if (coord is NoteCoordinates ncCoord)
        {
            newCoord = new NoteCoordinates { Fifths = ncCoord.Fifths + fifths, Octaves = ncCoord.Octaves };
        }
        else
        {
            var pcCoord = coord as PitchClassCoordinates;
            newCoord = new PitchClassCoordinates { Fifths = pcCoord.Fifths + fifths };
        }
        return Get(newCoord).Name;
    }

    /// <summary>
    /// Calculates the interval between two notes (from → to), returning an interval symbol.
    /// </summary>
    /// <example>Note.Distance("C", "D") → "2M"</example>
    public static string Distance(string from, string to) => Interval.Distance(from, to);

    /// <summary>
    /// Given an array of values, returns an array of valid note names (normalized).
    /// If no argument is passed, returns the seven natural pitch classes.
    /// </summary>
    /// <example>Note.Names(["fx","bb",12]) → ["F##","Bb"]</example>
    public static IEnumerable<string> Names(IEnumerable<object>? items = null)
    {
        if (items == null)
        {
            return new[] { "C", "D", "E", "F", "G", "A", "B" };
        }

        return items
            .Select(item => item?.ToString() ?? "")
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => Name(name))
            .Where(name => !string.IsNullOrEmpty(name));
    }

    /// <summary>
    /// Sorts an array of note names ascending (or via optional comparator).
    /// Non-notes are filtered out.
    /// </summary>
    /// <example>Note.SortedNames(["c2","c5","c1"]) → ["C1","C2","C5"]</example>
    public static IEnumerable<string> SortedNames(IEnumerable<string> notes, IComparer<string>? comparer = null)
    {
        var validNotes = notes
            .Select(name => Name(name))
            .Where(name => !string.IsNullOrEmpty(name));

        if (comparer != null)
        {
            return validNotes.OrderBy(n => n, comparer);
        }
        else
        {
            return validNotes.OrderBy(n => Get(n).Height);
        }
    }

    /// <summary>
    /// Sorts note names ascending and removes duplicates.
    /// </summary>
    /// <example>Note.SortedUniqNames(["C4","C4","E4"]) → ["C4","E4"]</example>
    public static IEnumerable<string> SortedUniqNames(IEnumerable<string> notes) => SortedNames(notes).Distinct();

    /// <summary>
    /// Simplifies a note’s spelling to use fewer accidentals, or returns an empty string if invalid.
    /// </summary>
    /// <example>Note.Simplify("C###") → "D#"</example>
    public static string Simplify(string noteName)
    {
        var note = Get(noteName);
        if (note.Empty) {
            return "";
        }

        int midiValue = note.Midi ?? note.Chroma;

        return Tonal.Core.Midi.MidiToNoteName(
            midiValue, 
            sharps: note.Alteration > 0, 
            pitchClass: note.Midi == null);  
    }

    /// <summary>
    /// Returns the enharmonic equivalent of a note. Optionally, specify a target pitch class root.
    /// </summary>
    /// <example>Note.Enharmonic("C#") → "Db"</example>
    /// <example>Note.Enharmonic("F2", "E#") → "E#2"</example>
    public static string Enharmonic(string noteName, string? targetPitchClass = null)
    {
        var note = Get(noteName);
        if (note.Empty) return string.Empty;

        var midi = note.Midi;
        var chroma = note.Chroma;
        var isPitchClass = midi == null;

        if (targetPitchClass != null)
        {
            // Find enharmonic with the target pitch class
            var targetNote = Get(targetPitchClass);
            if (targetNote.Empty) return string.Empty;

            // Verify the notes are enharmonic (same chroma)
            int[] SEMI = {0, 2, 4, 5, 7, 9, 11}; // C, D, E, F, G, A, B
            int targetBaseSemitone = SEMI[targetNote.Step];
            int targetChroma = ((targetBaseSemitone + targetNote.Alteration) % 12 + 12) % 12;
            
            if (targetChroma != chroma) return string.Empty;

            // Build note name with target pitch class root
            var stepToLetter = "CDEFGAB";
            var letter = stepToLetter[targetNote.Step];
            var accidentals = targetNote.Alteration > 0
                ? new string('#', targetNote.Alteration)
                : targetNote.Alteration < 0
                    ? new string('b', -targetNote.Alteration)
                    : "";

            if (isPitchClass)
            {
                return letter + accidentals;
            }
            else
            {
                // Calculate octave: midi = (octave + 1) * 12 + baseSemitone + alt
                // octave = (midi - baseSemitone - alt) / 12 - 1
                int midiValue = midi.Value;
                int octave = (midiValue - targetBaseSemitone - targetNote.Alteration) / 12 - 1;
                return letter + accidentals + octave.ToString();
            }
        }
        else
        {
            // Standard enharmonic: sharp to flat or vice versa
            var sharps = note.Accidentals.Contains('#');
            var midiValue = midi ?? chroma;
            return Tonal.Core.Midi.MidiToNoteName(midiValue, sharps: !sharps, pitchClass: isPitchClass);
        }
    }
}

/// <summary>
/// Represents a structured musical note (like the output of Note.Get()).
/// </summary>
public class NoteInfo
{
    public string Name { get; init; } = string.Empty;
    public string PitchClass { get; init; } = string.Empty;
    public string Letter { get; init; } = string.Empty;
    public int Step { get; init; }
    public string Accidentals { get; init; } = string.Empty;
    public int Alteration { get; init; }
    public int? Octave { get; init; }
    public int Chroma { get; init; }
    public int? Midi { get; init; }
    public double? Frequency { get; init; }
    public int Height { get; init; }
    public bool Empty { get; init; }
    public Coordinates Coord { get; init; }
}

