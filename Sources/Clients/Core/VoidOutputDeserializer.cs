using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Client.Core;

public sealed class VoidOutputDeserializer :
    IResponseDeserializer<VoidOutput>
{
    private VoidOutputDeserializer()
    {
    }

    public static readonly VoidOutputDeserializer Instance = new();

    public ValueTask<VoidOutput> DeserializeAsync(
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<VoidOutput>(VoidOutput.Instance);
    }
}
