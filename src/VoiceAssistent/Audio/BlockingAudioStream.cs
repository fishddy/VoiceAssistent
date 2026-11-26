using System.Collections.Concurrent;
using System.IO;

namespace VoiceAssistent.Audio;

public sealed class BlockingAudioStream : Stream
{
    private const long UnboundedLength = -1;

    private readonly BlockingCollection<byte[]> _chunks = new(new ConcurrentQueue<byte[]>());
    private readonly Lock _completionLock = new();
    private byte[] _currentChunk = [];
    private int _currentChunkOffset;
    private long _position;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => UnboundedLength;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public void Append(byte[] chunk)
    {
        lock (_completionLock)
        {
            if (!_chunks.IsAddingCompleted)
            {
                _chunks.Add(chunk);
            }
        }
    }

    public void Complete()
    {
        lock (_completionLock)
        {
            _chunks.CompleteAdding();
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = 0;
        while (bytesRead < count)
        {
            if (_currentChunkOffset >= _currentChunk.Length && !TryTakeNextChunk())
            {
                break;
            }

            var bytesToCopy = Math.Min(count - bytesRead, _currentChunk.Length - _currentChunkOffset);
            Buffer.BlockCopy(_currentChunk, _currentChunkOffset, buffer, offset + bytesRead, bytesToCopy);
            _currentChunkOffset += bytesToCopy;
            bytesRead += bytesToCopy;
        }

        _position += bytesRead;
        return bytesRead;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        offset == 0 && origin == SeekOrigin.Current ? _position : throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Complete();
            _chunks.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool TryTakeNextChunk()
    {
        if (!_chunks.TryTake(out var nextChunk, Timeout.Infinite))
        {
            return false;
        }

        _currentChunk = nextChunk;
        _currentChunkOffset = 0;
        return true;
    }
}
