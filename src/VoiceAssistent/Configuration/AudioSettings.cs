namespace VoiceAssistent.Configuration;

public sealed record AudioSettings
{
    public string? InputDeviceId { get; init; }
}
