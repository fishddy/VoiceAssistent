using System.IO;
using VoiceAssistant.Configuration;

namespace VoiceAssistant.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"VoiceAssistantTests-{Guid.NewGuid()}");
    private readonly string _defaultsPath;
    private readonly string _userPath;

    public SettingsStoreTests()
    {
        Directory.CreateDirectory(_directory);
        _defaultsPath = Path.Combine(_directory, "defaults.json");
        _userPath = Path.Combine(_directory, "user", "appsettings.json");
        File.WriteAllText(_defaultsPath, """{ "Overlay": { "Corner": "TopLeft" } }""");
    }

    [Fact]
    public void Load_WithoutUserSettings_UsesDefaultsFile()
    {
        var store = new SettingsStore(_defaultsPath, _userPath);

        Assert.Equal(ScreenCorner.TopLeft, store.Load().Overlay.Corner);
    }

    [Fact]
    public void Save_ThenLoad_ReturnsSavedSettings()
    {
        var store = new SettingsStore(_defaultsPath, _userPath);
        var settings = store.Load() with
        {
            Overlay = new OverlaySettings { Corner = ScreenCorner.BottomLeft, Margin = 40 }
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.True(File.Exists(_userPath));
        Assert.Equal(ScreenCorner.BottomLeft, loaded.Overlay.Corner);
        Assert.Equal(40, loaded.Overlay.Margin);
    }

    [Fact]
    public void Save_InvalidSettings_ThrowsAndWritesNothing()
    {
        var store = new SettingsStore(_defaultsPath, _userPath);
        var invalid = new AssistantSettings { Overlay = new OverlaySettings { Margin = -5 } };

        Assert.Throws<InvalidSettingsException>(() => store.Save(invalid));
        Assert.False(File.Exists(_userPath));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
