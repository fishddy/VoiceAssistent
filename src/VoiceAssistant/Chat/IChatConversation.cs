namespace VoiceAssistant.Chat;

public interface IChatConversation : IDisposable
{
    Task<string> AskAsync(string message, Action<string> onAnswerUpdated, CancellationToken cancellationToken);
}
