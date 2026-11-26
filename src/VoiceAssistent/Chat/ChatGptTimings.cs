namespace VoiceAssistent.Chat;

public sealed record ChatGptTimings(
    TimeSpan PollInterval,
    TimeSpan PageReadyTimeout,
    TimeSpan SendReadyTimeout,
    TimeSpan SendConfirmationTimeout,
    int SendAttempts,
    TimeSpan AnswerStartTimeout,
    TimeSpan AnswerTimeout,
    int StablePollsBeforeComplete)
{
    public static ChatGptTimings Default { get; } = new(
        PollInterval: TimeSpan.FromMilliseconds(250),
        PageReadyTimeout: TimeSpan.FromSeconds(20),
        SendReadyTimeout: TimeSpan.FromSeconds(10),
        SendConfirmationTimeout: TimeSpan.FromSeconds(2),
        SendAttempts: 3,
        AnswerStartTimeout: TimeSpan.FromSeconds(30),
        AnswerTimeout: TimeSpan.FromMinutes(3),
        StablePollsBeforeComplete: 3);
}
