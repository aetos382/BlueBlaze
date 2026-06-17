using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlueBlaze.Application.Cli;

/// <summary>
/// <c>--input-json</c> オプションの値を解決する。<c>@file</c> でファイル読み込み、
/// <c>-</c> で標準入力読み込み、それ以外はそのまま JSON 文字列として扱う。
/// </summary>
internal static class CliInputReader
{
    internal static async Task<string> ReadInputJsonAsync(string value, CancellationToken cancellationToken)
    {
        if (value == "-")
        {
            return await Console.In.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        if (value.StartsWith('@'))
        {
            var path = value[1..];
            return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        }

        return value;
    }
}
