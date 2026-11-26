using NAudio.Wave;

namespace VoiceAssistent.Audio;

internal sealed class MonoDownmixSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _channels;
    private float[] _sourceBuffer = [];

    public MonoDownmixSampleProvider(ISampleProvider source)
    {
        _source = source;
        _channels = source.WaveFormat.Channels;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(Span<float> buffer)
    {
        var requiredSourceSamples = buffer.Length * _channels;
        if (_sourceBuffer.Length < requiredSourceSamples)
        {
            _sourceBuffer = new float[requiredSourceSamples];
        }

        var frames = _source.Read(_sourceBuffer.AsSpan(0, requiredSourceSamples)) / _channels;
        for (var frame = 0; frame < frames; frame++)
        {
            buffer[frame] = AverageChannels(frame);
        }

        return frames;
    }

    private float AverageChannels(int frame)
    {
        var sum = 0f;
        for (var channel = 0; channel < _channels; channel++)
        {
            sum += _sourceBuffer[frame * _channels + channel];
        }

        return sum / _channels;
    }
}
