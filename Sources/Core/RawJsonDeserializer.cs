using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Core;

/// <summary>
/// レスポンスボディを型にデシリアライズせず、JSON DOM(<see cref="JsonDocument"/>)としてそのまま受け取る。
/// CLI のようにレスポンスをそのまま表示するだけの用途で、デシリアライズ→再シリアライズの
/// 無駄な往復(および NativeAOT 非互換なリフレクションベースのシリアライズ)を避けるために使う。
/// <see cref="JsonDocument.ParseAsync(System.IO.Stream, JsonDocumentOptions, CancellationToken)"/> は
/// <see cref="JsonDocument"/> 専用の静的メソッドで、汎用の <c>JsonSerializer</c> 経由ではないため
/// リフレクション不要(NativeAOT 安全)。汎用拡張メソッドの
/// <c>HttpContentJsonExtensions.ReadFromJsonAsync&lt;T&gt;</c> はメソッド自体に
/// <c>RequiresUnreferencedCode</c>/<c>RequiresDynamicCode</c> が付いており、
/// <c>T=JsonDocument</c> でも警告が出るため使わない。
/// </summary>
public sealed class RawJsonDeserializer :
    IResponseDeserializer<JsonDocument>
{
    private RawJsonDeserializer()
    {
    }

    public static readonly RawJsonDeserializer Instance = new();

    public async ValueTask<JsonDocument> DeserializeAsync(
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        // procedure で出力が無い(VoidOutput 相当)場合、レスポンスボディが空のことがある。
        // 空ボディを ParseAsync に渡すと JsonException になるため先に判定する。
        if (content.Headers.ContentLength is 0)
        {
            return JsonDocument.Parse("null");
        }

#if NET
        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
        // netstandard2.0 には ReadAsStreamAsync(CancellationToken) オーバーロードが無い。
        var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
