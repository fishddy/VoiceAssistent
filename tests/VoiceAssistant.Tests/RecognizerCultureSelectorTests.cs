using System.Globalization;
using VoiceAssistant.Speech;

namespace VoiceAssistant.Tests;

public class RecognizerCultureSelectorTests
{
    private static readonly CultureInfo English = new("en-US");
    private static readonly CultureInfo BritishEnglish = new("en-GB");
    private static readonly CultureInfo German = new("de-DE");
    private static readonly CultureInfo French = new("fr-FR");

    [Fact]
    public void Select_PreferredCultureInstalled_ReturnsItIgnoringCase()
    {
        var selected = RecognizerCultureSelector.Select([German, English], "EN-us", German);

        Assert.Equal(English, selected);
    }

    [Fact]
    public void Select_PreferredCultureMissing_Throws()
    {
        var exception = Assert.Throws<SpeechSetupException>(
            () => RecognizerCultureSelector.Select([German], "en-US", German));

        Assert.Contains("de-DE", exception.Message);
    }

    [Fact]
    public void Select_NoPreference_ReturnsSystemCulture()
    {
        var selected = RecognizerCultureSelector.Select([English, German], null, German);

        Assert.Equal(German, selected);
    }

    [Fact]
    public void Select_NoExactSystemMatch_ReturnsSameLanguage()
    {
        var selected = RecognizerCultureSelector.Select([German, BritishEnglish], " ", English);

        Assert.Equal(BritishEnglish, selected);
    }

    [Fact]
    public void Select_NoLanguageMatch_ReturnsFirstInstalled()
    {
        var selected = RecognizerCultureSelector.Select([German, English], null, French);

        Assert.Equal(German, selected);
    }

    [Fact]
    public void Select_NothingInstalled_Throws()
    {
        Assert.Throws<SpeechSetupException>(() => RecognizerCultureSelector.Select([], null, English));
    }
}
