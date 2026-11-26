using System.Net.Http;

namespace VoiceAssistent.Speech;

public sealed class ResponseLengthRecorder(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    public long? ContentLength { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        ContentLength = response.Content.Headers.ContentLength;
        return response;
    }
}
