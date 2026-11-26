using VoiceAssistant.Audio;
using VoiceAssistant.Configuration;
using VoiceAssistant.Settings;
using VoiceAssistant.Speech;

namespace VoiceAssistant.Tests;

public class SettingsChoicesTests
{
    [Fact]
    public void InputDevices_StartsWithWindowsDefault()
    {
        var choices = SettingsChoices.InputDevices([new AudioInputDevice("id-1", "USB Microphone")]);

        Assert.Equal(SettingsChoices.WindowsDefaultDeviceLabel, choices[0].Label);
        Assert.Null(choices[0].Value);
        Assert.Equal(new Choice<string?>("USB Microphone", "id-1"), choices[1]);
    }

    [Fact]
    public void Languages_OffersSystemAndAutomaticBeforeNamedLanguages()
    {
        var choices = SettingsChoices.Languages(["en", "de"]);

        Assert.Null(choices[0].Value);
        Assert.Equal(SpeechSettings.DetectLanguageAutomatically, choices[1].Value);
        Assert.Contains(choices, choice => choice.Value == "de" && choice.Label.Contains("Deutsch"));
    }

    [Fact]
    public void Find_UnknownValue_FallsBackToFirstChoice()
    {
        var choices = SettingsChoices.InputDevices([new AudioInputDevice("id-1", "USB Microphone")]);

        Assert.Same(choices[0], SettingsChoices.Find(choices, "unplugged-device"));
    }

    [Fact]
    public void Corners_CoverEveryScreenCorner()
    {
        Assert.Equal(
            Enum.GetValues<ScreenCorner>().Order(),
            SettingsChoices.Corners.Select(choice => choice.Value).Order());
    }

    [Fact]
    public void Models_CoverEveryTranscriptionModelWithDistinctFileNames()
    {
        var models = SettingsChoices.Models.Select(choice => choice.Value).ToList();

        Assert.Equal(Enum.GetValues<TranscriptionModel>().Order(), models.Order());
        Assert.Equal(models.Count, models.Select(WhisperModelStore.GetFileName).Distinct().Count());
    }
}
