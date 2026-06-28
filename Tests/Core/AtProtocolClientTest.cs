using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BlueBlaze.Core;

namespace BlueBlaze.Core.Tests;

[TestClass]
public sealed class AtProtocolClientTest
{
    private static readonly Uri BaseAddress = new("https://bsky.social");

    private static HttpClient CreateHttpClient(MockHttpMessageHandler handler, Uri? baseAddress = null)
    {
        return new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = baseAddress ?? BaseAddress
        };
    }

    [TestMethod]
    public async Task GETリクエスト_200レスポンスをOutputに変換して返す()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("""{"value":42}""")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeQueryRequest("com.example.getStuff");
        var response = await client.QueryAsync(request, new SimpleOutputJsonDeserializer()).ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(42, response.Output.Value);
    }

    [TestMethod]
    public async Task GETリクエスト_NSIDがURLパスに含まれる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("{}")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeQueryRequest("com.atproto.server.createSession");
        await client.QueryAsync(request, new SimpleOutputJsonDeserializer()).ConfigureAwait(false);

        Assert.AreEqual("/xrpc/com.atproto.server.createSession", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [TestMethod]
    public async Task クエリパラメーター_URLのクエリ文字列として付与される()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("{}")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var parameters = new FakeParameters(new Dictionary<string, string[]>
        {
            ["q"] = ["hello world"],
            ["limit"] = ["50"]
        });

        var request = new FakeQueryRequest("com.example.search", parameters: parameters);
        await client.QueryAsync(request, new SimpleOutputJsonDeserializer()).ConfigureAwait(false);

        var query = handler.LastRequest!.RequestUri!.Query;
        StringAssert.Contains(query, "q=hello%20world", StringComparison.Ordinal);
        StringAssert.Contains(query, "limit=50", StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task パラメーターなし_クエリ文字列は空()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("{}")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeQueryRequest("com.example.getStuff");
        await client.QueryAsync(request, new SimpleOutputJsonDeserializer()).ConfigureAwait(false);

        Assert.AreEqual(string.Empty, handler.LastRequest!.RequestUri!.Query);
    }

    [TestMethod]
    public async Task POSTリクエスト_HTTPメソッドがPOSTになる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("{}")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeProcedureRequest("com.example.createSession");
        await client.ProcedureAsync(request, new SimpleOutputJsonDeserializer()).ConfigureAwait(false);

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
    }

    [TestMethod]
    public async Task POSTリクエスト_InputがリクエストボディとしてHTTPコンテンツになる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("{}")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var input = new FakeInput("""{"handle":"test.bsky.social"}""");
        var request = new FakeProcedureRequest("com.example.createSession", input: input);
        await client.ProcedureAsync(request, new SimpleOutputJsonDeserializer()).ConfigureAwait(false);

        Assert.AreEqual(/*lang=json,strict*/ """{"handle":"test.bsky.social"}""", handler.LastRequestBody);
    }

    [TestMethod]
    public async Task Void手続き_VoidOutputInstanceが返る()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8)
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeProcedureRequest("com.example.deleteSession");
        var response = await client.ProcedureAsync(request, VoidOutputDeserializer.Instance).ConfigureAwait(false);

        Assert.AreSame(VoidOutput.Instance, response.Output);
    }

    [TestMethod]
    public async Task エラーレスポンス_LexiconHttpExceptionをスローする()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = HttpContent.CreateJsonStringContent("""{"error":"InvalidToken","message":"Token has expired"}""")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeQueryRequest("com.example.getProfile");

        var ex = await Assert.ThrowsAsync<LexiconHttpException>(
            () => client.QueryAsync(request, new SimpleOutputJsonDeserializer()).AsTask()).ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.IsNotNull(ex.Error);
        Assert.AreEqual("InvalidToken", ex.Error.Error);
        Assert.AreEqual("Token has expired", ex.Error.Message);
    }

    [TestMethod]
    public async Task エラーレスポンス_RequestUriがLexiconHttpExceptionに含まれる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = HttpContent.CreateJsonStringContent("""{"error":"NotFound"}""")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeQueryRequest("com.example.getRepo");

        var ex = await Assert.ThrowsAsync<LexiconHttpException>(
            () => client.QueryAsync(request, new SimpleOutputJsonDeserializer()).AsTask()).ConfigureAwait(false);

        StringAssert.Contains(ex.RequestUri!.AbsolutePath, "com.example.getRepo", StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task キャンセル済みトークン_OperationCanceledExceptionをスローする()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("{}")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        var request = new FakeQueryRequest("com.example.getStuff");

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => client.QueryAsync(request, new SimpleOutputJsonDeserializer(), cts.Token).AsTask()).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task BaseAddress_リクエストURLのホストに使われる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = HttpContent.CreateJsonStringContent("{}")
        };

        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler, new Uri("https://custom.example.com"));
        var client = new AtProtocolClient(httpClient);

        var request = new FakeQueryRequest("com.example.getStuff");
        await client.QueryAsync(request, new SimpleOutputJsonDeserializer()).ConfigureAwait(false);

        Assert.AreEqual("custom.example.com", handler.LastRequest!.RequestUri!.Host);
    }
}
