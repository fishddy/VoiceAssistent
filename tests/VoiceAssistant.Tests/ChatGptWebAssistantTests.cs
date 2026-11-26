using System.Text.Json;
using VoiceAssistant.Chat;

namespace VoiceAssistant.Tests;

public class ChatGptWebAssistantTests
{
    private const string Instructions = "Answer briefly.";

    private static readonly ChatGptTimings FastTimings = new(
        PollInterval: TimeSpan.FromMilliseconds(1),
        PageReadyTimeout: TimeSpan.FromMilliseconds(200),
        SendReadyTimeout: TimeSpan.FromMilliseconds(200),
        SendConfirmationTimeout: TimeSpan.FromMilliseconds(50),
        SendAttempts: 3,
        AnswerStartTimeout: TimeSpan.FromMilliseconds(200),
        AnswerTimeout: TimeSpan.FromSeconds(2),
        StablePollsBeforeComplete: 2);

    [Fact]
    public async Task AskAsync_FirstMessage_IncludesInstructionsAndFollowUpDoesNot()
    {
        var page = new FakeChatPage(answerFrames: ["Vier."]);
        using var conversation = await StartConversationAsync(page, Instructions);

        await conversation.AskAsync("Was ist 2+2?", _ => { }, CancellationToken.None);
        await conversation.AskAsync("Und 3+3?", _ => { }, CancellationToken.None);

        Assert.Equal($"{Instructions}\n\nWas ist 2+2?", page.SentMessages[0]);
        Assert.Equal("Und 3+3?", page.SentMessages[1]);
    }

    [Fact]
    public async Task StartConversationAsync_EachConversationNavigatesAndSendsInstructions()
    {
        var page = new FakeChatPage(answerFrames: ["Ok."]);
        var assistant = new ChatGptWebAssistant(new FakePageProvider(page), Instructions, FastTimings);

        using (var first = await assistant.StartConversationAsync(CancellationToken.None))
        {
            await first.AskAsync("Erste", _ => { }, CancellationToken.None);
        }

        using (var second = await assistant.StartConversationAsync(CancellationToken.None))
        {
            await second.AskAsync("Zweite", _ => { }, CancellationToken.None);
        }

        Assert.Equal(2, page.NavigationCount);
        Assert.StartsWith(Instructions, page.SentMessages[1]);
    }

    [Fact]
    public async Task DisposingConversation_ReleasesBrowserPage()
    {
        var page = new FakeChatPage(answerFrames: ["Ok."]);
        var conversation = await StartConversationAsync(page, Instructions);

        conversation.Dispose();

        Assert.Equal(1, page.DisposeCount);
    }

    [Fact]
    public async Task StartConversationAsync_NavigationFails_ReleasesBrowserPage()
    {
        var page = new FakeChatPage(answerFrames: []) { NavigationFailure = new ChatUnavailableException("offline") };
        var assistant = new ChatGptWebAssistant(new FakePageProvider(page), Instructions, FastTimings);

        await Assert.ThrowsAsync<ChatUnavailableException>(() => assistant.StartConversationAsync(CancellationToken.None));

        Assert.Equal(1, page.DisposeCount);
    }

    [Fact]
    public async Task AskAsync_StreamsGrowingAnswerAndReturnsFinalText()
    {
        string[] frames = ["Es", "Es ist", "Es ist zwölf", "Es ist zwölf Uhr."];
        var page = new FakeChatPage(answerFrames: frames);
        using var conversation = await StartConversationAsync(page, string.Empty);
        var updates = new List<string>();

        var answer = await conversation.AskAsync("Wie spät ist es?", updates.Add, CancellationToken.None);

        Assert.Equal("Es ist zwölf Uhr.", answer);
        Assert.True(updates.Count >= 2);
        Assert.Equal(frames[^updates.Count..], updates);
        Assert.Equal("Wie spät ist es?", page.SentMessages.Single());
    }

    [Fact]
    public async Task AskAsync_PageWithoutComposer_Throws()
    {
        var page = new FakeChatPage(answerFrames: ["unused"]) { HasComposer = false };
        using var conversation = await StartConversationAsync(page, Instructions);

        var exception = await Assert.ThrowsAsync<ChatUnavailableException>(
            () => conversation.AskAsync("Hallo", _ => { }, CancellationToken.None));

        Assert.Contains("not ready", exception.Message);
        Assert.Empty(page.SentMessages);
    }

    [Fact]
    public async Task AskAsync_NoAnswerAppears_Throws()
    {
        var page = new FakeChatPage(answerFrames: []) { AnswersAfterSend = false };
        using var conversation = await StartConversationAsync(page, Instructions);

        var exception = await Assert.ThrowsAsync<ChatUnavailableException>(
            () => conversation.AskAsync("Hallo", _ => { }, CancellationToken.None));

        Assert.Contains("did not answer", exception.Message);
    }

    [Fact]
    public async Task AskAsync_IgnoredSendClick_IsRetried()
    {
        var page = new FakeChatPage(answerFrames: ["Zwölf."]) { IgnoredSendClicks = 1 };
        using var conversation = await StartConversationAsync(page, string.Empty);

        var answer = await conversation.AskAsync("Und mal drei?", _ => { }, CancellationToken.None);

        Assert.Equal("Zwölf.", answer);
        Assert.Equal(2, page.SendClicks);
        Assert.Equal("Und mal drei?", page.SentMessages.Single());
    }

    [Fact]
    public async Task AskAsync_EverySendClickIgnored_Throws()
    {
        var page = new FakeChatPage(answerFrames: ["unused"]) { IgnoredSendClicks = int.MaxValue };
        using var conversation = await StartConversationAsync(page, string.Empty);

        var exception = await Assert.ThrowsAsync<ChatUnavailableException>(
            () => conversation.AskAsync("Hallo", _ => { }, CancellationToken.None));

        Assert.Contains("did not accept", exception.Message);
        Assert.Equal(FastTimings.SendAttempts, page.SendClicks);
    }

    [Fact]
    public async Task AskAsync_Cancelled_ThrowsOperationCanceled()
    {
        var page = new FakeChatPage(answerFrames: []) { AnswersAfterSend = false };
        var assistant = new ChatGptWebAssistant(
            new FakePageProvider(page),
            Instructions,
            FastTimings with { AnswerStartTimeout = TimeSpan.FromSeconds(10) });
        using var conversation = await assistant.StartConversationAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => conversation.AskAsync("Hallo", _ => { }, cancellation.Token));
    }

    private static Task<IChatConversation> StartConversationAsync(FakeChatPage page, string instructions) =>
        new ChatGptWebAssistant(new FakePageProvider(page), instructions, FastTimings)
            .StartConversationAsync(CancellationToken.None);

    private sealed class FakePageProvider(FakeChatPage page) : IChatPageProvider
    {
        public Task<IChatPageLease> AcquireAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IChatPageLease>(page);
    }

    private sealed class FakeChatPage(IReadOnlyList<string> answerFrames) : IChatPageLease
    {
        private const string SetMessageCall = "voiceAssistantChatGpt.setMessage(";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private string? _typedMessage;
        private int _assistantMessageCount;
        private int _nextFrame;
        private string _latestAnswer = string.Empty;

        public bool HasComposer { get; init; } = true;

        public bool AnswersAfterSend { get; init; } = true;

        public int IgnoredSendClicks { get; init; }

        public Exception? NavigationFailure { get; init; }

        public int SendClicks { get; private set; }

        public List<string> SentMessages { get; } = [];

        public int NavigationCount { get; private set; }

        public int DisposeCount { get; private set; }

        public Task NavigateAsync(Uri address, CancellationToken cancellationToken)
        {
            NavigationCount++;
            return NavigationFailure is null ? Task.CompletedTask : Task.FromException(NavigationFailure);
        }

        public Task<string> EvaluateAsync(string script)
        {
            if (script.Contains(".status()"))
            {
                return Task.FromResult(JsonSerializer.Serialize(NextStatus(), JsonOptions));
            }

            if (script.Contains(SetMessageCall))
            {
                return Task.FromResult(Json(TypeMessage(script)));
            }

            if (script.Contains(".send()"))
            {
                return Task.FromResult(Json(Send()));
            }

            throw new InvalidOperationException($"Unexpected script: {script}");
        }

        public void Dispose() => DisposeCount++;

        private static string Json(bool value) => value ? "true" : "false";

        private bool TypeMessage(string script)
        {
            if (!HasComposer)
            {
                return false;
            }

            var argumentStart = script.IndexOf(SetMessageCall, StringComparison.Ordinal) + SetMessageCall.Length;
            var argumentEnd = script.LastIndexOf(") :", StringComparison.Ordinal);
            _typedMessage = JsonSerializer.Deserialize<string>(script[argumentStart..argumentEnd]);
            return true;
        }

        private bool Send()
        {
            if (_typedMessage is null)
            {
                return false;
            }

            if (SendClicks++ < IgnoredSendClicks)
            {
                return true;
            }

            SentMessages.Add(_typedMessage);
            _typedMessage = null;
            if (AnswersAfterSend)
            {
                _assistantMessageCount++;
                _nextFrame = 0;
                _latestAnswer = string.Empty;
            }

            return true;
        }

        private ChatGptPageStatus NextStatus()
        {
            var isGenerating = AnswersAfterSend && _nextFrame < answerFrames.Count && _assistantMessageCount > 0;
            if (isGenerating)
            {
                _latestAnswer = answerFrames[_nextFrame++];
            }

            return new ChatGptPageStatus(
                HasComposer,
                CanSend: _typedMessage is not null,
                IsGenerating: isGenerating,
                UserMessageCount: SentMessages.Count,
                _assistantMessageCount,
                _latestAnswer);
        }
    }
}
