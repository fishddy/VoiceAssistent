namespace VoiceAssistant.Chat;

public interface IChatAssistant
{
    Task<IChatConversation> StartConversationAsync(CancellationToken cancellationToken);
}
