namespace VoiceAssistant.Chat;

public interface IChatPageProvider
{
    Task<IChatPageLease> AcquireAsync(CancellationToken cancellationToken);
}
