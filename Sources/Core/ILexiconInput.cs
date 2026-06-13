using System.Net.Http;

namespace BlueBlaze.Core;

public interface ILexiconInput
{
    HttpContent ToHttpContent();
}
