using System.IO;

namespace VoiceAssistant.Configuration;

public sealed class SettingsStore
{
    private const string SettingsFileName = "appsettings.json";
    private const string ApplicationFolderName = "VoiceAssistant";

    private readonly string _defaultSettingsPath;
    private readonly string _userSettingsPath;

    public SettingsStore(string defaultSettingsPath, string userSettingsPath)
    {
        _defaultSettingsPath = defaultSettingsPath;
        _userSettingsPath = userSettingsPath;
    }

    public static SettingsStore CreateDefault() => new(
        Path.Combine(AppContext.BaseDirectory, SettingsFileName),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ApplicationFolderName,
            SettingsFileName));

    public AssistantSettings Load() =>
        SettingsLoader.LoadFromFile(File.Exists(_userSettingsPath) ? _userSettingsPath : _defaultSettingsPath);

    public void Save(AssistantSettings settings)
    {
        var json = SettingsLoader.Serialize(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(_userSettingsPath)!);
        File.WriteAllText(_userSettingsPath, json);
    }
}
