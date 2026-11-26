using System.Windows;
using VoiceAssistant.Configuration;

namespace VoiceAssistant.Overlay;

public static class OverlayPlacement
{
    public static Point CalculateTopLeft(Rect workArea, Size windowSize, ScreenCorner corner, double margin)
    {
        var left = IsLeft(corner)
            ? workArea.Left + margin
            : workArea.Right - windowSize.Width - margin;
        var top = IsTop(corner)
            ? workArea.Top + margin
            : workArea.Bottom - windowSize.Height - margin;

        return new Point(left, top);
    }

    private static bool IsLeft(ScreenCorner corner) =>
        corner is ScreenCorner.TopLeft or ScreenCorner.BottomLeft;

    private static bool IsTop(ScreenCorner corner) =>
        corner is ScreenCorner.TopLeft or ScreenCorner.TopRight;
}
