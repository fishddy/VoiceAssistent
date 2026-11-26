using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using VoiceAssistent.Chat;
using VoiceAssistent.Configuration;

namespace VoiceAssistent.Overlay;

public partial class OverlayWindow : Window
{
    private const string ListeningStatus = "Listening…";
    private const string ErrorStatus = "Something went wrong";

    private readonly OverlaySettings _settings;
    private readonly ObservableCollection<ConversationExchange> _exchanges = [];

    public OverlayWindow(OverlaySettings settings)
    {
        _settings = settings;
        InitializeComponent();
        ApplyAppearance();
        ExchangeList.ItemsSource = _exchanges;

        SourceInitialized += (_, _) => NativeWindowStyles.MakeNonActivatingToolWindow(this);
        SizeChanged += (_, _) => PositionOnPrimaryScreen();
        ConversationPanel.SizeChanged += (_, _) => ConversationScroller.ScrollToEnd();
    }

    public void ShowListening()
    {
        StatusText.Text = ListeningStatus;
        _exchanges.Clear();
        ShowDraft(string.Empty, string.Empty);

        if (!IsVisible)
        {
            Show();
        }

        PositionOnPrimaryScreen();
    }

    public void ShowDraft(string finalText, string partialText)
    {
        StatusText.Text = ListeningStatus;
        FinalRun.Text = finalText;
        PartialRun.Text = finalText.Length > 0 && partialText.Length > 0 ? " " + partialText : partialText;
        DraftText.Visibility = finalText.Length > 0 || partialText.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public ConversationExchange AddExchange(string question)
    {
        var exchange = new ConversationExchange(question);
        _exchanges.Add(exchange);
        ShowDraft(string.Empty, string.Empty);
        return exchange;
    }

    public void ShowError(string message)
    {
        StatusText.Text = ErrorStatus;
        FinalRun.Text = message;
        PartialRun.Text = string.Empty;
        DraftText.Visibility = Visibility.Visible;
    }

    private void ApplyAppearance()
    {
        Width = _settings.Width;
        ConversationScroller.MaxHeight = _settings.MaxHeight;
        ConversationScroller.FontSize = _settings.FontSize;
        Container.CornerRadius = new CornerRadius(_settings.CornerRadius);
        Container.Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x1E))
        {
            Opacity = _settings.BackgroundOpacity
        };
    }

    private void PositionOnPrimaryScreen()
    {
        var topLeft = OverlayPlacement.CalculateTopLeft(
            SystemParameters.WorkArea,
            new Size(ActualWidth, ActualHeight),
            _settings.Corner,
            _settings.Margin);

        Left = topLeft.X;
        Top = topLeft.Y;
    }
}
