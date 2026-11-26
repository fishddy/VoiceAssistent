using System.IO;

namespace VoiceAssistent.Speech;

public static class ProgressStreamCopier
{
    private const int BufferSize = 81920;
    private const int FullPercent = 100;

    public static async Task CopyAsync(
        Stream source,
        Stream target,
        long? totalBytes,
        IProgress<int> percentProgress,
        CancellationToken cancellationToken)
    {
        if (totalBytes is not > 0)
        {
            await source.CopyToAsync(target, cancellationToken);
            return;
        }

        var buffer = new byte[BufferSize];
        long copiedBytes = 0;
        var reportedPercent = -1;
        int readBytes;
        while ((readBytes = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, readBytes), cancellationToken);
            copiedBytes += readBytes;

            var percent = CalculatePercent(copiedBytes, totalBytes.Value);
            if (percent > reportedPercent)
            {
                reportedPercent = percent;
                percentProgress.Report(percent);
            }
        }
    }

    private static int CalculatePercent(long copiedBytes, long totalBytes) =>
        (int)Math.Min(FullPercent, copiedBytes * FullPercent / totalBytes);
}
