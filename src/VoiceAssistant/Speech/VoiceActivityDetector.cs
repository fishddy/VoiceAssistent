using VoiceAssistant.Audio;

namespace VoiceAssistant.Speech;

public sealed class VoiceActivityDetector
{
    public const float MinimumSpeechLevel = 0.005f;
    public const float SpeechToNoiseRatio = 3f;
    private const float NoiseFloorAdaptationRate = 0.05f;

    private readonly int _hangoverSampleCount;
    private float _noiseFloor;
    private int _silentSamplesWhileSpeaking;

    public VoiceActivityDetector(int sampleRate, TimeSpan hangover)
    {
        _hangoverSampleCount = (int)(sampleRate * hangover.TotalSeconds);
    }

    public bool IsSpeaking { get; private set; }

    public VoiceActivity Process(ReadOnlySpan<float> samples)
    {
        var level = AudioSamples.RootMeanSquare(samples);

        if (level >= CurrentThreshold)
        {
            return ContinueSpeech();
        }

        return IsSpeaking ? CountSilenceWhileSpeaking(samples.Length) : TrackNoise(level);
    }

    public void Reset()
    {
        IsSpeaking = false;
        _silentSamplesWhileSpeaking = 0;
    }

    private float CurrentThreshold => Math.Max(MinimumSpeechLevel, _noiseFloor * SpeechToNoiseRatio);

    private VoiceActivity ContinueSpeech()
    {
        _silentSamplesWhileSpeaking = 0;
        if (IsSpeaking)
        {
            return VoiceActivity.Speaking;
        }

        IsSpeaking = true;
        return VoiceActivity.SpeechStarted;
    }

    private VoiceActivity CountSilenceWhileSpeaking(int sampleCount)
    {
        _silentSamplesWhileSpeaking += sampleCount;
        if (_silentSamplesWhileSpeaking < _hangoverSampleCount)
        {
            return VoiceActivity.Pausing;
        }

        Reset();
        return VoiceActivity.SpeechEnded;
    }

    private VoiceActivity TrackNoise(float level)
    {
        _noiseFloor += (level - _noiseFloor) * NoiseFloorAdaptationRate;
        return VoiceActivity.Silence;
    }
}
