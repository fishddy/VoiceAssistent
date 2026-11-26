namespace VoiceAssistent.Settings;

public static class PhraseList
{
    private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

    public static IReadOnlyList<string> Parse(string text) =>
        text.Split(LineSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public static string Format(IEnumerable<string> phrases) => string.Join(Environment.NewLine, phrases);
}
