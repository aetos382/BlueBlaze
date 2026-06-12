using System.Net.Http;

namespace BlueBlaze.Client.Core;

public interface ILexiconRequest
{
    string Nsid { get; }

    HttpMethod Method { get; }

    ILexiconParameters? Parameters { get; }

    ILexiconInput? Input { get; }
}
