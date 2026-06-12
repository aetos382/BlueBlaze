using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BlueBlaze.Client.Core;

namespace BlueBlaze.Client.Core.Tests;

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
        var responseJson = /*lang=json,strict*/ """{"value":42}""";
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeRequest("com.example.getStuff", HttpMethod.Get);
        var response = await client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(42, response.Output.Value);
    }

    [TestMethod]
    public async Task GETリクエスト_NSIDがURLパスに含まれる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeRequest("com.atproto.server.createSession", HttpMethod.Get);
        await client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).ConfigureAwait(false);

        Assert.AreEqual("/xrpc/com.atproto.server.createSession", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [TestMethod]
    public async Task クエリパラメーター_URLのクエリ文字列として付与される()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var parameters = new FakeParameters(new Dictionary<string, string[]>
        {
            ["q"] = ["hello world"],
            ["limit"] = ["50"]
        });
        var request = new FakeRequest("com.example.search", HttpMethod.Get, parameters: parameters);
        await client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).ConfigureAwait(false);

        var query = handler.LastRequest!.RequestUri!.Query;
        StringAssert.Contains(query, "q=hello%20world", StringComparison.Ordinal);
        StringAssert.Contains(query, "limit=50", StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task パラメーターなし_クエリ文字列は空()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeRequest("com.example.getStuff", HttpMethod.Get);
        await client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).ConfigureAwait(false);

        Assert.AreEqual(string.Empty, handler.LastRequest!.RequestUri!.Query);
    }

    [TestMethod]
    public async Task POSTリクエスト_HTTPメソッドがPOSTになる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeRequest("com.example.createSession", HttpMethod.Post);
        await client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).ConfigureAwait(false);

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
    }

    [TestMethod]
    public async Task POSTリクエスト_InputがリクエストボディとしてHTTPコンテンツになる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var input = new FakeInput(/*lang=json,strict*/ """{"handle":"test.bsky.social"}""");
        var request = new FakeRequest("com.example.createSession", HttpMethod.Post, input: input);
        await client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).ConfigureAwait(false);

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

        var request = new FakeRequest("com.example.deleteSession", HttpMethod.Post);
        var response = await client.SendAsync(request, VoidOutputDeserializer.Instance).ConfigureAwait(false);

        Assert.AreSame(VoidOutput.Instance, response.Output);
    }

    [TestMethod]
    public async Task エラーレスポンス_LexiconExceptionをスローする()
    {
        var errorJson = /*lang=json,strict*/ """{"error":"InvalidToken","description":"Token has expired"}""";
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeRequest("com.example.getProfile", HttpMethod.Get);

        var ex = await Assert.ThrowsAsync<LexiconException>(
            () => client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).AsTask()).ConfigureAwait(false);

        Assert.AreEqual(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.AreEqual("InvalidToken", ex.Error.Error);
        Assert.AreEqual("Token has expired", ex.Error.Description);
    }

    [TestMethod]
    public async Task エラーレスポンス_RequestUriがLexiconExceptionに含まれる()
    {
        var errorJson = /*lang=json,strict*/ """{"error":"NotFound"}""";
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        var request = new FakeRequest("com.example.getRepo", HttpMethod.Get);

        var ex = await Assert.ThrowsAsync<LexiconException>(
            () => client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).AsTask()).ConfigureAwait(false);

        StringAssert.Contains(ex.RequestUri.AbsolutePath, "com.example.getRepo", StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task キャンセル済みトークン_OperationCanceledExceptionをスローする()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler);
        var client = new AtProtocolClient(httpClient);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        var request = new FakeRequest("com.example.getStuff", HttpMethod.Get);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => client.SendAsync(request, new JsonDeserializer<SimpleOutput>(), cts.Token).AsTask()).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task BaseAddress_リクエストURLのホストに使われる()
    {
        using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var handler = new MockHttpMessageHandler(responseMessage);
        using var httpClient = CreateHttpClient(handler, new Uri("https://custom.example.com"));
        var client = new AtProtocolClient(httpClient);

        var request = new FakeRequest("com.example.getStuff", HttpMethod.Get);
        await client.SendAsync(request, new JsonDeserializer<SimpleOutput>()).ConfigureAwait(false);

        Assert.AreEqual("custom.example.com", handler.LastRequest!.RequestUri!.Host);
    }
}
