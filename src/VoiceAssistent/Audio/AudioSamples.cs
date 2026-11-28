namespace VoiceAssistent.Audio;

public static class AudioSamples
{
    private const double SilenceFloorDecibels = -60;

    public static byte[] ToPcm16(ReadOnlySpan<float> samples)
    {
        var bytes = new byte[samples.Length * sizeof(short)];
        for (var index = 0; index < samples.Length; index++)
        {
            var value = (short)(Math.Clamp(samples[index], -1f, 1f) * short.MaxValue);
            bytes[index * 2] = (byte)value;
            bytes[index * 2 + 1] = (byte)(value >> 8);
        }

        return bytes;
    }

    public static float[] FromPcm16(ReadOnlySpan<byte> bytes)
    {
        var samples = new float[bytes.Length / sizeof(short)];
        for (var index = 0; index < samples.Length; index++)
        {
            var value = (short)(bytes[index * 2] | (bytes[index * 2 + 1] << 8));
            samples[index] = value / (float)short.MaxValue;
        }

        return samples;
    }

    public static float RootMeanSquare(ReadOnlySpan<float> samples)
    {
        if (samples.IsEmpty)
        {
            return 0;
        }

        var sumOfSquares = 0.0;
        foreach (var sample in samples)
        {
            sumOfSquares += sample * sample;
        }

        return (float)Math.Sqrt(sumOfSquares / samples.Length);
    }

    public static double ToDisplayLevel(float rootMeanSquare)
    {
        if (rootMeanSquare <= 0)
        {
            return 0;
        }

        var decibels = 20 * Math.Log10(rootMeanSquare);
        return Math.Clamp((decibels - SilenceFloorDecibels) / -SilenceFloorDecibels, 0, 1);
    }
}
