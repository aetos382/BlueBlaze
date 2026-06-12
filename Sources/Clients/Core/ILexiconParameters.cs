using System.Collections.Generic;

namespace BlueBlaze.Client.Core;

public interface ILexiconParameters
{
    IReadOnlyDictionary<string, string[]> ToDictionary();
}
