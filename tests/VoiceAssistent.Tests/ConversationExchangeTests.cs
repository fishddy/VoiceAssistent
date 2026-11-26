using VoiceAssistent.Chat;

namespace VoiceAssistent.Tests;

public class ConversationExchangeTests
{
    [Fact]
    public void NewExchange_HasNoAnswerArea()
    {
        var exchange = new ConversationExchange("Wie heiße ich?");

        Assert.Equal("Wie heiße ich?", exchange.Question);
        Assert.Equal(AnswerState.None, exchange.AnswerState);
        Assert.Equal(string.Empty, exchange.Answer);
    }

    [Fact]
    public void AnswerLifecycle_MovesThroughStatesAndNotifies()
    {
        var exchange = new ConversationExchange("Frage");
        var changes = new List<string?>();
        exchange.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        exchange.WaitForAnswer();
        Assert.Equal(AnswerState.Waiting, exchange.AnswerState);

        exchange.UpdateAnswer("Teil");
        Assert.Equal(AnswerState.Streaming, exchange.AnswerState);
        Assert.Equal("Teil", exchange.Answer);

        exchange.CompleteAnswer("Teil der Antwort.");
        Assert.Equal(AnswerState.Complete, exchange.AnswerState);
        Assert.Equal("Teil der Antwort.", exchange.Answer);

        Assert.Equal(
            [nameof(ConversationExchange.AnswerState), nameof(ConversationExchange.AnswerState), nameof(ConversationExchange.Answer),
             nameof(ConversationExchange.AnswerState), nameof(ConversationExchange.Answer)],
            changes);
    }

    [Fact]
    public void FailAnswer_ShowsMessageInFailedState()
    {
        var exchange = new ConversationExchange("Frage");
        exchange.WaitForAnswer();

        exchange.FailAnswer("ChatGPT is not ready.");

        Assert.Equal(AnswerState.Failed, exchange.AnswerState);
        Assert.Equal("ChatGPT is not ready.", exchange.Answer);
    }
}
