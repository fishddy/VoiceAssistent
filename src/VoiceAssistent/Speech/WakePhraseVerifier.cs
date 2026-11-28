namespace VoiceAssistent.Speech;

public sealed class WakePhraseVerifier : IAsyncDisposable
{
    private readonly ISpeechTranscriber _transcriber;
    private readonly IReadOnlyList<string[]> _wakePhrases;
    private readonly SemaphoreSlim _transcriptionSlot = new(1, 1);
    private readonly CancellationTokenSource _disposal = new();

    public WakePhraseVerifier(ISpeechTranscriber transcriber, IEnumerable<string> wakePhrases)
    {
        _transcriber = transcriber;
        _wakePhrases = PhraseWords.SplitAll(wakePhrases);
    }

    public async Task<bool> IsWakePhraseAsync(float[] samples)
    {
        if (!_transcriptionSlot.Wait(0))
        {
            return false;
        }

        var cancellationToken = _disposal.Token;
        try
        {
            var transcript = await Task.Run(() => _transcriber.TranscribeAsync(samples, cancellationToken), cancellationToken);
            return ContainsWakePhrase(transcript);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            _transcriptionSlot.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _disposal.CancelAsync();
        await _transcriptionSlot.WaitAsync();
        _disposal.Dispose();
    }

    private bool ContainsWakePhrase(string transcript)
    {
        var words = PhraseWords.Split(transcript);
        return _wakePhrases.Any(phrase => words.AsSpan().IndexOf(phrase) >= 0);
    }
}
