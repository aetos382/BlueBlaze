using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Client.Core;

public interface IResponseDeserializer<TOutput>
{
    ValueTask<TOutput> DeserializeAsync(
        HttpContent content,
        CancellationToken cancellationToken = default);
}
