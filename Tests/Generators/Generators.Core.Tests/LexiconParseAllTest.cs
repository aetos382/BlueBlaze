using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueBlaze.Generators.Core.Tests;

[TestClass]
public sealed class LexiconParseAllTest
{
    [TestMethod]
    public void 全Lexiconファイルをパースしてコード生成_ExtensionDataWarnなし()
    {
        var lexiconsDir = FindLexiconsDirectory();
        var files = Directory.GetFiles(lexiconsDir, "*.json", SearchOption.AllDirectories);

        var parseResults = new List<ParseResult>(files.Length);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            parseResults.Add(LexiconGenerator.Parse(text, file));
        }

        var result = LexiconGenerator.Generate(parseResults, "BlueBlaze.Generated");

        var errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (errors.Count > 0)
        {
            var messages = string.Join("\n", errors.Select(d => d.Message));
            Assert.Fail($"パース/生成エラーが {errors.Count} 件あります:\n{messages}");
        }

        var extensionDataWarnings = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Warning && d.Message.StartsWith("Unknown field", StringComparison.Ordinal))
            .ToList();

        if (extensionDataWarnings.Count > 0)
        {
            var messages = string.Join("\n", extensionDataWarnings.Select(d => d.Message));
            Assert.Fail($"ExtensionData の Warning が {extensionDataWarnings.Count} 件あります:\n{messages}");
        }
    }

    private static string FindLexiconsDirectory()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "submodules", "atproto", "lexicons");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("submodules/atproto/lexicons ディレクトリが見つかりません。");
    }
}
