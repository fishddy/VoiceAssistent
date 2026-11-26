using VoiceAssistent.Settings;

namespace VoiceAssistent.Tests;

public class PhraseListTests
{
    [Fact]
    public void Parse_SplitsLinesTrimsAndDropsBlankLines()
    {
        var phrases = PhraseList.Parse("  hey claude \r\n\r\nhey klod\n   \rhallo");

        Assert.Equal(["hey claude", "hey klod", "hallo"], phrases);
    }

    [Fact]
    public void Parse_EmptyText_ReturnsNoPhrases()
    {
        Assert.Empty(PhraseList.Parse("  \n "));
    }

    [Fact]
    public void Format_ThenParse_RoundTrips()
    {
        string[] phrases = ["danke", "das war's"];

        Assert.Equal(phrases, PhraseList.Parse(PhraseList.Format(phrases)));
    }
}
