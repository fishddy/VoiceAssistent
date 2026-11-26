using System.Net;
using System.Net.Http;
using VoiceAssistent.Speech;

namespace VoiceAssistent.Tests;

public class ResponseLengthRecorderTests
{
    [Fact]
    public async Task SendAsync_ResponseWithContent_RecordsContentLength()
    {
        var recorder = new ResponseLengthRecorder(new FixedResponseHandler(new ByteArrayContent(new byte[1234])));
        using var client = new HttpClient(recorder);

        await client.GetAsync("https://example.test/model.bin");

        Assert.Equal(1234, recorder.ContentLength);
    }

    [Fact]
    public void ContentLength_BeforeAnyRequest_IsNull()
    {
        using var recorder = new ResponseLengthRecorder(new FixedResponseHandler(new ByteArrayContent([])));

        Assert.Null(recorder.ContentLength);
    }

    private sealed class FixedResponseHandler(HttpContent content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }
}
