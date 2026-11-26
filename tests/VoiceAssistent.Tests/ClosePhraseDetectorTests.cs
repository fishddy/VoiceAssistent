using VoiceAssistent.Speech;

namespace VoiceAssistent.Tests;

public class ClosePhraseDetectorTests
{
    [Theory]
    [InlineData("Danke.")]
    [InlineData("danke")]
    [InlineData("DANKE!")]
    [InlineData("Okay, danke.")]
    [InlineData("Wie spät ist es? Danke")]
    public void EndsWithClosePhrase_UtteranceEndingWithPhrase_ReturnsTrue(string utterance)
    {
        var detector = new ClosePhraseDetector(["danke"]);

        Assert.True(detector.EndsWithClosePhrase(utterance));
    }

    [Theory]
    [InlineData("Danke für die Hilfe, und jetzt noch eine Frage")]
    [InlineData("Undankbar")]
    [InlineData("Dankeschön")]
    [InlineData("")]
    public void EndsWithClosePhrase_PhraseNotAtEndOrNotAWholeWord_ReturnsFalse(string utterance)
    {
        var detector = new ClosePhraseDetector(["danke"]);

        Assert.False(detector.EndsWithClosePhrase(utterance));
    }

    [Fact]
    public void EndsWithClosePhrase_MultiWordPhrase_IgnoresCaseAndPunctuation()
    {
        var detector = new ClosePhraseDetector(["Das war's"]);

        Assert.True(detector.EndsWithClosePhrase("Okay. Das war s!"));
        Assert.True(detector.EndsWithClosePhrase("das war's"));
        Assert.False(detector.EndsWithClosePhrase("war's"));
    }

    [Fact]
    public void EndsWithClosePhrase_AnyOfSeveralPhrases_ReturnsTrue()
    {
        var detector = new ClosePhraseDetector(["danke", "tschüss"]);

        Assert.True(detector.EndsWithClosePhrase("Tschüss!"));
        Assert.False(detector.EndsWithClosePhrase("Hallo"));
    }
}
