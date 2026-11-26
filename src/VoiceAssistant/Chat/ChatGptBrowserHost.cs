using Microsoft.Web.WebView2.Core;

namespace VoiceAssistant.Chat;

public sealed class ChatGptBrowserHost : IChatPageProvider, IDisposable
{
    private const string MissingRuntimeMessage =
        "ChatGPT answers need the Microsoft Edge WebView2 Runtime, which is not installed.";

    private readonly Func<Task<IChatBrowserWindow>> _createWindow;
    private Task<IChatBrowserWindow>? _window;
    private int _activeLeases;

    public ChatGptBrowserHost(Func<Task<IChatBrowserWindow>> createWindow)
    {
        _createWindow = createWindow;
    }

    public bool IsOpen => _window is not null;

    public async Task<IChatPageLease> AcquireAsync(CancellationToken cancellationToken)
    {
        _activeLeases++;
        try
        {
            var window = await GetOrOpenWindowAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return new Lease(this, window);
        }
        catch
        {
            ReleaseLease();
            throw;
        }
    }

    public void Dispose()
    {
        _activeLeases = 0;
        CloseWindow();
    }

    private static async Task CloseWhenOpenedAsync(Task<IChatBrowserWindow> pendingWindow)
    {
        await ((Task)pendingWindow).ConfigureAwait(
            ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        if (pendingWindow.IsCompletedSuccessfully)
        {
            pendingWindow.Result.Close();
        }
    }

    private async Task<IChatBrowserWindow> GetOrOpenWindowAsync()
    {
        _window ??= OpenWindowAsync();
        var pendingWindow = _window;
        try
        {
            return await pendingWindow;
        }
        catch
        {
            if (_window == pendingWindow)
            {
                _window = null;
            }

            throw;
        }
    }

    private async Task<IChatBrowserWindow> OpenWindowAsync()
    {
        try
        {
            return await _createWindow();
        }
        catch (WebView2RuntimeNotFoundException)
        {
            throw new ChatUnavailableException(MissingRuntimeMessage);
        }
    }

    private void ReleaseLease()
    {
        _activeLeases--;
        if (_activeLeases == 0)
        {
            CloseWindow();
        }
    }

    private void CloseWindow()
    {
        var pendingWindow = _window;
        _window = null;
        if (pendingWindow is not null)
        {
            _ = CloseWhenOpenedAsync(pendingWindow);
        }
    }

    private sealed class Lease(ChatGptBrowserHost host, IChatBrowserWindow window) : IChatPageLease
    {
        private bool _isReleased;

        public Task NavigateAsync(Uri address, CancellationToken cancellationToken) =>
            window.NavigateAsync(address, cancellationToken);

        public Task<string> EvaluateAsync(string script) => window.EvaluateAsync(script);

        public void Dispose()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            host.ReleaseLease();
        }
    }
}
