namespace VoiceAssistant.Chat;

public sealed class UtteranceBatcher : IDisposable
{
    private readonly TimeSpan _pauseBeforeSending;
    private readonly ITimer _timer;
    private readonly Lock _stateLock = new();
    private bool _isSpeaking;
    private bool _hasPendingText;

    public UtteranceBatcher(TimeSpan pauseBeforeSending, TimeProvider timeProvider)
    {
        _pauseBeforeSending = pauseBeforeSending;
        _timer = timeProvider.CreateTimer(_ => OnPauseElapsed(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public event EventHandler? BatchReady;

    public void SpeechStarted()
    {
        lock (_stateLock)
        {
            _isSpeaking = true;
            StopTimer();
        }
    }

    public void SpeechEnded()
    {
        lock (_stateLock)
        {
            _isSpeaking = false;
            RestartTimerIfPending();
        }
    }

    public void TextAdded()
    {
        lock (_stateLock)
        {
            _hasPendingText = true;
            RestartTimerIfPending();
        }
    }

    public void Reset()
    {
        lock (_stateLock)
        {
            _isSpeaking = false;
            _hasPendingText = false;
            StopTimer();
        }
    }

    public void Dispose() => _timer.Dispose();

    private void RestartTimerIfPending()
    {
        if (_hasPendingText && !_isSpeaking)
        {
            _timer.Change(_pauseBeforeSending, Timeout.InfiniteTimeSpan);
        }
    }

    private void StopTimer() => _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    private void OnPauseElapsed()
    {
        lock (_stateLock)
        {
            if (!_hasPendingText || _isSpeaking)
            {
                return;
            }

            _hasPendingText = false;
        }

        BatchReady?.Invoke(this, EventArgs.Empty);
    }
}
