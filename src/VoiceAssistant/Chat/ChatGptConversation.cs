using System.Diagnostics;
using System.Text.Json;

namespace VoiceAssistant.Chat;

public sealed class ChatGptConversation : IChatConversation
{
    private const string StatusScript =
        "window.voiceAssistantChatGpt ? window.voiceAssistantChatGpt.status() : null";
    private const string SendScript =
        "window.voiceAssistantChatGpt ? window.voiceAssistantChatGpt.send() : false";
    private const string RetryHint = "Please try again.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatPageLease _page;
    private readonly string _instructions;
    private readonly ChatGptTimings _timings;
    private bool _hasSentInstructions;

    public ChatGptConversation(IChatPageLease page, string instructions, ChatGptTimings timings)
    {
        _page = page;
        _instructions = instructions.Trim();
        _timings = timings;
    }

    public async Task<string> AskAsync(string message, Action<string> onAnswerUpdated, CancellationToken cancellationToken)
    {
        var readyStatus = await WaitForStatusAsync(status => status.HasComposer, _timings.PageReadyTimeout, cancellationToken)
            ?? throw Unavailable("ChatGPT is not ready.");

        await TypeMessageAsync(ComposePrompt(message), cancellationToken);
        await SendAsync(readyStatus.UserMessageCount, cancellationToken);
        _hasSentInstructions = true;

        _ = await WaitForStatusAsync(
                status => status.AssistantMessageCount > readyStatus.AssistantMessageCount,
                _timings.AnswerStartTimeout,
                cancellationToken)
            ?? throw Unavailable("ChatGPT did not answer.");

        return await FollowAnswerAsync(onAnswerUpdated, cancellationToken);
    }

    public void Dispose() => _page.Dispose();

    private string ComposePrompt(string message) =>
        _hasSentInstructions || _instructions.Length == 0 ? message : $"{_instructions}\n\n{message}";

    private async Task TypeMessageAsync(string prompt, CancellationToken cancellationToken)
    {
        var setMessageScript =
            $"window.voiceAssistantChatGpt ? window.voiceAssistantChatGpt.setMessage({JsonSerializer.Serialize(prompt)}) : false";
        if (!await EvaluateAsync<bool>(setMessageScript))
        {
            throw Unavailable("The ChatGPT message field was not found.");
        }

        _ = await WaitForStatusAsync(status => status.CanSend, _timings.SendReadyTimeout, cancellationToken)
            ?? throw Unavailable("ChatGPT did not accept the message.");
    }

    private async Task SendAsync(int userMessagesBeforeSending, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < _timings.SendAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clicked = await EvaluateAsync<bool>(SendScript);
            if (!clicked && attempt == 0)
            {
                throw Unavailable("The ChatGPT send button was not found.");
            }

            var confirmation = await WaitForStatusAsync(
                status => status.UserMessageCount > userMessagesBeforeSending,
                _timings.SendConfirmationTimeout,
                cancellationToken);
            if (confirmation is not null)
            {
                return;
            }
        }

        throw Unavailable("ChatGPT did not accept the message.");
    }

    private async Task<string> FollowAnswerAsync(Action<string> onAnswerUpdated, CancellationToken cancellationToken)
    {
        var deadline = Stopwatch.StartNew();
        var latestAnswer = string.Empty;
        var unchangedPolls = 0;

        while (deadline.Elapsed < _timings.AnswerTimeout)
        {
            var status = await ReadStatusAsync();
            if (status is not null && status.LatestAnswer != latestAnswer)
            {
                latestAnswer = status.LatestAnswer;
                unchangedPolls = 0;
                onAnswerUpdated(latestAnswer);
            }
            else if (status is { IsGenerating: false } && latestAnswer.Length > 0
                && ++unchangedPolls >= _timings.StablePollsBeforeComplete)
            {
                return latestAnswer;
            }

            await Task.Delay(_timings.PollInterval, cancellationToken);
        }

        return latestAnswer.Length > 0 ? latestAnswer : throw Unavailable("ChatGPT did not finish answering.");
    }

    private async Task<ChatGptPageStatus?> WaitForStatusAsync(
        Func<ChatGptPageStatus, bool> condition,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            var status = await ReadStatusAsync();
            if (status is not null && condition(status))
            {
                return status;
            }

            if (deadline.Elapsed >= timeout)
            {
                return null;
            }

            await Task.Delay(_timings.PollInterval, cancellationToken);
        }
    }

    private Task<ChatGptPageStatus?> ReadStatusAsync() => EvaluateAsync<ChatGptPageStatus?>(StatusScript);

    private async Task<T?> EvaluateAsync<T>(string script) =>
        JsonSerializer.Deserialize<T>(await _page.EvaluateAsync(script), JsonOptions);

    private static ChatUnavailableException Unavailable(string reason) => new($"{reason} {RetryHint}");
}
