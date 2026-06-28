using BlueBlaze.Core;

namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

[Lexicon("com.atproto.sync.subscribeRepos", LexiconOperationKind.Subscription)]
public sealed class Request : ISubscribeRequest
{
    public Request(Parameters? parameters = null)
    {
        this.Parameters = parameters;
    }

    public string Nsid => "com.atproto.sync.subscribeRepos";
    public ILexiconParameters? Parameters { get; }
}
