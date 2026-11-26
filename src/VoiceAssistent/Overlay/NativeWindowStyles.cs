using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VoiceAssistent.Overlay;

internal static class NativeWindowStyles
{
    private const int ExtendedStyleIndex = -20;
    private const nint ToolWindowStyle = 0x00000080;
    private const nint NoActivateStyle = 0x08000000;

    public static void MakeNonActivatingToolWindow(Window window) =>
        UpdateExtendedStyle(window, style => style | ToolWindowStyle | NoActivateStyle);

    public static void MakeToolWindow(Window window) =>
        UpdateExtendedStyle(window, style => style | ToolWindowStyle);

    private static void UpdateExtendedStyle(Window window, Func<nint, nint> update)
    {
        var handle = new WindowInteropHelper(window).Handle;
        SetWindowLongPtr(handle, ExtendedStyleIndex, update(GetWindowLongPtr(handle, ExtendedStyleIndex)));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);
}
