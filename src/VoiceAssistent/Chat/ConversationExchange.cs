using System.ComponentModel;

namespace VoiceAssistent.Chat;

public sealed class ConversationExchange : INotifyPropertyChanged
{
    public ConversationExchange(string question)
    {
        Question = question;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Question { get; }

    public string Answer { get; private set; } = string.Empty;

    public AnswerState AnswerState { get; private set; } = AnswerState.None;

    public void WaitForAnswer() => Update(AnswerState.Waiting, string.Empty);

    public void UpdateAnswer(string partialAnswer) => Update(AnswerState.Streaming, partialAnswer);

    public void CompleteAnswer(string answer) => Update(AnswerState.Complete, answer);

    public void FailAnswer(string message) => Update(AnswerState.Failed, message);

    private void Update(AnswerState state, string answer)
    {
        var stateChanged = AnswerState != state;
        var answerChanged = Answer != answer;
        AnswerState = state;
        Answer = answer;

        if (stateChanged)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnswerState)));
        }

        if (answerChanged)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Answer)));
        }
    }
}
