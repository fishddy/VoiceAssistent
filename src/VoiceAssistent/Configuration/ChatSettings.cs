namespace VoiceAssistent.Configuration;

public sealed record ChatSettings
{
    public const string DefaultInstructions =
        "Keep your answers short: at most three short sentences, without headings or lists. "
        + "Answer in the language of my message. My messages come from speech recognition "
        + "and may contain small recognition errors.";

    private const double MaximumSendAfterPauseSeconds = 10;

    public string Instructions { get; init; } = DefaultInstructions;

    public double SendAfterPauseSeconds { get; init; } = 1;

    public TimeSpan SendAfterPause => TimeSpan.FromSeconds(SendAfterPauseSeconds);

    internal void Validate()
    {
        InvalidSettingsException.ThrowUnless(Instructions is not null, "Chat.Instructions must not be null.");
        InvalidSettingsException.ThrowUnless(
            SendAfterPauseSeconds is > 0 and <= MaximumSendAfterPauseSeconds,
            $"Chat.SendAfterPauseSeconds must be greater than 0 and at most {MaximumSendAfterPauseSeconds}.");
    }
}
