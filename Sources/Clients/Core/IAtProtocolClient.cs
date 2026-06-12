using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Client.Core;

public interface IAtProtocolClient
{
    ValueTask<TResponse> SendQueryAsync<TResponse>(
        string nsid,
        IReadOnlyDictionary<string, string?>? queryParameters,
        CancellationToken cancellationToken);

    ValueTask<TResponse> SendProcedureAsync<TRequest, TResponse>(
        string nsid,
        TRequest request,
        CancellationToken cancellationToken);

    ValueTask SendProcedureAsync<TRequest>(
        string nsid,
        TRequest request,
        CancellationToken cancellationToken);
}
