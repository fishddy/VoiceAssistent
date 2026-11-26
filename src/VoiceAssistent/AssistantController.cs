using System.Windows.Threading;
using VoiceAssistent.Chat;
using VoiceAssistent.Overlay;
using VoiceAssistent.Speech;

namespace VoiceAssistent;

public sealed class AssistantController : IDisposable
{
    private readonly WakePhraseListener _wakePhraseListener;
    private readonly DictationSession _dictation;
    private readonly OverlayWindow _overlay;
    private readonly ClosePhraseDetector _closePhraseDetector;
    private readonly IChatAssistant _chat;
    private readonly UtteranceBatcher _batcher;
    private readonly Dispatcher _dispatcher;
    private readonly TranscriptBuffer _pendingText = new();
    private Conversation? _conversation;

    public AssistantController(
        WakePhraseListener wakePhraseListener,
        DictationSession dictation,
        OverlayWindow overlay,
        ClosePhraseDetector closePhraseDetector,
        IChatAssistant chat,
        UtteranceBatcher batcher,
        Dispatcher dispatcher)
    {
        _wakePhraseListener = wakePhraseListener;
        _dictation = dictation;
        _overlay = overlay;
        _closePhraseDetector = closePhraseDetector;
        _chat = chat;
        _batcher = batcher;
        _dispatcher = dispatcher;

        _wakePhraseListener.WakePhraseDetected += OnWakePhraseDetected;
        _dictation.SpeechStarted += OnSpeechStarted;
        _dictation.SpeechEnded += OnSpeechEnded;
        _dictation.PartialTranscriptReceived += OnPartialTranscriptReceived;
        _dictation.FinalTranscriptReceived += OnFinalTranscriptReceived;
        _dictation.TranscriptionFailed += OnTranscriptionFailed;
        _batcher.BatchReady += OnBatchReady;
    }

    public void ShowError(string message) => _dispatcher.InvokeAsync(() =>
    {
        BeginConversation();
        _overlay.ShowError(message);
    });

    public void Dispose()
    {
        _conversation?.Cancel();
        _wakePhraseListener.WakePhraseDetected -= OnWakePhraseDetected;
        _dictation.SpeechStarted -= OnSpeechStarted;
        _dictation.SpeechEnded -= OnSpeechEnded;
        _dictation.PartialTranscriptReceived -= OnPartialTranscriptReceived;
        _dictation.FinalTranscriptReceived -= OnFinalTranscriptReceived;
        _dictation.TranscriptionFailed -= OnTranscriptionFailed;
        _batcher.BatchReady -= OnBatchReady;
        _overlay.Close();
    }

    private void OnWakePhraseDetected(object? sender, EventArgs e) => _dispatcher.InvokeAsync(BeginConversation);

    private void OnSpeechStarted(object? sender, EventArgs e) => _batcher.SpeechStarted();

    private void OnSpeechEnded(object? sender, EventArgs e) => _batcher.SpeechEnded();

    private void OnPartialTranscriptReceived(object? sender, string partialText) => _dispatcher.InvokeAsync(() =>
    {
        if (IsCollectingQuestion)
        {
            _overlay.ShowDraft(_pendingText.Text, partialText);
        }
    });

    private void OnFinalTranscriptReceived(object? sender, string finalText) => _dispatcher.InvokeAsync(() =>
    {
        if (_closePhraseDetector.EndsWithClosePhrase(finalText))
        {
            EndConversation();
            return;
        }

        if (!IsCollectingQuestion)
        {
            return;
        }

        _pendingText.Append(finalText);
        _overlay.ShowDraft(_pendingText.Text, string.Empty);
        _batcher.TextAdded();
    });

    private void OnTranscriptionFailed(object? sender, string message) =>
        _dispatcher.InvokeAsync(() => _overlay.ShowError(message));

    private void OnBatchReady(object? sender, EventArgs e) => _dispatcher.InvokeAsync(SubmitQuestion);

    private bool IsCollectingQuestion => _conversation is { HasQuestion: false };

    private void BeginConversation()
    {
        if (_conversation is not null)
        {
            return;
        }

        _conversation = new Conversation(_chat);
        _wakePhraseListener.Pause();
        _pendingText.Clear();
        _batcher.Reset();
        _overlay.ShowListening();
        _dictation.Start();
    }

    private void EndConversation()
    {
        _conversation?.Cancel();
        _conversation = null;
        _batcher.Reset();
        _pendingText.Clear();
        _dictation.Stop();
        _overlay.Hide();
        _wakePhraseListener.Resume();
    }

    private void SubmitQuestion()
    {
        var question = _pendingText.Text;
        if (!IsCollectingQuestion || question.Length == 0)
        {
            return;
        }

        _pendingText.Clear();
        _batcher.Reset();
        _conversation!.Ask(_overlay.AddExchange(question));
    }

    private sealed class Conversation
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task<ChatStart> _start;
        private Task _answer = Task.CompletedTask;

        public Conversation(IChatAssistant chat)
        {
            _start = StartAsync(chat, _cancellation.Token);
        }

        public bool HasQuestion { get; private set; }

        public void Ask(ConversationExchange exchange)
        {
            HasQuestion = true;
            exchange.WaitForAnswer();
            _answer = AnswerAsync(exchange);
        }

        public void Cancel()
        {
            _cancellation.Cancel();
            _ = ReleaseChatWhenIdleAsync();
        }

        private static async Task<ChatStart> StartAsync(IChatAssistant chat, CancellationToken cancellationToken)
        {
            try
            {
                return new ChatStart(await chat.StartConversationAsync(cancellationToken), Error: null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return ChatStart.Cancelled;
            }
            catch (ChatUnavailableException exception)
            {
                return new ChatStart(Chat: null, exception.Message);
            }
            catch (Exception exception)
            {
                return new ChatStart(Chat: null, $"ChatGPT could not be opened: {exception.Message}");
            }
        }

        private async Task AnswerAsync(ConversationExchange exchange)
        {
            var cancellationToken = _cancellation.Token;
            var start = await _start;
            if (start.Chat is null)
            {
                exchange.FailAnswer(start.Error ?? "ChatGPT is not available.");
                return;
            }

            try
            {
                var answer = await start.Chat.AskAsync(exchange.Question, exchange.UpdateAnswer, cancellationToken);
                exchange.CompleteAnswer(answer);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (ChatUnavailableException exception)
            {
                exchange.FailAnswer(exception.Message);
            }
            catch (Exception exception)
            {
                exchange.FailAnswer($"ChatGPT failed: {exception.Message}");
            }
            finally
            {
                start.Chat.Dispose();
            }
        }

        private async Task ReleaseChatWhenIdleAsync()
        {
            var start = await _start;
            await _answer;
            start.Chat?.Dispose();
        }

        private sealed record ChatStart(IChatConversation? Chat, string? Error)
        {
            public static ChatStart Cancelled { get; } = new(Chat: null, Error: null);
        }
    }
}
