namespace VoiceAssistent.Speech;

public sealed class SpeechSetupException : Exception
{
    public SpeechSetupException(string message)
        : base(message)
    {
    }

    public SpeechSetupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
