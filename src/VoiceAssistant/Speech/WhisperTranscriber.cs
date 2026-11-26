using Whisper.net;
using VoiceAssistant.Configuration;

namespace VoiceAssistant.Speech;

public sealed class WhisperTranscriber : ISpeechTranscriber, IAsyncDisposable
{
    private readonly WhisperFactory _factory;
    private readonly WhisperProcessor _processor;

    private WhisperTranscriber(WhisperFactory factory, WhisperProcessor processor)
    {
        _factory = factory;
        _processor = processor;
    }

    public static IReadOnlyList<string> SupportedLanguages => WhisperFactory.GetSupportedLanguages().ToList();

    public static async Task<WhisperTranscriber> LoadAsync(string modelPath, string language, int sampleRate)
    {
        var transcriber = await Task.Run(() => Create(modelPath, language));
        await transcriber.WarmUpAsync(sampleRate);
        return transcriber;
    }

    public async Task<string> TranscribeAsync(float[] samples, CancellationToken cancellationToken)
    {
        var spokenSegments = new List<string>();
        await foreach (var segment in _processor.ProcessAsync(samples, cancellationToken))
        {
            var text = segment.Text.Trim();
            if (!TranscriptText.IsNonSpeechAnnotation(text))
            {
                spokenSegments.Add(text);
            }
        }

        return string.Join(' ', spokenSegments);
    }

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
        _factory.Dispose();
    }

    private static WhisperTranscriber Create(string modelPath, string language)
    {
        var factory = WhisperFactory.FromPath(modelPath);
        var builder = factory.CreateBuilder();
        builder = language == SpeechSettings.DetectLanguageAutomatically
            ? builder.WithLanguageDetection()
            : builder.WithLanguage(language);

        return new WhisperTranscriber(factory, builder.Build());
    }

    private Task WarmUpAsync(int sampleRate) =>
        Task.Run(() => TranscribeAsync(new float[sampleRate], CancellationToken.None));
}
