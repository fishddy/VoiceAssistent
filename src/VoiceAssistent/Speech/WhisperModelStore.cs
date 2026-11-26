using System.IO;
using System.Net.Http;
using Whisper.net.Ggml;
using VoiceAssistent.Configuration;

namespace VoiceAssistent.Speech;

public static class WhisperModelStore
{
    private const string ApplicationFolderName = "VoiceAssistent";
    private const string ModelFolderName = "Models";
    private const string PartialDownloadExtension = ".part";
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromHours(1);

    private static readonly string ModelDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationFolderName,
        ModelFolderName);

    public static bool IsDownloaded(TranscriptionModel model) => File.Exists(GetModelPath(model));

    public static async Task<string> EnsureDownloadedAsync(
        TranscriptionModel model,
        IProgress<int> downloadPercent,
        CancellationToken cancellationToken)
    {
        var modelPath = GetModelPath(model);
        if (File.Exists(modelPath))
        {
            return modelPath;
        }

        Directory.CreateDirectory(ModelDirectory);
        var partialPath = modelPath + PartialDownloadExtension;
        var (type, quantization) = Describe(model);

        var lengthRecorder = new ResponseLengthRecorder(new HttpClientHandler());
        using var httpClient = new HttpClient(lengthRecorder) { Timeout = DownloadTimeout };
        var downloader = new WhisperGgmlDownloader(httpClient);

        try
        {
            await using (var source = await downloader.GetGgmlModelAsync(type, quantization, cancellationToken))
            await using (var target = File.Create(partialPath))
            {
                await ProgressStreamCopier.CopyAsync(
                    source,
                    target,
                    lengthRecorder.ContentLength,
                    downloadPercent,
                    cancellationToken);
            }

            File.Move(partialPath, modelPath, overwrite: true);
            return modelPath;
        }
        catch (HttpRequestException exception)
        {
            throw new SpeechSetupException($"Downloading the speech model failed: {exception.Message}", exception);
        }
    }

    internal static string GetFileName(TranscriptionModel model)
    {
        var (type, quantization) = Describe(model);
        return $"ggml-{type}-{quantization}.bin".ToLowerInvariant();
    }

    private static string GetModelPath(TranscriptionModel model) => Path.Combine(ModelDirectory, GetFileName(model));

    private static (GgmlType Type, QuantizationType Quantization) Describe(TranscriptionModel model) => model switch
    {
        TranscriptionModel.Base => (GgmlType.Base, QuantizationType.Q5_1),
        TranscriptionModel.Small => (GgmlType.Small, QuantizationType.Q5_1),
        TranscriptionModel.Medium => (GgmlType.Medium, QuantizationType.Q5_0),
        TranscriptionModel.LargeV3Turbo => (GgmlType.LargeV3Turbo, QuantizationType.Q5_0),
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown transcription model.")
    };
}
