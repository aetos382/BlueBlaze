using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BlueBlaze.LexiconGenerator.Core;

namespace BlueBlaze.LexiconGenerator.Cli;

internal static class GenerateHandler
{
    internal static async Task<int> RunAsync(
        FileInfo[] inputs,
        DirectoryInfo outputDir,
        string generatedCodeNamespace,
        GeneratorOptions options,
        TextWriter errorWriter,
        CancellationToken cancellationToken)
    {
        var parseResults = new List<ParseResult>(inputs.Length);
        var hasError = false;

        foreach (var file in inputs)
        {
            var text = await File.ReadAllTextAsync(file.FullName, cancellationToken).ConfigureAwait(false);
            var result = LexiconCodeGenerator.Parse(text, file.FullName);
            parseResults.Add(result);

            foreach (var diag in result.Diagnostics)
            {
                PrintDiagnostic(diag, errorWriter);
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    hasError = true;
                }
            }
        }

        if (hasError)
        {
            return 1;
        }

        var generateResult = LexiconCodeGenerator.Generate(parseResults, generatedCodeNamespace, options);

        foreach (var diag in generateResult.Diagnostics)
        {
            PrintDiagnostic(diag, errorWriter);
            if (diag.Severity == DiagnosticSeverity.Error)
            {
                hasError = true;
            }
        }

        if (hasError)
        {
            return 1;
        }

        outputDir.Create();

        foreach (var file in generateResult.Files)
        {
            var outputPath = Path.Combine(outputDir.FullName, file.HintName);
            var dir = Path.GetDirectoryName(outputPath);
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }

            await File
                .WriteAllTextAsync(outputPath, file.SourceText, new UTF8Encoding(false), cancellationToken)
                .ConfigureAwait(false);
        }

        return 0;
    }

    private static void PrintDiagnostic(Diagnostic diag, TextWriter output)
    {
        var prefix = diag.Severity == DiagnosticSeverity.Error ? "error" : "warning";
        output.WriteLine($"{prefix}: {diag.Message}");
    }
}
