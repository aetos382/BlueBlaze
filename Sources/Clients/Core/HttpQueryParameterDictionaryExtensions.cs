using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlaze.Client.Core;

internal static class HttpQueryParameterDictionaryExtensions
{
    public static string? ToUriParameterString(
        this IReadOnlyDictionary<string, string[]> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var kvp in parameters)
        {
            foreach (var value in kvp.Value)
            {
                if (sb.Length > 0)
                {
                    sb.Append('&');
                }

                sb.Append(Uri.EscapeDataString(kvp.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(value));
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}
