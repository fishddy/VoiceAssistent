namespace VoiceAssistant.Speech;

public interface ISpeechTranscriber
{
    Task<string> TranscribeAsync(float[] samples, CancellationToken cancellationToken);
}
