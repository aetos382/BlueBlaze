using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Core;

public class AtProtocolClient :
    IAtProtocolClient
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseAddress;

    public AtProtocolClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (httpClient.BaseAddress is not { } baseAddress)
        {
            throw new ArgumentException("BaseAddress must be set.", nameof(httpClient));
        }

        this._httpClient = httpClient;
        this._baseAddress = baseAddress;
    }

    public async ValueTask<LexiconResponse<TOutput>> SendAsync<TOutput>(
        ILexiconRequest request,
        IResponseDeserializer<TOutput> responseDeserializer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseDeserializer);

        var queryParameters = request.Parameters?.ToDictionary().ToUriParameterString();

        var uriBuilder = new UriBuilder(this._baseAddress)
        {
            Path = $"/xrpc/{request.Nsid}",
            Query = queryParameters
        };

        var requestUri = uriBuilder.Uri;

        using var requestMessage = new HttpRequestMessage(request.Method, requestUri)
        {
            Content = request.Input?.ToHttpContent()
        };

        using var responseMessage = await this._httpClient
            .SendAsync(requestMessage, cancellationToken)
            .ConfigureAwait(false);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var error = await responseMessage.Content
                .ReadFromJsonAsync(ErrorSerializerContext.Default.LexiconError, cancellationToken)
                .ConfigureAwait(false);

            throw new LexiconException(
                requestUri,
                responseMessage.StatusCode,
                responseMessage.Headers,
                error);
        }

        var output = await responseDeserializer
            .DeserializeAsync(responseMessage.Content, cancellationToken)
            .ConfigureAwait(false);

        return new LexiconResponse<TOutput>
        {
            StatusCode = responseMessage.StatusCode,
            Headers = responseMessage.Headers,
            Output = output
        };
    }
}
