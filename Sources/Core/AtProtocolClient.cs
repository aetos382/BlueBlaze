using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
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

    public ValueTask<LexiconResponse<TOutput>> QueryAsync<TOutput>(
        IQueryRequest request,
        IHttpResponseDeserializer<TOutput> responseDeserializer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseDeserializer);

        return this.SendHttpAsync(HttpMethod.Get, request.Nsid, request.Parameters, input: null, responseDeserializer, cancellationToken);
    }

    public ValueTask<LexiconResponse<TOutput>> ProcedureAsync<TOutput>(
        IProcedureRequest request,
        IHttpResponseDeserializer<TOutput> responseDeserializer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseDeserializer);

        return this.SendHttpAsync(HttpMethod.Post, request.Nsid, parameters: null, request.Input, responseDeserializer, cancellationToken);
    }

    public async IAsyncEnumerable<TMessage> SubscribeAsync<TMessage>(
        ISubscribeRequest request,
        ISubscriptionMessageDeserializer<TMessage> messageDeserializer,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(messageDeserializer);

        var uri = this.BuildWebSocketUri(request.Nsid, request.Parameters);
        using var ws = new ClientWebSocket();
        try
        {
            await ws.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new LexiconSubscriptionException("WebSocket 接続に失敗しました。", ex);
        }

        var tempBuffer = new byte[4096];
        using var frameBuffer = new MemoryStream();

        while (!cancellationToken.IsCancellationRequested)
        {
            frameBuffer.SetLength(0);
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(tempBuffer), cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    yield break;
                }

#if NET
                await frameBuffer.WriteAsync(tempBuffer.AsMemory(0, result.Count), cancellationToken).ConfigureAwait(false);
#else
                await frameBuffer.WriteAsync(tempBuffer, 0, result.Count, cancellationToken).ConfigureAwait(false);
#endif
            }
            while (!result.EndOfMessage);

            var frame = frameBuffer.ToArray();
            var (op, messageType, payloadStart) = ParseCborHeader(frame);

            if (op == -1)
            {
                throw new LexiconSubscriptionException(ParseCborError(frame, payloadStart));
            }

            TMessage? message = default;
            if (op == 1)
            {
                var payload = new ReadOnlyMemory<byte>(frame, payloadStart, frame.Length - payloadStart);
                message = messageDeserializer.Deserialize(messageType, payload);
            }

            if (message is not null)
            {
                yield return message;
            }
        }
    }

    private async ValueTask<LexiconResponse<TOutput>> SendHttpAsync<TOutput>(
        HttpMethod method,
        string nsid,
        ILexiconParameters? parameters,
        ILexiconInput? input,
        IHttpResponseDeserializer<TOutput> responseDeserializer,
        CancellationToken cancellationToken)
    {
        var queryParameters = parameters?.ToDictionary().ToUriParameterString();

        var uriBuilder = new UriBuilder(this._baseAddress)
        {
            Path = $"/xrpc/{nsid}",
            Query = queryParameters
        };

        var requestUri = uriBuilder.Uri;

        using var requestMessage = new HttpRequestMessage(method, requestUri)
        {
            Content = input?.ToHttpContent()
        };

        using var responseMessage = await this._httpClient
            .SendAsync(requestMessage, cancellationToken)
            .ConfigureAwait(false);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var error = await responseMessage.Content
                .ReadFromJsonAsync(ErrorSerializerContext.Default.LexiconError, cancellationToken)
                .ConfigureAwait(false);

            throw new LexiconHttpException(
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

    private Uri BuildWebSocketUri(string nsid, ILexiconParameters? parameters)
    {
        var scheme = this._baseAddress.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";
        var query = parameters?.ToDictionary().ToUriParameterString();
        var builder = new UriBuilder(this._baseAddress)
        {
            Scheme = scheme,
            Path = $"/xrpc/{nsid}",
            Query = query
        };
        return builder.Uri;
    }

    private static (int op, string? messageType, int payloadStart) ParseCborHeader(byte[] frame)
    {
        int? op = null;
        string? messageType = null;
        var reader = new CborReader(frame);

        var mapLength = reader.ReadStartMap();
        bool isDefinite = mapLength.HasValue;
        int remaining = mapLength ?? 0;

        while (isDefinite ? remaining-- > 0 : reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "op":
                    op = (int)reader.ReadInt64();
                    break;
                case "t":
                    messageType = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        return (
            op ?? throw new InvalidDataException("CBOR ヘッダーに 'op' フィールドがありません。"),
            messageType,
            frame.Length - reader.BytesRemaining);
    }

    private static LexiconError ParseCborError(byte[] frame, int payloadStart)
    {
        var reader = new CborReader(new ReadOnlyMemory<byte>(frame, payloadStart, frame.Length - payloadStart));
        string? errorCode = null;
        string? message = null;

        var mapLength = reader.ReadStartMap();
        bool isDefinite = mapLength.HasValue;
        int remaining = mapLength ?? 0;

        while (isDefinite ? remaining-- > 0 : reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "error":
                    errorCode = reader.ReadTextString();
                    break;
                case "message":
                    message = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();
        return new LexiconError
        {
            Error = errorCode ?? string.Empty,
            Message = message
        };
    }
}
