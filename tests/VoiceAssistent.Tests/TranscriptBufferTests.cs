using VoiceAssistent.Speech;

namespace VoiceAssistent.Tests;

public class TranscriptBufferTests
{
    [Fact]
    public void Append_JoinsTrimmedSegmentsWithSingleSpaces()
    {
        var buffer = new TranscriptBuffer();

        buffer.Append(" open the ");
        buffer.Append("browser");

        Assert.Equal("open the browser", buffer.Text);
    }

    [Fact]
    public void Append_IgnoresBlankSegments()
    {
        var buffer = new TranscriptBuffer();

        buffer.Append("hello");
        buffer.Append("   ");

        Assert.Equal("hello", buffer.Text);
    }

    [Fact]
    public void Clear_RemovesAllSegments()
    {
        var buffer = new TranscriptBuffer();
        buffer.Append("hello");

        buffer.Clear();

        Assert.Equal(string.Empty, buffer.Text);
    }
}
