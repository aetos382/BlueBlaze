using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueBlaze.LexiconGenerator.Core.Tests;

[TestClass]
public sealed class LexiconParseAllTest
{
    [TestMethod]
    public void 全Lexiconファイルをパースしてコード生成_ExtensionDataWarnなし()
    {
        var lexiconsDir = FindLexiconsDirectory();
        if (lexiconsDir is null)
        {
            Assert.Inconclusive("external/atproto/lexicons が見つかりません。scripts/fetch-atproto-lexicon.mts を実行して lexicon を取得していない環境ではこのテストをスキップします。");
        }

        var files = Directory.GetFiles(lexiconsDir, "*.json", SearchOption.AllDirectories);

        var parseResults = new List<ParseResult>(files.Length);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            parseResults.Add(LexiconCodeGenerator.Parse(text, file));
        }

        var result = LexiconCodeGenerator.Generate(parseResults, "BlueBlaze.Generated");

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

    private static string? FindLexiconsDirectory()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "external", "atproto", "lexicons");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }
        return null;
    }
}
