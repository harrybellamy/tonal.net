using Xunit;

namespace Tonal.Core.Tests;

public class MidiTests
{
    [Theory]
    [InlineData(100, 100)]
    [InlineData("C4", 60)]
    [InlineData("60", 60)]
    [InlineData(0, 0)]
    [InlineData("0", 0)]
    [InlineData(-1, null)]
    [InlineData(128, null)]
    [InlineData("blah", null)]
    public void ToMidi_ReturnsExpected(object input, int? expected)
    {
        int? result = input switch
        {
            int i => Midi.ToMidi(i),
            string s => Midi.ToMidi(s),
            _ => null,
        };
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(220.0, 57.0, 0.001)]
    [InlineData(261.62, 60.0, 0.02)]
    [InlineData(261.0, 59.96, 0.05)]
    public void FreqToMidi_ReturnsExpected(double freq, double expected, double tolerance)
    {
        var result = Midi.FreqToMidi(freq);
        Assert.InRange(result, expected - tolerance, expected + tolerance);
    }

    [Theory]
    [InlineData(60, 440.0, 261.6255653005986)]
    [InlineData(69, 443.0, 443.0)]
    public void MidiToFreq_ReturnsExpected(int midi, double tuning, double expected)
    {
        var result = Midi.MidiToFreq(midi, tuning);
        Assert.InRange(result, expected - 0.0001, expected + 0.0001);
    }

    [Theory]
    [InlineData(60, "C4")]
    [InlineData(61, "Db4")]
    [InlineData(62, "D4")]
    [InlineData(63, "Eb4")]
    [InlineData(64, "E4")]
    [InlineData(65, "F4")]
    [InlineData(66, "Gb4")]
    [InlineData(67, "G4")]
    [InlineData(68, "Ab4")]
    [InlineData(69, "A4")]
    [InlineData(70, "Bb4")]
    [InlineData(71, "B4")]
    [InlineData(72, "C5")]
    public void MidiToNoteName_WithFlats_ReturnsExpected(int midi, string expected)
    {
        var result = Midi.MidiToNoteName(midi, sharps: false, pitchClass: false);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(60, "C4")]
    [InlineData(61, "C#4")]
    [InlineData(62, "D4")]
    [InlineData(63, "D#4")]
    [InlineData(64, "E4")]
    [InlineData(65, "F4")]
    [InlineData(66, "F#4")]
    [InlineData(67, "G4")]
    [InlineData(68, "G#4")]
    [InlineData(69, "A4")]
    [InlineData(70, "A#4")]
    [InlineData(71, "B4")]
    [InlineData(72, "C5")]
    public void MidiToNoteName_WithSharps_ReturnsExpected(int midi, string expected)
    {
        var result = Midi.MidiToNoteName(midi, sharps: true, pitchClass: false);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(60, "C")]
    [InlineData(61, "Db")]
    [InlineData(62, "D")]
    [InlineData(63, "Eb")]
    [InlineData(64, "E")]
    [InlineData(65, "F")]
    [InlineData(66, "Gb")]
    [InlineData(67, "G")]
    [InlineData(68, "Ab")]
    [InlineData(69, "A")]
    [InlineData(70, "Bb")]
    [InlineData(71, "B")]
    [InlineData(72, "C")]
    public void MidiToNoteName_PitchClassOnly_ReturnsExpected(int midi, string expected)
    {
        var result = Midi.MidiToNoteName(midi, sharps: false, pitchClass: true);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(double.NaN, "")]
    [InlineData(double.NegativeInfinity, "")]
    [InlineData(double.PositiveInfinity, "")]
    public void MidiToNoteName_WithInvalidInput_ReturnsEmpty(double midi, string expected)
    {
        var result = Midi.MidiToNoteName(midi);
        Assert.Equal(expected, result);
    }

    [Fact(Skip = "Not yet implemented: Midi.Pcset")]
    public void Pcset_FromChroma_ReturnsExpected()
    {
        // expect(Midi.pcset("100100100101")).toEqual([0, 3, 6, 9, 11]);
    }

    [Fact(Skip = "Not yet implemented: Midi.Pcset")]
    public void Pcset_FromMidi_ReturnsExpected()
    {
        // expect(Midi.pcset([62, 63, 60, 65, 70, 72])).toEqual([0, 2, 3, 5, 10]);
    }

    [Fact(Skip = "Not yet implemented: Midi.PcsetNearest")]
    public void PcsetNearest_FindNearestUpwards_ReturnsExpected()
    {
        // const nearest = Midi.pcsetNearest([0, 5, 7]);
        // expect([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map(nearest)).toEqual([
        //   0, 0, 0, 5, 5, 5, 7, 7, 7, 7, 12, 12, 12,
        // ]);
    }

    [Fact(Skip = "Not yet implemented: Midi.PcsetNearest")]
    public void PcsetNearest_ChromaticToCMinorPentatonic_ReturnsExpected()
    {
        // const nearest = Midi.pcsetNearest("100101010010");
        // expect(
        //   [36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47].map(nearest),
        // ).toEqual([36, 36, 39, 39, 41, 41, 43, 43, 43, 46, 46, 48]);
    }

    [Fact(Skip = "Not yet implemented: Midi.PcsetNearest")]
    public void PcsetNearest_ChromaticToHalfOctave_ReturnsExpected()
    {
        // const nearest = Midi.pcsetNearest("100000100000");
        // expect(
        //   [36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47].map(nearest),
        // ).toEqual([36, 36, 36, 42, 42, 42, 42, 42, 42, 48, 48, 48]);
    }

    [Fact(Skip = "Not yet implemented: Midi.PcsetNearest")]
    public void PcsetNearest_EmptyPcset_ReturnsUndefined()
    {
        // expect([10, 30, 40].map(Midi.pcsetNearest([]))).toEqual([
        //   undefined,
        //   undefined,
        //   undefined,
        // ]);
    }
}