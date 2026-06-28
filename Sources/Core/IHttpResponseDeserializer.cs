using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Core;

public interface IHttpResponseDeserializer<TOutput>
{
    ValueTask<TOutput> DeserializeAsync(
        HttpContent content,
        CancellationToken cancellationToken = default);
}
