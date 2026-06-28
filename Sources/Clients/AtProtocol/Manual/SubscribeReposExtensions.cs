using System;
using System.Collections.Generic;
using System.Threading;

using BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;
using BlueBlaze.Core;

namespace BlueBlaze.Client.AtProtocol.Manual;

public static class SubscribeReposExtensions
{
    public static IAsyncEnumerable<IMessage> SubscribeReposAsync(
        this IAtProtocolClient client,
        Parameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SubscribeAsync(
            new Request(parameters),
            Deserializer.Instance,
            cancellationToken);
    }
}
