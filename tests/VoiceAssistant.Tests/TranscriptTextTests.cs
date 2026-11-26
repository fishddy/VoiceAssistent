using VoiceAssistant.Speech;

namespace VoiceAssistant.Tests;

public class TranscriptTextTests
{
    [Theory]
    [InlineData("")]
    [InlineData("[Musik]")]
    [InlineData("(Applaus)")]
    [InlineData("*lacht*")]
    [InlineData("[BLANK_AUDIO]")]
    public void IsNonSpeechAnnotation_RecognizesAnnotations(string text)
    {
        Assert.True(TranscriptText.IsNonSpeechAnnotation(text));
    }

    [Theory]
    [InlineData("Wie heiße ich?")]
    [InlineData("Was ist 2+2?")]
    [InlineData("(Klammer am Anfang) und Text")]
    public void IsNonSpeechAnnotation_KeepsSpokenText(string text)
    {
        Assert.False(TranscriptText.IsNonSpeechAnnotation(text));
    }
}
