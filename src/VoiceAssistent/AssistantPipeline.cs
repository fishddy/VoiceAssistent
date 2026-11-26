using System.Globalization;
using System.Windows.Threading;
using VoiceAssistent.Audio;
using VoiceAssistent.Chat;
using VoiceAssistent.Configuration;
using VoiceAssistent.Overlay;
using VoiceAssistent.Speech;

namespace VoiceAssistent;

public sealed class AssistantPipeline : IAsyncDisposable
{
    private WhisperTranscriber? _transcriber;
    private DictationSession? _dictation;
    private MicrophoneCapture? _capture;
    private BlockingAudioStream? _wakePhraseAudio;
    private WakePhraseListener? _wakePhraseListener;
    private UtteranceBatcher? _batcher;
    private AssistantController? _controller;

    private AssistantPipeline()
    {
    }

    public string InputDeviceName => _capture?.DeviceName ?? string.Empty;

    public static async Task<AssistantPipeline> StartAsync(
        AssistantSettings settings,
        IChatPageProvider chatPages,
        Dispatcher dispatcher)
    {
        var pipeline = new AssistantPipeline();
        try
        {
            await pipeline.BuildAsync(settings, chatPages, dispatcher);
            return pipeline;
        }
        catch
        {
            await pipeline.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _controller?.Dispose();
        _batcher?.Dispose();
        _capture?.Dispose();
        _wakePhraseAudio?.Complete();
        _wakePhraseListener?.Dispose();
        _wakePhraseAudio?.Dispose();

        if (_dictation is not null)
        {
            await _dictation.DisposeAsync();
        }

        if (_transcriber is not null)
        {
            await _transcriber.DisposeAsync();
        }
    }

    private static string ResolveTranscriptionLanguage(SpeechSettings settings) =>
        settings.TranscriptionLanguage ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    private async Task BuildAsync(AssistantSettings settings, IChatPageProvider chatPages, Dispatcher dispatcher)
    {
        var modelPath = await WhisperModelStore.EnsureDownloadedAsync(settings.Speech.TranscriptionModel, CancellationToken.None);
        _transcriber = await WhisperTranscriber.LoadAsync(
            modelPath,
            ResolveTranscriptionLanguage(settings.Speech),
            MicrophoneCapture.SampleRate);
        _dictation = new DictationSession(_transcriber, MicrophoneCapture.SampleRate);

        _wakePhraseListener = new WakePhraseListener(settings.Speech);
        _wakePhraseAudio = new BlockingAudioStream();
        _capture = new MicrophoneCapture(AudioDeviceCatalog.OpenInputDevice(settings.Audio.InputDeviceId));
        _batcher = new UtteranceBatcher(settings.Chat.SendAfterPause, TimeProvider.System);
        _controller = new AssistantController(
            _wakePhraseListener,
            _dictation,
            new OverlayWindow(settings.Overlay),
            new ClosePhraseDetector(settings.Speech.ClosePhrases),
            new ChatGptWebAssistant(chatPages, settings.Chat.Instructions, ChatGptTimings.Default),
            _batcher,
            dispatcher);

        _capture.SamplesAvailable += OnSamplesAvailable;
        _capture.Failed += OnCaptureFailed;
        _wakePhraseListener.Start(_wakePhraseAudio, MicrophoneCapture.SampleRate);
        _capture.Start();
    }

    private void OnSamplesAvailable(object? sender, float[] samples)
    {
        _wakePhraseAudio!.Append(AudioSamples.ToPcm16(samples));
        _dictation!.AcceptSamples(samples);
    }

    private void OnCaptureFailed(object? sender, Exception exception) =>
        _controller!.ShowError($"The microphone stopped working: {exception.Message}");
}
