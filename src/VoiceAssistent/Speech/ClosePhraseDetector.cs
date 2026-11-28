namespace VoiceAssistent.Speech;

public sealed class ClosePhraseDetector
{
    private readonly IReadOnlyList<string[]> _closePhrases;

    public ClosePhraseDetector(IEnumerable<string> closePhrases)
    {
        _closePhrases = PhraseWords.SplitAll(closePhrases);
    }

    public bool EndsWithClosePhrase(string utterance)
    {
        var words = PhraseWords.Split(utterance);
        return _closePhrases.Any(phrase =>
            words.Length >= phrase.Length && words.AsSpan(words.Length - phrase.Length).SequenceEqual(phrase));
    }
}
