namespace VoiceAssistant.Chat;

public sealed class ChatGptWebAssistant : IChatAssistant
{
    private static readonly Uri NewConversationAddress = new("https://chatgpt.com/");

    private readonly IChatPageProvider _pages;
    private readonly string _instructions;
    private readonly ChatGptTimings _timings;

    public ChatGptWebAssistant(IChatPageProvider pages, string instructions, ChatGptTimings timings)
    {
        _pages = pages;
        _instructions = instructions;
        _timings = timings;
    }

    public async Task<IChatConversation> StartConversationAsync(CancellationToken cancellationToken)
    {
        var page = await _pages.AcquireAsync(cancellationToken);
        try
        {
            await page.NavigateAsync(NewConversationAddress, cancellationToken);
            return new ChatGptConversation(page, _instructions, _timings);
        }
        catch
        {
            page.Dispose();
            throw;
        }
    }
}
