namespace VoiceAssistent.Speech;

public sealed class DictationSession : IAsyncDisposable
{
    private static readonly TimeSpan EndOfUtteranceSilence = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan PartialTranscriptionInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MinimumSpeechLength = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan MaximumUtteranceLength = TimeSpan.FromSeconds(28);
    private static readonly TimeSpan PreRollLength = TimeSpan.FromMilliseconds(300);

    private readonly ISpeechTranscriber _transcriber;
    private readonly VoiceActivityDetector _voiceActivity;
    private readonly int _partialIntervalSamples;
    private readonly int _minimumSpeechSamples;
    private readonly int _maximumUtteranceSamples;
    private readonly int _preRollSamples;
    private readonly Lock _stateLock = new();
    private readonly SemaphoreSlim _transcriptionSlot = new(1, 1);
    private readonly CancellationTokenSource _disposal = new();
    private readonly List<float> _utterance = [];
    private readonly Queue<float[]> _preRoll = new();
    private int _preRollSampleCount;
    private int _samplesAtLastPartial;
    private int _speechSampleCount;
    private int _utteranceGeneration;
    private int _sessionGeneration;
    private bool _isActive;

    public DictationSession(ISpeechTranscriber transcriber, int sampleRate)
    {
        _transcriber = transcriber;
        _voiceActivity = new VoiceActivityDetector(sampleRate, EndOfUtteranceSilence);
        _partialIntervalSamples = SampleCount(sampleRate, PartialTranscriptionInterval);
        _minimumSpeechSamples = SampleCount(sampleRate, MinimumSpeechLength);
        _maximumUtteranceSamples = SampleCount(sampleRate, MaximumUtteranceLength);
        _preRollSamples = SampleCount(sampleRate, PreRollLength);
    }

    public event EventHandler? SpeechStarted;

    public event EventHandler? SpeechEnded;

    public event EventHandler<string>? PartialTranscriptReceived;

    public event EventHandler<string>? FinalTranscriptReceived;

    public event EventHandler<string>? TranscriptionFailed;

    public void Start()
    {
        lock (_stateLock)
        {
            _isActive = true;
            _sessionGeneration++;
            _voiceActivity.Reset();
            ResetUtterance();
        }
    }

    public void Stop()
    {
        lock (_stateLock)
        {
            _isActive = false;
            _sessionGeneration++;
            ResetUtterance();
        }
    }

    public void AcceptSamples(float[] samples)
    {
        TranscriptionRequest? request;
        VoiceActivity activity;

        lock (_stateLock)
        {
            if (!_isActive)
            {
                return;
            }

            activity = _voiceActivity.Process(samples);
            request = HandleActivity(activity, samples);
        }

        RaiseSpeechBoundary(activity);

        if (request is not null)
        {
            _ = TranscribeAsync(request);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        await _disposal.CancelAsync();
        await _transcriptionSlot.WaitAsync();
        _disposal.Dispose();
    }

    private static int SampleCount(int sampleRate, TimeSpan duration) => (int)(sampleRate * duration.TotalSeconds);

    private void RaiseSpeechBoundary(VoiceActivity activity)
    {
        var handler = activity switch
        {
            VoiceActivity.SpeechStarted => SpeechStarted,
            VoiceActivity.SpeechEnded => SpeechEnded,
            _ => null
        };
        handler?.Invoke(this, EventArgs.Empty);
    }

    private TranscriptionRequest? HandleActivity(VoiceActivity activity, float[] samples)
    {
        switch (activity)
        {
            case VoiceActivity.Silence:
                AddToPreRoll(samples);
                return null;
            case VoiceActivity.SpeechStarted:
                MovePreRollIntoUtterance();
                _utterance.AddRange(samples);
                _speechSampleCount += samples.Length;
                return null;
            case VoiceActivity.Speaking:
                _utterance.AddRange(samples);
                _speechSampleCount += samples.Length;
                return _utterance.Count >= _maximumUtteranceSamples ? CompleteUtterance() : RequestPartialIfDue();
            case VoiceActivity.Pausing:
                _utterance.AddRange(samples);
                return _utterance.Count >= _maximumUtteranceSamples ? CompleteUtterance() : null;
            case VoiceActivity.SpeechEnded:
                _utterance.AddRange(samples);
                return CompleteUtterance();
            default:
                throw new ArgumentOutOfRangeException(nameof(activity), activity, "Unknown voice activity.");
        }
    }

    private TranscriptionRequest? RequestPartialIfDue()
    {
        if (_utterance.Count - _samplesAtLastPartial < _partialIntervalSamples)
        {
            return null;
        }

        _samplesAtLastPartial = _utterance.Count;
        return new TranscriptionRequest([.. _utterance], IsFinal: false, _utteranceGeneration, _sessionGeneration);
    }

    private TranscriptionRequest? CompleteUtterance()
    {
        var samples = _utterance.ToArray();
        var hasEnoughSpeech = _speechSampleCount >= _minimumSpeechSamples;
        var generation = _utteranceGeneration;
        ResetUtterance();

        return !hasEnoughSpeech
            ? null
            : new TranscriptionRequest(samples, IsFinal: true, generation, _sessionGeneration);
    }

    private void ResetUtterance()
    {
        _utteranceGeneration++;
        _utterance.Clear();
        _samplesAtLastPartial = 0;
        _speechSampleCount = 0;
        _preRoll.Clear();
        _preRollSampleCount = 0;
    }

    private void AddToPreRoll(float[] samples)
    {
        _preRoll.Enqueue(samples);
        _preRollSampleCount += samples.Length;

        while (_preRollSampleCount - _preRoll.Peek().Length >= _preRollSamples)
        {
            _preRollSampleCount -= _preRoll.Dequeue().Length;
        }
    }

    private void MovePreRollIntoUtterance()
    {
        foreach (var chunk in _preRoll)
        {
            _utterance.AddRange(chunk);
        }

        _preRoll.Clear();
        _preRollSampleCount = 0;
    }

    private async Task TranscribeAsync(TranscriptionRequest request)
    {
        var cancellationToken = _disposal.Token;
        if (!await TryEnterTranscriptionSlotAsync(request.IsFinal, cancellationToken))
        {
            return;
        }

        try
        {
            var text = await _transcriber.TranscribeAsync(request.Samples, cancellationToken);
            Publish(request, text.Trim());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            TranscriptionFailed?.Invoke(this, $"Transcription failed: {exception.Message}");
        }
        finally
        {
            _transcriptionSlot.Release();
        }
    }

    private async Task<bool> TryEnterTranscriptionSlotAsync(bool waitForTurn, CancellationToken cancellationToken)
    {
        if (!waitForTurn)
        {
            return _transcriptionSlot.Wait(0, CancellationToken.None);
        }

        try
        {
            await _transcriptionSlot.WaitAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private void Publish(TranscriptionRequest request, string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        bool isCurrent;
        lock (_stateLock)
        {
            isCurrent = request.SessionGeneration == _sessionGeneration
                && (request.IsFinal || request.UtteranceGeneration == _utteranceGeneration);
        }

        if (!isCurrent)
        {
            return;
        }

        var handler = request.IsFinal ? FinalTranscriptReceived : PartialTranscriptReceived;
        handler?.Invoke(this, text);
    }

    private sealed record TranscriptionRequest(
        float[] Samples,
        bool IsFinal,
        int UtteranceGeneration,
        int SessionGeneration);
}
