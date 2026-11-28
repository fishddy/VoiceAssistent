using System.Globalization;

namespace VoiceAssistent.Speech;

public static class PhraseWords
{
    public static string[] Split(string text)
    {
        var lettersAndSpaces = text
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLower(character, CultureInfo.InvariantCulture) : ' ')
            .ToArray();

        return new string(lettersAndSpaces).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    public static IReadOnlyList<string[]> SplitAll(IEnumerable<string> phrases) =>
        phrases
            .Select(Split)
            .Where(words => words.Length > 0)
            .ToList();
}
