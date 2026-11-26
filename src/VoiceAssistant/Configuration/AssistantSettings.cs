namespace VoiceAssistant.Configuration;

public sealed record AssistantSettings
{
    public AudioSettings Audio { get; init; } = new();

    public SpeechSettings Speech { get; init; } = new();

    public ChatSettings Chat { get; init; } = new();

    public OverlaySettings Overlay { get; init; } = new();

    internal void Validate()
    {
        InvalidSettingsException.ThrowUnless(Audio is not null, "The Audio section must not be null.");
        InvalidSettingsException.ThrowUnless(Speech is not null, "The Speech section must not be null.");
        InvalidSettingsException.ThrowUnless(Chat is not null, "The Chat section must not be null.");
        InvalidSettingsException.ThrowUnless(Overlay is not null, "The Overlay section must not be null.");
        Speech!.Validate();
        Chat!.Validate();
        Overlay!.Validate();
    }
}
