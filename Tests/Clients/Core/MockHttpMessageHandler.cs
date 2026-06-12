using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Client.Core.Tests;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    private readonly Action<HttpRequestMessage>? _onSend;

    internal MockHttpMessageHandler(HttpResponseMessage response, Action<HttpRequestMessage>? onSend = null)
    {
        this._response = response;
        this._onSend = onSend;
    }

    internal HttpRequestMessage? LastRequest { get; private set; }
    internal string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.LastRequest = request;
        if (request.Content is not null)
        {
            this.LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        this._onSend?.Invoke(request);
        cancellationToken.ThrowIfCancellationRequested();
        return this._response;
    }
}
