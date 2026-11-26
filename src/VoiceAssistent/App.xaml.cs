using System.Windows;
using VoiceAssistent.Chat;
using VoiceAssistent.Configuration;
using VoiceAssistent.Overlay;
using VoiceAssistent.Settings;
using VoiceAssistent.Speech;

namespace VoiceAssistent;

public partial class App : Application
{
    private const string ApplicationName = "Voice Assistent";
    private const int StartupFailureExitCode = 1;

    private readonly SettingsStore _settingsStore = SettingsStore.CreateDefault();
    private readonly SemaphoreSlim _pipelineLock = new(1, 1);
    private AssistantSettings _settings = new();
    private AssistantPipeline? _pipeline;
    private readonly ChatGptBrowserHost _chatGptBrowser = new(async () => await ChatGptBrowserWindow.CreateAsync());
    private TrayIcon? _trayIcon;
    private SettingsWindow? _openSettingsWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _settings = _settingsStore.Load();
        }
        catch (InvalidSettingsException exception)
        {
            MessageBox.Show(exception.Message, ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(StartupFailureExitCode);
            return;
        }

        _trayIcon = new TrayIcon(ApplicationName, OpenSettings, () => Shutdown());
        await RestartPipelineAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pipeline?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _chatGptBrowser.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    private async void OpenSettings()
    {
        if (_openSettingsWindow is not null)
        {
            _openSettingsWindow.Activate();
            return;
        }

        _openSettingsWindow = new SettingsWindow(_settings, WhisperTranscriber.SupportedLanguages);
        var saved = _openSettingsWindow.ShowDialog() == true;
        var savedSettings = _openSettingsWindow.SavedSettings;
        _openSettingsWindow = null;

        if (saved && savedSettings is not null)
        {
            _settingsStore.Save(savedSettings);
            _settings = savedSettings;
            await RestartPipelineAsync();
        }
    }

    private async Task RestartPipelineAsync()
    {
        await _pipelineLock.WaitAsync();
        try
        {
            if (_pipeline is not null)
            {
                await _pipeline.DisposeAsync();
                _pipeline = null;
            }

            _pipeline = await StartPipelineAsync(_settings);
        }
        finally
        {
            _pipelineLock.Release();
        }
    }

    private async Task<AssistantPipeline?> StartPipelineAsync(AssistantSettings settings)
    {
        var trayIcon = _trayIcon!;
        DownloadProgressWindow? downloadProgressWindow = null;
        if (!WhisperModelStore.IsDownloaded(settings.Speech.TranscriptionModel))
        {
            trayIcon.SetToolTip($"{ApplicationName} - downloading speech model…");
            trayIcon.ShowNotification(ApplicationName, "Downloading the speech model. This happens once and can take a few minutes.");
            downloadProgressWindow = new DownloadProgressWindow();
            downloadProgressWindow.Show();
        }
        else
        {
            trayIcon.SetToolTip($"{ApplicationName} - loading…");
        }

        try
        {
            var downloadPercent = new Progress<int>(percent => downloadProgressWindow?.ShowPercent(percent));
            var pipeline = await AssistantPipeline.StartAsync(settings, _chatGptBrowser, Dispatcher, downloadPercent);
            trayIcon.SetToolTip($"{ApplicationName} - {pipeline.InputDeviceName}");
            return pipeline;
        }
        catch (SpeechSetupException exception)
        {
            trayIcon.SetToolTip($"{ApplicationName} - not listening");
            MessageBox.Show(
                $"{exception.Message}\n\nRight-click the tray icon and open Settings to change the configuration.",
                ApplicationName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return null;
        }
        finally
        {
            downloadProgressWindow?.Close();
        }
    }
}
