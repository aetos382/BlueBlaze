using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Client.Core;

public interface IAtProtocolClient
{
    ValueTask<LexiconResponse<TOutput>> SendAsync<TOutput>(
        ILexiconRequest request,
        IResponseDeserializer<TOutput> responseDeserializer,
        CancellationToken cancellationToken = default);
}
