using System.Globalization;

namespace VoiceAssistent.Speech;

public sealed class ClosePhraseDetector
{
    private readonly IReadOnlyList<string[]> _closePhrases;

    public ClosePhraseDetector(IEnumerable<string> closePhrases)
    {
        _closePhrases = closePhrases
            .Select(SplitIntoWords)
            .Where(words => words.Length > 0)
            .ToList();
    }

    public bool EndsWithClosePhrase(string utterance)
    {
        var words = SplitIntoWords(utterance);
        return _closePhrases.Any(phrase =>
            words.Length >= phrase.Length && words.AsSpan(words.Length - phrase.Length).SequenceEqual(phrase));
    }

    private static string[] SplitIntoWords(string text)
    {
        var lettersAndSpaces = text
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLower(character, CultureInfo.InvariantCulture) : ' ')
            .ToArray();

        return new string(lettersAndSpaces).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
