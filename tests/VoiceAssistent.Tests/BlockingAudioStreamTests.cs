using System.IO;
using VoiceAssistent.Audio;

namespace VoiceAssistent.Tests;

public class BlockingAudioStreamTests
{
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void Read_FillsRequestedCountAcrossChunksInOrder()
    {
        using var stream = new BlockingAudioStream();
        stream.Append([1, 2, 3]);
        stream.Append([4, 5]);
        var buffer = new byte[4];

        Assert.Equal(4, stream.Read(buffer, 0, 4));
        Assert.Equal([1, 2, 3, 4], buffer);
        Assert.Equal(4, stream.Position);
    }

    [Fact]
    public void Seek_ZeroFromCurrent_ReportsPositionWithoutMoving()
    {
        using var stream = new BlockingAudioStream();
        stream.Append([1, 2, 3]);
        stream.ReadExactly(new byte[2]);

        Assert.Equal(2, stream.Seek(0, SeekOrigin.Current));
        Assert.Throws<NotSupportedException>(() => stream.Seek(1, SeekOrigin.Begin));
    }

    [Fact]
    public void Length_ReportsUnboundedLiveStream()
    {
        using var stream = new BlockingAudioStream();

        Assert.Equal(-1, stream.Length);
    }

    [Fact]
    public async Task Read_BlocksUntilRequestedCountIsAvailable()
    {
        using var stream = new BlockingAudioStream();
        var buffer = new byte[4];
        var read = Task.Run(() => stream.Read(buffer, 0, buffer.Length));

        stream.Append([9, 8]);
        await Task.Delay(50);
        Assert.False(read.IsCompleted);

        stream.Append([7, 6, 5]);
        Assert.Equal(4, await read.WaitAsync(ReadTimeout));
        Assert.Equal([9, 8, 7, 6], buffer);
    }

    [Fact]
    public async Task Read_AfterComplete_ReturnsRemainingBytesThenEndOfStream()
    {
        using var stream = new BlockingAudioStream();
        stream.Append([1]);
        var buffer = new byte[4];
        var read = Task.Run(() => stream.Read(buffer, 0, 4));

        stream.Complete();
        stream.Append([2]);

        Assert.Equal(1, await read.WaitAsync(ReadTimeout));
        Assert.Equal(0, stream.Read(buffer, 0, 4));
    }
}
