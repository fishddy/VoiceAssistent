using System.IO;
using VoiceAssistent.Speech;

namespace VoiceAssistent.Tests;

public class ProgressStreamCopierTests
{
    private const int SourceLength = 1_000_000;

    [Fact]
    public async Task CopyAsync_KnownLength_CopiesAllBytesAndReportsUpToHundredPercent()
    {
        var data = CreateData(SourceLength);
        using var target = new MemoryStream();
        var progress = new RecordingProgress();

        await ProgressStreamCopier.CopyAsync(new MemoryStream(data), target, data.Length, progress, CancellationToken.None);

        Assert.Equal(data, target.ToArray());
        Assert.Equal(100, progress.Reports[^1]);
        Assert.Equal(progress.Reports.Order(), progress.Reports);
        Assert.Equal(progress.Reports.Distinct(), progress.Reports);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public async Task CopyAsync_UnknownLength_CopiesAllBytesWithoutReporting(long? totalBytes)
    {
        var data = CreateData(SourceLength);
        using var target = new MemoryStream();
        var progress = new RecordingProgress();

        await ProgressStreamCopier.CopyAsync(new MemoryStream(data), target, totalBytes, progress, CancellationToken.None);

        Assert.Equal(data, target.ToArray());
        Assert.Empty(progress.Reports);
    }

    [Fact]
    public async Task CopyAsync_SourceLongerThanExpected_NeverReportsMoreThanHundredPercent()
    {
        var data = CreateData(SourceLength);
        using var target = new MemoryStream();
        var progress = new RecordingProgress();

        await ProgressStreamCopier.CopyAsync(new MemoryStream(data), target, SourceLength / 2, progress, CancellationToken.None);

        Assert.All(progress.Reports, percent => Assert.InRange(percent, 0, 100));
    }

    private static byte[] CreateData(int length)
    {
        var data = new byte[length];
        new Random(42).NextBytes(data);
        return data;
    }

    private sealed class RecordingProgress : IProgress<int>
    {
        public List<int> Reports { get; } = [];

        public void Report(int value) => Reports.Add(value);
    }
}
