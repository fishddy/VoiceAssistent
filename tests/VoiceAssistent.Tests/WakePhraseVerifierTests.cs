using VoiceAssistent.Speech;

namespace VoiceAssistent.Tests;

public class WakePhraseVerifierTests
{
    private static readonly string[] WakePhrases = ["hey james"];

    [Theory]
    [InlineData("Hey James!")]
    [InlineData("hey james")]
    [InlineData("Hey, James.")]
    [InlineData("Okay, hey James")]
    public async Task IsWakePhraseAsync_TranscriptContainsPhrase_ReturnsTrue(string transcript)
    {
        await using var verifier = new WakePhraseVerifier(new FixedTranscriber(transcript), WakePhrases);

        Assert.True(await verifier.IsWakePhraseAsync(new float[16]));
    }

    [Theory]
    [InlineData("Hey games!")]
    [InlineData("Hey Jane.")]
    [InlineData("James")]
    [InlineData("Hey Jameson")]
    [InlineData("Hello James")]
    [InlineData("")]
    public async Task IsWakePhraseAsync_TranscriptWithoutPhrase_ReturnsFalse(string transcript)
    {
        await using var verifier = new WakePhraseVerifier(new FixedTranscriber(transcript), WakePhrases);

        Assert.False(await verifier.IsWakePhraseAsync(new float[16]));
    }

    [Fact]
    public async Task IsWakePhraseAsync_AnyOfSeveralPhrases_ReturnsTrue()
    {
        await using var verifier = new WakePhraseVerifier(new FixedTranscriber("Hey Claude"), ["hey james", "hey claude"]);

        Assert.True(await verifier.IsWakePhraseAsync(new float[16]));
    }

    [Fact]
    public async Task IsWakePhraseAsync_WhileCheckIsRunning_RejectsSecondCandidate()
    {
        var transcriber = new BlockingTranscriber("hey james");
        await using var verifier = new WakePhraseVerifier(transcriber, WakePhrases);

        var first = verifier.IsWakePhraseAsync(new float[16]);
        await transcriber.Started;
        var second = await verifier.IsWakePhraseAsync(new float[16]);
        transcriber.Finish();

        Assert.False(second);
        Assert.True(await first);
    }

    [Fact]
    public async Task DisposeAsync_WhileCheckIsRunning_CancelsAndRejects()
    {
        var transcriber = new BlockingTranscriber("hey james");
        var verifier = new WakePhraseVerifier(transcriber, WakePhrases);

        var check = verifier.IsWakePhraseAsync(new float[16]);
        await transcriber.Started;
        await verifier.DisposeAsync();

        Assert.False(await check);
    }

    private sealed class FixedTranscriber(string transcript) : ISpeechTranscriber
    {
        public Task<string> TranscribeAsync(float[] samples, CancellationToken cancellationToken) =>
            Task.FromResult(transcript);
    }

    private sealed class BlockingTranscriber(string transcript) : ISpeechTranscriber
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _finish = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public void Finish() => _finish.TrySetResult();

        public async Task<string> TranscribeAsync(float[] samples, CancellationToken cancellationToken)
        {
            _started.TrySetResult();
            await _finish.Task.WaitAsync(cancellationToken);
            return transcript;
        }
    }
}
