using System.Windows;
using VoiceAssistent.Configuration;

namespace VoiceAssistent.Overlay;

public partial class DownloadProgressWindow : Window
{
    private const double ScreenMargin = 24;
    private const string StatusPrefix = "Downloading speech model…";

    public DownloadProgressWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => NativeWindowStyles.MakeNonActivatingToolWindow(this);
        SizeChanged += (_, _) => PositionInTopRightCorner();
    }

    public void ShowPercent(int percent)
    {
        DownloadBar.IsIndeterminate = false;
        DownloadBar.Value = percent;
        StatusText.Text = $"{StatusPrefix} {percent} %";
    }

    private void PositionInTopRightCorner()
    {
        var topLeft = OverlayPlacement.CalculateTopLeft(
            SystemParameters.WorkArea,
            new Size(ActualWidth, ActualHeight),
            ScreenCorner.TopRight,
            ScreenMargin);

        Left = topLeft.X;
        Top = topLeft.Y;
    }
}
