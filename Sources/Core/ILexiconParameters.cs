using System.Collections.Generic;

namespace BlueBlaze.Core;

public interface ILexiconParameters
{
    IReadOnlyDictionary<string, string[]> ToDictionary();
}
