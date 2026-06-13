using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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

    internal FakeInput([StringSyntax(StringSyntaxAttribute.Json)] string json)
    {
        this._json = json;
    }

    public HttpContent ToHttpContent()
    {
        return new StringContent(this._json, Encoding.UTF8, "application/json");
    }
}

internal sealed class SimpleOutputJsonDeserializer : IResponseDeserializer<SimpleOutput>
{
    public async ValueTask<SimpleOutput> DeserializeAsync(HttpContent content, CancellationToken cancellationToken = default)
    {
        var result = await content
            .ReadFromJsonAsync(TestSerializerContext.Default.SimpleOutput, cancellationToken)
            .ConfigureAwait(false);

        return result!;
    }
}

#pragma warning disable CA1812
internal sealed class SimpleOutput
{
    [JsonPropertyName("value")]
    public int Value { get; set; }
}
#pragma warning restore CA1812

internal static class HttpContentExtensions
{
    extension(HttpContent)
    {
        public static HttpContent CreateJsonStringContent(
            [StringSyntax(StringSyntaxAttribute.Json)] string json)
        {
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(SimpleOutput))]
internal sealed partial class TestSerializerContext : JsonSerializerContext;
