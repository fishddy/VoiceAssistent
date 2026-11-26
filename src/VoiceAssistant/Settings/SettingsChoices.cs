using System.Globalization;
using VoiceAssistant.Audio;
using VoiceAssistant.Configuration;

namespace VoiceAssistant.Settings;

public static class SettingsChoices
{
    public const string WindowsDefaultDeviceLabel = "Windows default device";

    public static IReadOnlyList<Choice<ScreenCorner>> Corners { get; } =
    [
        new("Top right", ScreenCorner.TopRight),
        new("Top left", ScreenCorner.TopLeft),
        new("Bottom right", ScreenCorner.BottomRight),
        new("Bottom left", ScreenCorner.BottomLeft)
    ];

    public static IReadOnlyList<Choice<TranscriptionModel>> Models { get; } =
    [
        new("Large v3 Turbo – most accurate (~550 MB)", TranscriptionModel.LargeV3Turbo),
        new("Medium – accurate, slower (~510 MB)", TranscriptionModel.Medium),
        new("Small – faster, less accurate (~180 MB)", TranscriptionModel.Small),
        new("Base – fastest, least accurate (~60 MB)", TranscriptionModel.Base)
    ];

    public static IReadOnlyList<Choice<string?>> InputDevices(IEnumerable<AudioInputDevice> activeDevices) =>
    [
        new(WindowsDefaultDeviceLabel, null),
        .. activeDevices.Select(device => new Choice<string?>(device.Name, device.Id))
    ];

    public static IReadOnlyList<Choice<string?>> Languages(IEnumerable<string> supportedLanguageCodes)
    {
        var languageNames = CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .GroupBy(culture => culture.TwoLetterISOLanguageName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().NativeName, StringComparer.OrdinalIgnoreCase);

        var languages = supportedLanguageCodes
            .Select(code => new Choice<string?>(DescribeLanguage(code, languageNames), code))
            .OrderBy(choice => choice.Label, StringComparer.CurrentCultureIgnoreCase);

        return
        [
            new($"Windows display language ({CultureInfo.CurrentUICulture.TwoLetterISOLanguageName})", null),
            new("Detect automatically", SpeechSettings.DetectLanguageAutomatically),
            .. languages
        ];
    }

    public static Choice<T> Find<T>(IReadOnlyList<Choice<T>> choices, T value) =>
        choices.FirstOrDefault(choice => EqualityComparer<T>.Default.Equals(choice.Value, value)) ?? choices[0];

    private static string DescribeLanguage(string code, IReadOnlyDictionary<string, string> languageNames) =>
        languageNames.TryGetValue(code, out var name) ? $"{name} ({code})" : code;
}
