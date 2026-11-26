using System.Windows;
using VoiceAssistent.Configuration;
using VoiceAssistent.Overlay;

namespace VoiceAssistent.Tests;

public class OverlayPlacementTests
{
    private static readonly Rect WorkArea = new(0, 0, 1920, 1040);
    private static readonly Size WindowSize = new(400, 200);
    private const double Margin = 20;

    [Theory]
    [InlineData(ScreenCorner.TopLeft, 20, 20)]
    [InlineData(ScreenCorner.TopRight, 1500, 20)]
    [InlineData(ScreenCorner.BottomLeft, 20, 820)]
    [InlineData(ScreenCorner.BottomRight, 1500, 820)]
    public void CalculateTopLeft_PlacesWindowInRequestedCorner(ScreenCorner corner, double expectedLeft, double expectedTop)
    {
        var topLeft = OverlayPlacement.CalculateTopLeft(WorkArea, WindowSize, corner, Margin);

        Assert.Equal(new Point(expectedLeft, expectedTop), topLeft);
    }

    [Fact]
    public void CalculateTopLeft_RespectsWorkAreaOffset()
    {
        var offsetWorkArea = new Rect(100, 50, 1920, 1040);

        var topLeft = OverlayPlacement.CalculateTopLeft(offsetWorkArea, WindowSize, ScreenCorner.TopRight, Margin);

        Assert.Equal(new Point(1600, 70), topLeft);
    }
}
