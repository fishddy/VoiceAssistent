using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using VoiceAssistant.Audio;
using VoiceAssistant.Configuration;
using VoiceAssistant.Speech;

namespace VoiceAssistant.Settings;

public partial class SettingsWindow : Window
{
    private const double LevelDecayPerUpdate = 0.08;
    private const string LevelHint = "Speak to check that the level moves.";

    private readonly AssistantSettings _currentSettings;
    private MicrophoneCapture? _levelMonitor;

    public SettingsWindow(AssistantSettings currentSettings, IReadOnlyList<string> supportedLanguages)
    {
        _currentSettings = currentSettings;
        InitializeComponent();
        PopulateChoices(supportedLanguages);
        Closed += (_, _) => StopLevelMonitor();
    }

    public AssistantSettings? SavedSettings { get; private set; }

    private static void Select<T>(ComboBox box, IReadOnlyList<Choice<T>> choices, T value)
    {
        box.ItemsSource = choices;
        box.SelectedItem = SettingsChoices.Find(choices, value);
    }

    private static T SelectedValue<T>(ComboBox box) => ((Choice<T>)box.SelectedItem).Value;

    private void PopulateChoices(IReadOnlyList<string> supportedLanguages)
    {
        Select(InputDeviceBox, SettingsChoices.InputDevices(AudioDeviceCatalog.GetActiveInputDevices()), _currentSettings.Audio.InputDeviceId);
        Select(CornerBox, SettingsChoices.Corners, _currentSettings.Overlay.Corner);
        Select(LanguageBox, SettingsChoices.Languages(supportedLanguages), _currentSettings.Speech.TranscriptionLanguage);
        Select(ModelBox, SettingsChoices.Models, _currentSettings.Speech.TranscriptionModel);
        MarginBox.Text = _currentSettings.Overlay.Margin.ToString(CultureInfo.CurrentCulture);
        WakePhrasesBox.Text = PhraseList.Format(_currentSettings.Speech.WakePhrases);
        ClosePhrasesBox.Text = PhraseList.Format(_currentSettings.Speech.ClosePhrases);
        ChatInstructionsBox.Text = _currentSettings.Chat.Instructions;
        SendAfterPauseBox.Text = _currentSettings.Chat.SendAfterPauseSeconds.ToString(CultureInfo.CurrentCulture);
    }

    private static bool TryParseNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private void OnInputDeviceChanged(object sender, SelectionChangedEventArgs e)
    {
        StopLevelMonitor();
        InputLevelBar.Value = 0;
        InputLevelHint.Text = LevelHint;

        try
        {
            _levelMonitor = new MicrophoneCapture(AudioDeviceCatalog.OpenInputDevice(SelectedValue<string?>(InputDeviceBox)));
            _levelMonitor.SamplesAvailable += OnLevelSamplesAvailable;
            _levelMonitor.Start();
        }
        catch (Exception exception) when (exception is SpeechSetupException or COMException)
        {
            StopLevelMonitor();
            InputLevelHint.Text = $"This device cannot be opened: {exception.Message}";
        }
    }

    private void OnLevelSamplesAvailable(object? sender, float[] samples)
    {
        var level = AudioSamples.ToDisplayLevel(AudioSamples.RootMeanSquare(samples));
        Dispatcher.InvokeAsync(() => InputLevelBar.Value = Math.Max(level, InputLevelBar.Value - LevelDecayPerUpdate));
    }

    private void StopLevelMonitor()
    {
        if (_levelMonitor is null)
        {
            return;
        }

        _levelMonitor.SamplesAvailable -= OnLevelSamplesAvailable;
        _levelMonitor.Dispose();
        _levelMonitor = null;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (!TryParseNumber(MarginBox.Text, out var margin))
        {
            ShowError("The distance from the screen edge must be a number.");
            return;
        }

        if (!TryParseNumber(SendAfterPauseBox.Text, out var sendAfterPause))
        {
            ShowError("The pause before sending must be a number.");
            return;
        }

        var settings = _currentSettings with
        {
            Audio = _currentSettings.Audio with { InputDeviceId = SelectedValue<string?>(InputDeviceBox) },
            Overlay = _currentSettings.Overlay with { Corner = SelectedValue<ScreenCorner>(CornerBox), Margin = margin },
            Speech = _currentSettings.Speech with
            {
                WakePhrases = PhraseList.Parse(WakePhrasesBox.Text),
                ClosePhrases = PhraseList.Parse(ClosePhrasesBox.Text),
                TranscriptionLanguage = SelectedValue<string?>(LanguageBox),
                TranscriptionModel = SelectedValue<TranscriptionModel>(ModelBox)
            },
            Chat = _currentSettings.Chat with
            {
                Instructions = ChatInstructionsBox.Text.Trim(),
                SendAfterPauseSeconds = sendAfterPause
            }
        };

        try
        {
            settings.Validate();
        }
        catch (InvalidSettingsException exception)
        {
            ShowError(exception.Message);
            return;
        }

        SavedSettings = settings;
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
