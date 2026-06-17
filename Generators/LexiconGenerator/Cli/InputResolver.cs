using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

using Microsoft.Extensions.FileSystemGlobbing;

namespace BlueBlaze.LexiconGenerator.Cli;

internal static class InputResolver
{
    internal static FileInfo[] Resolve(ArgumentResult result)
    {
        var files = new List<FileInfo>();

        foreach (var token in result.Tokens)
        {
            var path = token.Value;

            if (path.StartsWith('@'))
            {
                var listFile = path[1..];
                if (!File.Exists(listFile))
                {
                    result.AddError($"入力リストファイルが見つかりません: {listFile}");
                    continue;
                }
                foreach (var line in File.ReadAllLines(listFile))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        files.Add(new FileInfo(line.Trim()));
                    }
                }
                continue;
            }

            if (ContainsGlobChars(path))
            {
                var (baseDir, pattern) = SplitGlobPattern(path);
                if (!Directory.Exists(baseDir))
                {
                    result.AddError($"ベースディレクトリが見つかりません: {baseDir}");
                    continue;
                }

                var matcher = new Matcher();
                matcher.AddInclude(pattern);
                files.AddRange(matcher.GetResultsInFullPath(baseDir).Select(p => new FileInfo(p)));
            }
            else if (Directory.Exists(path))
            {
                var matcher = new Matcher();
                matcher.AddInclude("**/*.json");
                files.AddRange(matcher.GetResultsInFullPath(Path.GetFullPath(path)).Select(p => new FileInfo(p)));
            }
            else if (File.Exists(path))
            {
                files.Add(new FileInfo(Path.GetFullPath(path)));
            }
            else
            {
                result.AddError($"ファイルまたはディレクトリが見つかりません: {path}");
            }
        }

        return [.. files];
    }

    private static bool ContainsGlobChars(string path)
    {
#pragma warning disable CA1307
        return path.Contains('*');
#pragma warning restore
    }

    private static (string baseDir, string pattern) SplitGlobPattern(string path)
    {
        var parts = path.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        var globIndex = Array.FindIndex(parts, ContainsGlobChars);

        var nonGlobParts = parts.Take(globIndex).ToArray();
        var globParts = parts.Skip(globIndex).ToArray();

        var baseDir = nonGlobParts.Length == 0
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(Path.Combine(nonGlobParts));

        var pattern = Path.Combine(globParts);
        return (baseDir, pattern);
    }
}
