namespace VoiceAssistent.Chat;

public interface IChatAssistant
{
    Task<IChatConversation> StartConversationAsync(CancellationToken cancellationToken);
}
