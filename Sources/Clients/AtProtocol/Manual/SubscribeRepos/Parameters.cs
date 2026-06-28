using System.Collections.Generic;

using BlueBlaze.Core;

namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class Parameters : ILexiconParameters
{
    public Parameters(long? cursor = null)
    {
        this.Cursor = cursor;
    }

    public long? Cursor { get; }

    public IReadOnlyDictionary<string, string[]> ToDictionary()
    {
        var dict = new Dictionary<string, string[]>();
        if (this.Cursor.HasValue)
        {
            dict["cursor"] = [this.Cursor.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)];
        }

        return dict;
    }
}
