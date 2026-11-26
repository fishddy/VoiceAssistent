namespace VoiceAssistent.Configuration;

public sealed record SpeechSettings
{
    public const string DetectLanguageAutomatically = "auto";

    public string? WakePhraseCulture { get; init; }

    public IReadOnlyList<string> WakePhrases { get; init; } = ["hey claude"];

    public float WakePhraseMinimumConfidence { get; init; } = 0.6f;

    public IReadOnlyList<string> ClosePhrases { get; init; } = ["danke"];

    public string? TranscriptionLanguage { get; init; }

    public TranscriptionModel TranscriptionModel { get; init; } = TranscriptionModel.LargeV3Turbo;

    internal void Validate()
    {
        ValidatePhrases(WakePhrases, nameof(WakePhrases));
        ValidatePhrases(ClosePhrases, nameof(ClosePhrases));
        InvalidSettingsException.ThrowUnless(
            WakePhraseMinimumConfidence is >= 0f and <= 1f,
            "Speech.WakePhraseMinimumConfidence must be between 0 and 1.");
        InvalidSettingsException.ThrowUnless(
            TranscriptionLanguage is null || !string.IsNullOrWhiteSpace(TranscriptionLanguage),
            "Speech.TranscriptionLanguage must be null, \"auto\" or a language code such as \"de\".");
        InvalidSettingsException.ThrowUnless(
            Enum.IsDefined(TranscriptionModel),
            "Speech.TranscriptionModel is not a known model.");
    }

    private static void ValidatePhrases(IReadOnlyList<string>? phrases, string settingName)
    {
        InvalidSettingsException.ThrowUnless(
            phrases is { Count: > 0 },
            $"Speech.{settingName} must contain at least one phrase.");
        InvalidSettingsException.ThrowUnless(
            phrases!.All(phrase => !string.IsNullOrWhiteSpace(phrase)),
            $"Speech.{settingName} must not contain empty phrases.");
    }
}
