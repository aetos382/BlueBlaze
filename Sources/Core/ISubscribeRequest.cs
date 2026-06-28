namespace BlueBlaze.Core;

public interface ISubscribeRequest
{
    string Nsid { get; }

    ILexiconParameters? Parameters { get; }
}
