using System.IO;
using VoiceAssistant.Configuration;

namespace VoiceAssistant.Tests;

public class SettingsLoaderTests
{
    [Fact]
    public void Parse_EmptyObject_ReturnsDefaults()
    {
        var settings = SettingsLoader.Parse("{}");

        Assert.Equal(ScreenCorner.TopRight, settings.Overlay.Corner);
        Assert.Equal(["hey claude"], settings.Speech.WakePhrases);
        Assert.Null(settings.Speech.WakePhraseCulture);
        Assert.Null(settings.Speech.TranscriptionLanguage);
        Assert.Equal(TranscriptionModel.LargeV3Turbo, settings.Speech.TranscriptionModel);
        Assert.Null(settings.Audio.InputDeviceId);
    }

    [Fact]
    public void Parse_OverridesProvidedValuesAndKeepsOtherDefaults()
    {
        const string json = """
            {
              // comments are allowed
              "overlay": { "corner": "bottomLeft", "width": 300 },
              "speech": { "wakePhrases": ["hello claude"], "wakePhraseCulture": "de-DE", "transcriptionLanguage": "en", "transcriptionModel": "small" },
              "audio": { "inputDeviceId": "device-1" },
            }
            """;

        var settings = SettingsLoader.Parse(json);

        Assert.Equal(ScreenCorner.BottomLeft, settings.Overlay.Corner);
        Assert.Equal(300, settings.Overlay.Width);
        Assert.Equal(new OverlaySettings().Margin, settings.Overlay.Margin);
        Assert.Equal(["hello claude"], settings.Speech.WakePhrases);
        Assert.Equal("de-DE", settings.Speech.WakePhraseCulture);
        Assert.Equal("en", settings.Speech.TranscriptionLanguage);
        Assert.Equal(TranscriptionModel.Small, settings.Speech.TranscriptionModel);
        Assert.Equal("device-1", settings.Audio.InputDeviceId);
    }

    [Fact]
    public void Serialize_ThenParse_RoundTripsSettings()
    {
        var original = new AssistantSettings
        {
            Audio = new AudioSettings { InputDeviceId = "device-2" },
            Overlay = new OverlaySettings { Corner = ScreenCorner.BottomRight, Margin = 12 },
            Speech = new SpeechSettings { TranscriptionLanguage = "auto", TranscriptionModel = TranscriptionModel.Base }
        };

        var json = SettingsLoader.Serialize(original);
        var parsed = SettingsLoader.Parse(json);

        Assert.Contains("\"BottomRight\"", json);
        Assert.Equal(original.Audio, parsed.Audio);
        Assert.Equal(original.Overlay, parsed.Overlay);
        Assert.Equal(original.Speech.TranscriptionLanguage, parsed.Speech.TranscriptionLanguage);
        Assert.Equal(original.Speech.TranscriptionModel, parsed.Speech.TranscriptionModel);
    }

    [Fact]
    public void Parse_SettingsFromEarlierVersion_IgnoresRemovedHideAfterSilence()
    {
        var settings = SettingsLoader.Parse("""{ "Overlay": { "HideAfterSilenceSeconds": 8, "Margin": 30 } }""");

        Assert.Equal(30, settings.Overlay.Margin);
        Assert.Equal(["danke"], settings.Speech.ClosePhrases);
    }

    [Fact]
    public void Parse_ChatSection_DefaultsToShortAnswerInstructions()
    {
        var settings = SettingsLoader.Parse("{}");

        Assert.Contains("short", settings.Chat.Instructions);
        Assert.Equal(TimeSpan.FromSeconds(1), settings.Chat.SendAfterPause);
    }

    [Fact]
    public void Parse_ChatSection_IsReadAndIgnoresRemovedEnabledFlag()
    {
        var settings = SettingsLoader.Parse("""{ "Chat": { "Enabled": false, "Instructions": "", "SendAfterPauseSeconds": 2.5 } }""");

        Assert.Equal(string.Empty, settings.Chat.Instructions);
        Assert.Equal(TimeSpan.FromSeconds(2.5), settings.Chat.SendAfterPause);
    }

    [Fact]
    public void Parse_ClosePhrases_AreRead()
    {
        var settings = SettingsLoader.Parse("""{ "Speech": { "ClosePhrases": ["danke", "das wars"] } }""");

        Assert.Equal(["danke", "das wars"], settings.Speech.ClosePhrases);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "Overlay": { "Corner": "Middle" } }""")]
    [InlineData("""{ "Overlay": { "Corner": 7 } }""")]
    [InlineData("""{ "Overlay": null }""")]
    [InlineData("""{ "Speech": null }""")]
    [InlineData("""{ "Overlay": { "BackgroundOpacity": 1.5 } }""")]
    [InlineData("""{ "Overlay": { "Width": 0 } }""")]
    [InlineData("""{ "Overlay": { "Margin": -1 } }""")]
    [InlineData("""{ "Speech": { "WakePhrases": [] } }""")]
    [InlineData("""{ "Speech": { "ClosePhrases": [] } }""")]
    [InlineData("""{ "Speech": { "ClosePhrases": [""] } }""")]
    [InlineData("""{ "Speech": { "ClosePhrases": null } }""")]
    [InlineData("""{ "Speech": { "WakePhrases": ["  "] } }""")]
    [InlineData("""{ "Speech": { "WakePhraseMinimumConfidence": -0.1 } }""")]
    [InlineData("""{ "Speech": { "TranscriptionLanguage": " " } }""")]
    [InlineData("""{ "Speech": { "TranscriptionModel": "Huge" } }""")]
    [InlineData("""{ "Audio": null }""")]
    [InlineData("""{ "Chat": null }""")]
    [InlineData("""{ "Chat": { "Instructions": null } }""")]
    [InlineData("""{ "Chat": { "SendAfterPauseSeconds": 0 } }""")]
    [InlineData("""{ "Chat": { "SendAfterPauseSeconds": 11 } }""")]
    public void Parse_InvalidSettings_Throws(string json)
    {
        Assert.Throws<InvalidSettingsException>(() => SettingsLoader.Parse(json));
    }

    [Fact]
    public void LoadFromFile_MissingFile_ReturnsDefaults()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        var settings = SettingsLoader.LoadFromFile(missingPath);

        Assert.Equal(ScreenCorner.TopRight, settings.Overlay.Corner);
    }

    [Fact]
    public void LoadFromFile_ReadsBundledSettings()
    {
        var bundledPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        var settings = SettingsLoader.LoadFromFile(bundledPath);

        Assert.Contains("hey claude", settings.Speech.WakePhrases);
    }
}
