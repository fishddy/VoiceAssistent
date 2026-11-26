using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using VoiceAssistent.Overlay;

namespace VoiceAssistent.Chat;

public partial class ChatGptBrowserWindow : Window, IChatBrowserWindow
{
    private const string ApplicationFolderName = "VoiceAssistent";
    private const string BrowserProfileFolderName = "ChatGpt";
    private const string BridgeScriptResourceName = "VoiceAssistent.Chat.ChatGptBridge.js";
    private const string KeepPageActiveArguments =
        "--disable-background-timer-throttling --disable-renderer-backgrounding "
        + "--disable-backgrounding-occluded-windows --disable-features=CalculateNativeWinOcclusion";
    private const double OffScreenGap = 100;

    private ChatGptBrowserWindow()
    {
        InitializeComponent();
        Closed += (_, _) => Browser.Dispose();
    }

    private static string ProfileFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationFolderName,
        BrowserProfileFolderName);

    public static async Task<ChatGptBrowserWindow> CreateAsync()
    {
        var window = new ChatGptBrowserWindow();
        window.MoveOffScreen();
        window.Show();
        NativeWindowStyles.MakeToolWindow(window);

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: ProfileFolder,
            options: new CoreWebView2EnvironmentOptions(KeepPageActiveArguments));
        await window.Browser.EnsureCoreWebView2Async(environment);
        await window.Browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(LoadBridgeScript());

        return window;
    }

    public async Task NavigateAsync(Uri address, CancellationToken cancellationToken)
    {
        var navigation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e) =>
            navigation.TrySetResult(e.IsSuccess);

        Browser.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
        try
        {
            Browser.CoreWebView2.Navigate(address.AbsoluteUri);
            await using var cancellation = cancellationToken.Register(() => navigation.TrySetCanceled(cancellationToken));
            if (!await navigation.Task)
            {
                throw new ChatUnavailableException("ChatGPT could not be loaded. Check the internet connection.");
            }
        }
        finally
        {
            Browser.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
        }
    }

    public Task<string> EvaluateAsync(string script) => Browser.CoreWebView2.ExecuteScriptAsync(script);

    private static string LoadBridgeScript()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(BridgeScriptResourceName)
            ?? throw new InvalidOperationException($"The embedded resource {BridgeScriptResourceName} is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void MoveOffScreen()
    {
        Left = SystemParameters.VirtualScreenLeft - Width - OffScreenGap;
        Top = SystemParameters.VirtualScreenTop;
    }
}
