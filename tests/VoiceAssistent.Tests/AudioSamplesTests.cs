using VoiceAssistent.Audio;

namespace VoiceAssistent.Tests;

public class AudioSamplesTests
{
    [Fact]
    public void ToPcm16_ConvertsAndClampsToLittleEndianShorts()
    {
        var bytes = AudioSamples.ToPcm16([0f, 1f, -2f]);

        Assert.Equal(6, bytes.Length);
        Assert.Equal(0, BitConverter.ToInt16(bytes, 0));
        Assert.Equal(short.MaxValue, BitConverter.ToInt16(bytes, 2));
        Assert.Equal(-short.MaxValue, BitConverter.ToInt16(bytes, 4));
    }

    [Fact]
    public void FromPcm16_ReversesToPcm16()
    {
        float[] original = [0f, 0.5f, -0.25f, 1f, -1f];

        var roundTripped = AudioSamples.FromPcm16(AudioSamples.ToPcm16(original));

        Assert.Equal(original.Length, roundTripped.Length);
        for (var index = 0; index < original.Length; index++)
        {
            Assert.Equal(original[index], roundTripped[index], precision: 4);
        }
    }

    [Fact]
    public void FromPcm16_IgnoresTrailingOddByte()
    {
        Assert.Single(AudioSamples.FromPcm16([0, 0, 7]));
    }

    [Fact]
    public void RootMeanSquare_ComputesSignalLevel()
    {
        Assert.Equal(0.5f, AudioSamples.RootMeanSquare([0.5f, -0.5f, 0.5f, -0.5f]), precision: 5);
        Assert.Equal(0f, AudioSamples.RootMeanSquare([]));
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.0001f, 0)]
    [InlineData(0.001f, 0)]
    [InlineData(0.0316228f, 0.5)]
    [InlineData(1f, 1)]
    public void ToDisplayLevel_MapsDecibelsToZeroToOne(float rootMeanSquare, double expected)
    {
        Assert.Equal(expected, AudioSamples.ToDisplayLevel(rootMeanSquare), precision: 3);
    }
}
