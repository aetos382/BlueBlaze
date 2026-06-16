using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Application.Cli;

/// <summary>
/// <c>--access-jwt</c>/環境変数で受け取ったトークンを Authorization ヘッダーに注入する。
/// ログインフロー(createSession)やトークンリフレッシュは扱わない。
/// </summary>
// HttpClientHandler は DelegatingHandler の InnerHandler として渡され、
// DelegatingHandler.Dispose() と一緒に破棄されるため個別の Dispose は不要。
#pragma warning disable CA2000
internal sealed class CliAuthHandler(string? accessJwt) : DelegatingHandler(new HttpClientHandler())
#pragma warning restore CA2000
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(accessJwt))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessJwt);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
