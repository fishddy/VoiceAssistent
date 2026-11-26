using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using VoiceAssistent.Speech;

namespace VoiceAssistent.Audio;

public static class AudioDeviceCatalog
{
    public static IReadOnlyList<AudioInputDevice> GetActiveInputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(device => new AudioInputDevice(device.ID, device.FriendlyName))
            .OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static MMDevice OpenInputDevice(string? deviceId)
    {
        using var enumerator = new MMDeviceEnumerator();

        if (deviceId is not null && IsActiveInputDevice(enumerator, deviceId))
        {
            return enumerator.GetDevice(deviceId);
        }

        try
        {
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }
        catch (COMException exception)
        {
            throw new SpeechSetupException("No recording device is available.", exception);
        }
    }

    private static bool IsActiveInputDevice(MMDeviceEnumerator enumerator, string deviceId) =>
        enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Any(device => device.ID == deviceId);
}
