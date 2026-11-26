namespace VoiceAssistant.Speech;

public sealed class TranscriptBuffer
{
    private readonly List<string> _segments = [];

    public string Text => string.Join(' ', _segments);

    public void Append(string segment)
    {
        var trimmed = segment.Trim();
        if (trimmed.Length > 0)
        {
            _segments.Add(trimmed);
        }
    }

    public void Clear() => _segments.Clear();
}
