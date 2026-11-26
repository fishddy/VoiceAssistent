namespace VoiceAssistant.Speech;

public static class TranscriptText
{
    public static bool IsNonSpeechAnnotation(string text) =>
        text.Length == 0 || IsEnclosedBy(text, '[', ']') || IsEnclosedBy(text, '(', ')') || IsEnclosedBy(text, '*', '*');

    private static bool IsEnclosedBy(string text, char opening, char closing) =>
        text.Length >= 2 && text[0] == opening && text[^1] == closing;
}
