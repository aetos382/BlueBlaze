using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Core;

public interface IAtProtocolClient
{
    ValueTask<LexiconResponse<TOutput>> QueryAsync<TOutput>(
        IQueryRequest request,
        IHttpResponseDeserializer<TOutput> responseDeserializer,
        CancellationToken cancellationToken = default);

    ValueTask<LexiconResponse<TOutput>> ProcedureAsync<TOutput>(
        IProcedureRequest request,
        IHttpResponseDeserializer<TOutput> responseDeserializer,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<TMessage> SubscribeAsync<TMessage>(
        ISubscribeRequest request,
        ISubscriptionMessageDeserializer<TMessage> messageDeserializer,
        CancellationToken cancellationToken = default);
}
