namespace BlueBlaze.Core;

public interface IQueryRequest
{
    string Nsid { get; }

    ILexiconParameters? Parameters { get; }
}
