using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BlueBlaze.Client.Core;

namespace BlueBlaze.Client.Core.Tests;

internal sealed class FakeRequest : ILexiconRequest
{
    internal FakeRequest(
        string nsid,
        HttpMethod method,
        ILexiconParameters? parameters = null,
        ILexiconInput? input = null)
    {
        this.Nsid = nsid;
        this.Method = method;
        this.Parameters = parameters;
        this.Input = input;
    }

    public string Nsid { get; }
    public HttpMethod Method { get; }
    public ILexiconParameters? Parameters { get; }
    public ILexiconInput? Input { get; }
}

internal sealed class FakeParameters : ILexiconParameters
{
    private readonly IReadOnlyDictionary<string, string[]> _dict;

    internal FakeParameters(IReadOnlyDictionary<string, string[]> dict)
    {
        this._dict = dict;
    }

    public IReadOnlyDictionary<string, string[]> ToDictionary()
    {
        return this._dict;
    }
}

internal sealed class FakeInput : ILexiconInput
{
    private readonly string _json;

    internal FakeInput(string json)
    {
        this._json = json;
    }

    public HttpContent ToHttpContent()
    {
        return new StringContent(this._json, Encoding.UTF8, "application/json");
    }
}

internal sealed class JsonDeserializer<T> : IResponseDeserializer<T>
{
    public async ValueTask<T> DeserializeAsync(HttpContent content, CancellationToken cancellationToken = default)
    {
        var result = await content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
        return result!;
    }
}

#pragma warning disable CA1812
internal sealed class SimpleOutput
{
    public int Value { get; set; }
}
#pragma warning restore CA1812
