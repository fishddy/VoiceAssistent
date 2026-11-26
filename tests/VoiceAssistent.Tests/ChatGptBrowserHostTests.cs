using Microsoft.Web.WebView2.Core;
using VoiceAssistent.Chat;

namespace VoiceAssistent.Tests;

public class ChatGptBrowserHostTests
{
    private readonly List<FakeBrowserWindow> _createdWindows = [];

    [Fact]
    public void NewHost_DoesNotOpenBrowser()
    {
        var host = CreateHost();

        Assert.False(host.IsOpen);
        Assert.Empty(_createdWindows);
    }

    [Fact]
    public async Task ReleasingLastLease_ClosesBrowser()
    {
        var host = CreateHost();

        var lease = await host.AcquireAsync(CancellationToken.None);
        Assert.True(host.IsOpen);

        lease.Dispose();

        Assert.False(host.IsOpen);
        Assert.True(_createdWindows.Single().IsClosed);
    }

    [Fact]
    public async Task OverlappingLeases_ShareOneBrowserUntilBothAreReleased()
    {
        var host = CreateHost();

        var first = await host.AcquireAsync(CancellationToken.None);
        var second = await host.AcquireAsync(CancellationToken.None);
        first.Dispose();

        Assert.True(host.IsOpen);
        Assert.Single(_createdWindows);

        second.Dispose();
        Assert.False(host.IsOpen);
    }

    [Fact]
    public async Task DisposingLeaseTwice_ReleasesItOnce()
    {
        var host = CreateHost();
        var first = await host.AcquireAsync(CancellationToken.None);
        using var second = await host.AcquireAsync(CancellationToken.None);

        first.Dispose();
        first.Dispose();

        Assert.True(host.IsOpen);
    }

    [Fact]
    public async Task BrowserOpenedAgainAfterClosing_IsANewWindow()
    {
        var host = CreateHost();

        (await host.AcquireAsync(CancellationToken.None)).Dispose();
        (await host.AcquireAsync(CancellationToken.None)).Dispose();

        Assert.Equal(2, _createdWindows.Count);
        Assert.All(_createdWindows, window => Assert.True(window.IsClosed));
    }

    [Fact]
    public async Task MissingWebViewRuntime_IsReportedAndRetriedNextTime()
    {
        var failFirstAttempt = true;
        var host = new ChatGptBrowserHost(() =>
        {
            if (failFirstAttempt)
            {
                failFirstAttempt = false;
                throw new WebView2RuntimeNotFoundException();
            }

            return Task.FromResult<IChatBrowserWindow>(CreateWindow());
        });

        var exception = await Assert.ThrowsAsync<ChatUnavailableException>(() => host.AcquireAsync(CancellationToken.None));
        Assert.Contains("WebView2", exception.Message);
        Assert.False(host.IsOpen);

        using var lease = await host.AcquireAsync(CancellationToken.None);
        Assert.True(host.IsOpen);
    }

    [Fact]
    public async Task Dispose_ClosesOpenBrowser()
    {
        var host = CreateHost();
        _ = await host.AcquireAsync(CancellationToken.None);

        host.Dispose();

        Assert.False(host.IsOpen);
        Assert.True(_createdWindows.Single().IsClosed);
    }

    private ChatGptBrowserHost CreateHost() =>
        new(() => Task.FromResult<IChatBrowserWindow>(CreateWindow()));

    private FakeBrowserWindow CreateWindow()
    {
        var window = new FakeBrowserWindow();
        _createdWindows.Add(window);
        return window;
    }

    private sealed class FakeBrowserWindow : IChatBrowserWindow
    {
        public bool IsClosed { get; private set; }

        public Task NavigateAsync(Uri address, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string> EvaluateAsync(string script) => Task.FromResult("null");

        public void Close() => IsClosed = true;
    }
}
