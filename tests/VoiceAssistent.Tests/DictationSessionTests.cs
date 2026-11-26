using System.Collections.Concurrent;
using VoiceAssistent.Speech;

namespace VoiceAssistent.Tests;

public class DictationSessionTests
{
    private const int SampleRate = 16000;
    private const int ChunkSize = 1600;
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Speech_FollowedBySilence_PublishesFinalTranscript()
    {
        var transcriber = new FakeTranscriber(_ => "wie heiße ich");
        await using var session = new DictationSession(transcriber, SampleRate);
        var finals = new BlockingCollection<string>();
        session.FinalTranscriptReceived += (_, text) => finals.Add(text);
        session.Start();

        Feed(session, Loud, chunks: 5);
        Feed(session, Silent, chunks: 10);

        Assert.True(finals.TryTake(out var final, EventTimeout));
        Assert.Equal("wie heiße ich", final);
    }

    [Fact]
    public async Task LongSpeech_PublishesPartialTranscriptsWhileSpeaking()
    {
        var transcriber = new FakeTranscriber(samples => $"partial {samples.Length}");
        await using var session = new DictationSession(transcriber, SampleRate);
        var partials = new BlockingCollection<string>();
        session.PartialTranscriptReceived += (_, text) => partials.Add(text);
        session.Start();

        Feed(session, Loud, chunks: 15);

        Assert.True(partials.TryTake(out var partial, EventTimeout));
        Assert.StartsWith("partial", partial);
    }

    [Fact]
    public async Task SpeechBoundaries_AreReported()
    {
        var transcriber = new FakeTranscriber(_ => "text");
        await using var session = new DictationSession(transcriber, SampleRate);
        var boundaries = new List<string>();
        session.SpeechStarted += (_, _) => boundaries.Add("started");
        session.SpeechEnded += (_, _) => boundaries.Add("ended");
        session.Start();

        Feed(session, Silent, chunks: 2);
        Feed(session, Loud, chunks: 5);
        Feed(session, Silent, chunks: 10);

        Assert.Equal(["started", "ended"], boundaries);
    }

    [Fact]
    public async Task ShortClick_IsNotTranscribed()
    {
        var transcriber = new FakeTranscriber(_ => "noise");
        await using var session = new DictationSession(transcriber, SampleRate);
        session.Start();

        Feed(session, Loud, chunks: 1);
        Feed(session, Silent, chunks: 10);
        await Task.Delay(100);

        Assert.Equal(0, transcriber.CallCount);
    }

    [Fact]
    public async Task Samples_WhileStopped_AreIgnored()
    {
        var transcriber = new FakeTranscriber(_ => "ignored");
        await using var session = new DictationSession(transcriber, SampleRate);
        var published = false;
        session.PartialTranscriptReceived += (_, _) => published = true;
        session.FinalTranscriptReceived += (_, _) => published = true;

        Feed(session, Loud, chunks: 15);
        Feed(session, Silent, chunks: 10);
        await Task.Delay(100);

        Assert.False(published);
        Assert.Equal(0, transcriber.CallCount);
    }

    [Fact]
    public async Task FinalTranscript_IncludesSpeechOnsetFromPreRoll()
    {
        var transcriber = new FakeTranscriber(samples => samples.Length.ToString());
        await using var session = new DictationSession(transcriber, SampleRate);
        var finals = new BlockingCollection<string>();
        session.FinalTranscriptReceived += (_, text) => finals.Add(text);
        session.Start();

        Feed(session, Silent, chunks: 5);
        Feed(session, Loud, chunks: 5);
        Feed(session, Silent, chunks: 10);

        Assert.True(finals.TryTake(out var final, EventTimeout));
        Assert.True(int.Parse(final) > 5 * ChunkSize + 8 * ChunkSize);
    }

    [Fact]
    public async Task TranscriberFailure_IsReported()
    {
        var transcriber = new FakeTranscriber(_ => throw new InvalidOperationException("model broken"));
        await using var session = new DictationSession(transcriber, SampleRate);
        var failures = new BlockingCollection<string>();
        session.TranscriptionFailed += (_, message) => failures.Add(message);
        session.Start();

        Feed(session, Loud, chunks: 5);
        Feed(session, Silent, chunks: 10);

        Assert.True(failures.TryTake(out var failure, EventTimeout));
        Assert.Contains("model broken", failure);
    }

    private static float[] Loud => Chunk(0.2f);

    private static float[] Silent => Chunk(0.0005f);

    private static void Feed(DictationSession session, float[] chunk, int chunks)
    {
        for (var index = 0; index < chunks; index++)
        {
            session.AcceptSamples(chunk);
        }
    }

    private static float[] Chunk(float amplitude) =>
        Enumerable.Range(0, ChunkSize).Select(index => index % 2 == 0 ? amplitude : -amplitude).ToArray();

    private sealed class FakeTranscriber(Func<float[], string> transcribe) : ISpeechTranscriber
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task<string> TranscribeAsync(float[] samples, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(transcribe(samples));
        }
    }
}
