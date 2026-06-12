using System.Net.Http;

namespace BlueBlaze.Client.Core;

public interface ILexiconInput
{
    HttpContent ToHttpContent();
}
