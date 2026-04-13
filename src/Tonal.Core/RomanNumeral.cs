using System.Text.RegularExpressions;

namespace Tonal.Core;

/// <summary>
/// Provides functions for parsing and working with Roman numeral chord notation.
/// A Roman numeral encodes a scale degree (I–VII), an optional accidental alteration,
/// a major/minor quality (upper/lowercase), and an optional chord type suffix.
///
/// Examples: "I", "bVII", "#IV", "Imaj7", "iim7", "bVIImaj7"
///
/// Ported from Tonal.js RomanNumeral module:
/// https://github.com/tonaljs/tonal/blob/main/packages/roman-numeral/index.ts
/// </summary>
public static partial class RomanNumeral
{
    // Regex ported directly from the source:
    // /(#{1,}|b{1,}|x{1,}|)(IV|I{1,3}|VI{0,2}|iv|i{1,3}|vi{0,2})([^IViv]*)/
    [GeneratedRegex(
        @"^(#{1,}|b{1,}|x{1,}|)(IV|I{1,3}|VI{0,2}|iv|i{1,3}|vi{0,2})([^IViv]*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex RomanNumeralRegex();

    private static readonly string[] NamesUpper = ["I", "II", "III", "IV", "V", "VI", "VII"];
    private static readonly string[] NamesLower = ["i", "ii", "iii", "iv", "v", "vi", "vii"];

    private static readonly Dictionary<string, RomanNumeralInfo> _cache = [];

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a Roman numeral string and returns its properties.
    /// Results are cached for performance.
    /// Returns <see cref="RomanNumeralInfo.Empty"/> for unrecognised input.
    /// </summary>
    /// <example>RomanNumeral.Get("bVIImaj7") // => { Roman: "VII", Acc: "b", ChordType: "maj7", Major: true, Interval: "7m" }</example>
    /// <example>RomanNumeral.Get("iim7")     // => { Roman: "ii",  Acc: "",  ChordType: "m7",   Major: false, Interval: "2M" }</example>
    public static RomanNumeralInfo Get(string src)
    {
        if (_cache.TryGetValue(src, out var cached)) return cached;
        var result = Parse(src);
        _cache[src] = result;
        return result;
    }

    /// <summary>
    /// Returns a Roman numeral by its 0-based degree index (0 = I, 1 = II, ... 6 = VII).
    /// Returns <see cref="RomanNumeralInfo.Empty"/> for out-of-range indices.
    /// </summary>
    public static RomanNumeralInfo Get(int degreeIndex)
    {
        if (degreeIndex < 0 || degreeIndex >= NamesUpper.Length) return RomanNumeralInfo.Empty;
        return Get(NamesUpper[degreeIndex]);
    }

    /// <summary>
    /// Splits a Roman numeral string into its component parts without full parsing.
    /// Returns (fullMatch, accidentals, romanNumeral, chordType).
    /// Returns empty strings for all parts if the input is not a valid Roman numeral.
    /// </summary>
    /// <example>RomanNumeral.Tokenize("bVIImaj7") // => ("bVIImaj7", "b", "VII", "maj7")</example>
    public static (string Name, string Acc, string Roman, string ChordType) Tokenize(string src)
    {
        var m = RomanNumeralRegex().Match(src);
        if (!m.Success) return ("", "", "", "");
        return (m.Value, m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
    }

    /// <summary>
    /// Returns the canonical Roman numeral names for a major or minor key.
    /// </summary>
    /// <param name="major">If true (default), returns uppercase names (I–VII); otherwise lowercase (i–vii).</param>
    /// <example>RomanNumeral.Names()        // => ["I", "II", "III", "IV", "V", "VI", "VII"]</example>
    /// <example>RomanNumeral.Names(false)   // => ["i", "ii", "iii", "iv", "v", "vi", "vii"]</example>
    public static IReadOnlyList<string> Names(bool major = true) =>
        major ? NamesUpper : NamesLower;

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private static RomanNumeralInfo Parse(string src)
    {
        var (name, acc, roman, chordType) = Tokenize(src);
        if (string.IsNullOrEmpty(roman)) return RomanNumeralInfo.Empty;

        var upperRoman = roman.ToUpperInvariant();
        var step       = Array.IndexOf(NamesUpper, upperRoman);
        var alt        = AccToAlt(acc);
        var isMajor    = roman == upperRoman;
        var interval   = DeriveInterval(step, alt);

        return new RomanNumeralInfo
        {
            Name      = name,
            Roman     = roman,
            Acc       = acc,
            ChordType = chordType,
            Step      = step,
            Alt       = alt,
            Major     = isMajor,
            Interval  = interval,
        };
    }

    /// <summary>
    /// Converts an accidental string to a numeric alteration.
    /// '#' → 1, 'b' → -1, 'x' → 2 (double sharp), '' → 0.
    /// </summary>
    private static int AccToAlt(string acc)
    {
        if (string.IsNullOrEmpty(acc)) return 0;
        if (acc == "x") return 2;
        int alt = 0;
        foreach (var c in acc)
        {
            if (c == '#') alt++;
            else if (c == 'b') alt--;
            else if (c == 'x') alt += 2;
        }
        return alt;
    }

    /// <summary>
    /// Derives the interval name for a given step (0–6) and alteration,
    /// matching the logic in Interval.cs (AltToQ + step → interval name).
    /// </summary>
    private static string DeriveInterval(int step, int alt)
    {
        if (step < 0 || step > 6) return "";

        // TYPES: P=perfectable (I,IV,V), M=majorable (II,III,VI,VII)
        // Matches Pitch.cs TYPES = "PMMPPMM"
        const string Types = "PMMPPMM";
        var type  = Types[step];
        var num   = step + 1; // octave 0

        string quality;
        if (alt == 0)
        {
            quality = type == 'P' ? "P" : "M";
        }
        else if (alt == -1 && type == 'M')
        {
            quality = "m";
        }
        else if (alt > 0)
        {
            quality = new string('A', alt);
        }
        else
        {
            // diminished
            var dCount = type == 'P' ? -alt : -(alt + 1);
            quality = new string('d', dCount);
        }

        return $"{num}{quality}";
    }
}

/// <summary>
/// The properties of a parsed Roman numeral.
/// </summary>
public record RomanNumeralInfo
{
    /// <summary>The full input string (e.g. "bVIImaj7").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The Roman numeral part only, preserving original case (e.g. "VII", "ii").</summary>
    public string Roman { get; init; } = string.Empty;

    /// <summary>The accidental prefix (e.g. "b", "#", "bb", "x", or "").</summary>
    public string Acc { get; init; } = string.Empty;

    /// <summary>The chord type suffix (e.g. "maj7", "m7b5", "7", or "").</summary>
    public string ChordType { get; init; } = string.Empty;

    /// <summary>0-based scale degree (I=0, II=1, ..., VII=6).</summary>
    public int Step { get; init; }

    /// <summary>Numeric alteration derived from the accidental (+1 per sharp, -1 per flat).</summary>
    public int Alt { get; init; }

    /// <summary>True if the Roman numeral is uppercase (major quality); false if lowercase (minor).</summary>
    public bool Major { get; init; }

    /// <summary>
    /// The interval from the tonic this Roman numeral represents
    /// (e.g. "I"→"1P", "bVII"→"7m", "#IV"→"4A").
    /// </summary>
    public string Interval { get; init; } = string.Empty;

    /// <summary>True if this is the sentinel empty result.</summary>
    public bool IsEmpty { get; init; }

    /// <summary>Sentinel returned for invalid or unrecognised input.</summary>
    public static readonly RomanNumeralInfo Empty = new() { IsEmpty = true };
}
