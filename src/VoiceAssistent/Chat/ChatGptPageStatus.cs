namespace VoiceAssistent.Chat;

public sealed record ChatGptPageStatus(
    bool HasComposer,
    bool CanSend,
    bool IsGenerating,
    int UserMessageCount,
    int AssistantMessageCount,
    string LatestAnswer);
