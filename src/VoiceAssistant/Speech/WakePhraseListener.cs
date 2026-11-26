using System.Globalization;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using VoiceAssistant.Configuration;

namespace VoiceAssistant.Speech;

public sealed class WakePhraseListener : IDisposable
{
    private readonly SpeechRecognitionEngine _engine;
    private readonly Grammar _wakePhraseGrammar;
    private readonly float _minimumConfidence;
    private bool _isRunning;

    public WakePhraseListener(SpeechSettings settings)
    {
        var recognizer = SelectRecognizer(settings.WakePhraseCulture);
        _engine = new SpeechRecognitionEngine(recognizer);
        _minimumConfidence = settings.WakePhraseMinimumConfidence;

        _wakePhraseGrammar = CreateWakePhraseGrammar(settings.WakePhrases, recognizer.Culture);
        _engine.LoadGrammar(_wakePhraseGrammar);
        _engine.SpeechRecognized += OnSpeechRecognized;
    }

    public event EventHandler? WakePhraseDetected;

    public void Start(Stream pcm16MonoAudio, int sampleRate)
    {
        _engine.SetInputToAudioStream(
            pcm16MonoAudio,
            new SpeechAudioFormatInfo(sampleRate, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
        _engine.RecognizeAsync(RecognizeMode.Multiple);
        _isRunning = true;
    }

    public void Pause() => _wakePhraseGrammar.Enabled = false;

    public void Resume() => _wakePhraseGrammar.Enabled = true;

    public void Dispose()
    {
        _engine.SpeechRecognized -= OnSpeechRecognized;

        if (_isRunning)
        {
            _engine.RecognizeAsyncCancel();
        }

        _engine.Dispose();
    }

    private static RecognizerInfo SelectRecognizer(string? preferredCultureName)
    {
        var recognizers = SpeechRecognitionEngine.InstalledRecognizers();
        var culture = RecognizerCultureSelector.Select(
            recognizers.Select(recognizer => recognizer.Culture).ToList(),
            preferredCultureName,
            CultureInfo.CurrentUICulture);

        return recognizers.First(recognizer => recognizer.Culture.Equals(culture));
    }

    private static Grammar CreateWakePhraseGrammar(IEnumerable<string> wakePhrases, CultureInfo culture)
    {
        var builder = new GrammarBuilder(new Choices(wakePhrases.ToArray())) { Culture = culture };
        return new Grammar(builder) { Name = "WakePhrase" };
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Grammar == _wakePhraseGrammar && e.Result.Confidence >= _minimumConfidence)
        {
            WakePhraseDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}
