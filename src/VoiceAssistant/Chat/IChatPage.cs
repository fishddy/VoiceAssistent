namespace VoiceAssistant.Chat;

public interface IChatPage
{
    Task NavigateAsync(Uri address, CancellationToken cancellationToken);

    Task<string> EvaluateAsync(string script);
}
