using VoiceAssistent.Speech;

namespace VoiceAssistent.Tests;

public class VoiceActivityDetectorTests
{
    private const int SampleRate = 16000;
    private const int ChunkSize = 1600;

    private static readonly float[] SilentChunk = Chunk(0.0005f);
    private static readonly float[] LoudChunk = Chunk(0.2f);

    [Fact]
    public void Process_LoudAudioAfterSilence_ReportsSpeechStartedThenSpeaking()
    {
        var detector = new VoiceActivityDetector(SampleRate, TimeSpan.FromMilliseconds(300));

        Assert.Equal(VoiceActivity.Silence, detector.Process(SilentChunk));
        Assert.Equal(VoiceActivity.SpeechStarted, detector.Process(LoudChunk));
        Assert.Equal(VoiceActivity.Speaking, detector.Process(LoudChunk));
        Assert.True(detector.IsSpeaking);
    }

    [Fact]
    public void Process_SilenceShorterThanHangover_ReportsPausing()
    {
        var detector = new VoiceActivityDetector(SampleRate, TimeSpan.FromMilliseconds(300));
        detector.Process(LoudChunk);

        Assert.Equal(VoiceActivity.Pausing, detector.Process(SilentChunk));
        Assert.Equal(VoiceActivity.Pausing, detector.Process(SilentChunk));
        Assert.True(detector.IsSpeaking);
        Assert.Equal(VoiceActivity.Speaking, detector.Process(LoudChunk));
    }

    [Fact]
    public void Process_SilenceReachingHangover_ReportsSpeechEnded()
    {
        var detector = new VoiceActivityDetector(SampleRate, TimeSpan.FromMilliseconds(300));
        detector.Process(LoudChunk);

        detector.Process(SilentChunk);
        detector.Process(SilentChunk);
        var activity = detector.Process(SilentChunk);

        Assert.Equal(VoiceActivity.SpeechEnded, activity);
        Assert.False(detector.IsSpeaking);
    }

    [Fact]
    public void Process_SteadyBackgroundNoise_RaisesThreshold()
    {
        var detector = new VoiceActivityDetector(SampleRate, TimeSpan.FromMilliseconds(300));
        var noise = Chunk(0.004f);
        for (var index = 0; index < 200; index++)
        {
            detector.Process(noise);
        }

        Assert.Equal(VoiceActivity.Silence, detector.Process(Chunk(0.008f)));
        Assert.Equal(VoiceActivity.SpeechStarted, detector.Process(Chunk(0.05f)));
    }

    private static float[] Chunk(float amplitude) =>
        Enumerable.Range(0, ChunkSize).Select(index => index % 2 == 0 ? amplitude : -amplitude).ToArray();
}
