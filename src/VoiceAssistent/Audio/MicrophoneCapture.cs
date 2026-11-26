using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceAssistent.Audio;

public sealed class MicrophoneCapture : IDisposable
{
    public const int SampleRate = 16000;

    private readonly MMDevice _device;
    private readonly WasapiRecorder _recorder;
    private readonly BufferedWaveProvider _capturedAudio;
    private readonly ISampleProvider _convertedAudio;
    private readonly float[] _readBuffer = new float[SampleRate];

    public MicrophoneCapture(MMDevice device)
    {
        _device = device;
        _recorder = new WasapiRecorderBuilder().WithDevice(device).WithSharedMode().Build();
        _capturedAudio = new BufferedWaveProvider(_recorder.WaveFormat)
        {
            ReadFully = false,
            DiscardOnBufferOverflow = true
        };
        _convertedAudio = new WdlResamplingSampleProvider(
            new MonoDownmixSampleProvider(_capturedAudio.ToSampleProvider()),
            SampleRate);

        _recorder.DataAvailable += OnDataAvailable;
        _recorder.RecordingStopped += OnRecordingStopped;
    }

    public event EventHandler<float[]>? SamplesAvailable;

    public event EventHandler<Exception>? Failed;

    public string DeviceName => _device.FriendlyName;

    public void Start() => _recorder.StartRecording();

    public void Dispose()
    {
        _recorder.DataAvailable -= OnDataAvailable;
        _recorder.RecordingStopped -= OnRecordingStopped;
        _recorder.StopRecording();
        _recorder.Dispose();
        _device.Dispose();
    }

    private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        _capturedAudio.AddSamples(buffer.ToArray(), 0, buffer.Length);

        int samplesRead;
        while ((samplesRead = _convertedAudio.Read(_readBuffer.AsSpan())) > 0)
        {
            SamplesAvailable?.Invoke(this, _readBuffer[..samplesRead]);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            Failed?.Invoke(this, e.Exception);
        }
    }
}
