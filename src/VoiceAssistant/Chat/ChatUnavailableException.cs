namespace VoiceAssistant.Chat;

public sealed class ChatUnavailableException : Exception
{
    public ChatUnavailableException(string message)
        : base(message)
    {
    }
}
