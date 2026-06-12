using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Client.Core;

public class AtProtocolClient : IAtProtocolClient
{
    private readonly HttpClient _httpClient;

    public AtProtocolClient(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    public async ValueTask<TResponse> SendQueryAsync<TResponse>(
        string nsid,
        IReadOnlyDictionary<string, string?>? queryParameters,
        CancellationToken cancellationToken)
    {
        var uri = new System.Uri(BuildUriString(nsid, queryParameters), System.UriKind.Relative);
        var response = await this._httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return result!;
    }

    public async ValueTask<TResponse> SendProcedureAsync<TRequest, TResponse>(
        string nsid,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var response = await this._httpClient.PostAsJsonAsync($"xrpc/{nsid}", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return result!;
    }

    public async ValueTask SendProcedureAsync<TRequest>(
        string nsid,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var response = await this._httpClient.PostAsJsonAsync($"xrpc/{nsid}", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static string BuildUriString(string nsid, IReadOnlyDictionary<string, string?>? queryParameters)
    {
        if (queryParameters == null || queryParameters.Count == 0)
        {
            return $"xrpc/{nsid}";
        }

        var sb = new StringBuilder();
        sb.Append("xrpc/");
        sb.Append(nsid);
        sb.Append('?');

        var first = true;
        foreach (var kv in queryParameters)
        {
            if (kv.Value == null)
            {
                continue;
            }

            if (!first)
            {
                sb.Append('&');
            }

            sb.Append(System.Uri.EscapeDataString(kv.Key));
            sb.Append('=');
            sb.Append(System.Uri.EscapeDataString(kv.Value));
            first = false;
        }

        return sb.ToString();
    }
}
