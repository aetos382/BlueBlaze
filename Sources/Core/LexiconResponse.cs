using System.Net;
using System.Net.Http.Headers;

namespace BlueBlaze.Core;

public sealed class LexiconResponse<TOutput>
{
    public HttpStatusCode StatusCode { get; init; }

    public required HttpResponseHeaders Headers { get; init; }

    public required TOutput Output { get; init; }
}
