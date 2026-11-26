using Microsoft.Extensions.Time.Testing;
using VoiceAssistent.Chat;

namespace VoiceAssistent.Tests;

public sealed class UtteranceBatcherTests : IDisposable
{
    private static readonly TimeSpan Pause = TimeSpan.FromSeconds(1);

    private readonly FakeTimeProvider _time = new();
    private readonly UtteranceBatcher _batcher;
    private int _batches;

    public UtteranceBatcherTests()
    {
        _batcher = new UtteranceBatcher(Pause, _time);
        _batcher.BatchReady += (_, _) => _batches++;
    }

    [Fact]
    public void TextAdded_ThenPause_ReportsOneBatch()
    {
        _batcher.TextAdded();

        _time.Advance(Pause - TimeSpan.FromMilliseconds(1));
        Assert.Equal(0, _batches);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        _time.Advance(Pause * 3);
        Assert.Equal(1, _batches);
    }

    [Fact]
    public void SpeakingAgainBeforePause_WaitsUntilSpeechEnds()
    {
        _batcher.TextAdded();
        _time.Advance(Pause / 2);

        _batcher.SpeechStarted();
        _time.Advance(Pause * 5);
        Assert.Equal(0, _batches);

        _batcher.SpeechEnded();
        _time.Advance(Pause);
        Assert.Equal(1, _batches);
    }

    [Fact]
    public void MoreTextBeforePause_RestartsWaiting()
    {
        _batcher.TextAdded();
        _time.Advance(Pause / 2);
        _batcher.TextAdded();
        _time.Advance(Pause / 2 + TimeSpan.FromMilliseconds(100));

        Assert.Equal(0, _batches);

        _time.Advance(Pause / 2);
        Assert.Equal(1, _batches);
    }

    [Fact]
    public void SpeechEndedWithoutText_ReportsNothing()
    {
        _batcher.SpeechStarted();
        _batcher.SpeechEnded();
        _time.Advance(Pause * 3);

        Assert.Equal(0, _batches);
    }

    [Fact]
    public void Reset_DiscardsPendingBatch()
    {
        _batcher.TextAdded();
        _batcher.Reset();
        _time.Advance(Pause * 3);

        Assert.Equal(0, _batches);
    }

    public void Dispose() => _batcher.Dispose();
}
