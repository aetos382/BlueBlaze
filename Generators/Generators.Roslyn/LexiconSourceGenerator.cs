using System.Collections.Generic;

using BlueBlaze.Generators.Core;

using Microsoft.CodeAnalysis;

namespace BlueBlaze.Generators.Roslyn;

[Generator(LanguageNames.CSharp)]
public sealed class LexiconSourceGenerator :
    IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor ErrorDescriptor = new(
        id: "BB0001",
        title: "Lexicon generation error",
        messageFormat: "{0}",
        category: "LexiconGenerator",
        defaultSeverity: Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor WarningDescriptor = new(
        id: "BB0002",
        title: "Lexicon generation warning",
        messageFormat: "{0}",
        category: "LexiconGenerator",
        defaultSeverity: Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private readonly record struct LexiconFileResult(
        ParseResult? ParseResult,
        Core.Diagnostic? Diagnostic)
    {
        public static LexiconFileResult Skip { get; } = new(null, null);

        public bool IsSkip => this is (ParseResult: null, Diagnostic: null);
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var runAsBuildTaskProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (input, _) =>
            {
                var value = false;
                Core.Diagnostic? diagnostic = null;

                if (!input.GlobalOptions.TryGetValue("build_property.BlueBlazeGeneratorRunAsBuildTask", out var stringValue) ||
                    !bool.TryParse(stringValue, out value))
                {
                    var message = string.IsNullOrEmpty(stringValue)
                        ? "The MSBuild property 'BlueBlazeGeneratorRunAsBuildTask' is not set. It must be set to 'true' or 'false'."
                        : $"The MSBuild property 'BlueBlazeGeneratorRunAsBuildTask' has an invalid value '{stringValue}'. It must be 'true' or 'false'.";

                    diagnostic = new Core.Diagnostic(Core.DiagnosticSeverity.Error, message, null, null, null);
                }

                return (Value: value, Diagnostic: diagnostic);
            });

        var lexiconFilesProvider = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(runAsBuildTaskProvider)
            .Select(static (input, cancellationToken) =>
            {
                var ((additionalText, optionsProvider), (runAsBuildTask, runAsBuildTaskDiag)) = input;

                if (runAsBuildTask || runAsBuildTaskDiag is not null)
                {
                    return LexiconFileResult.Skip;
                }

                var options = optionsProvider.GetOptions(additionalText);
                if (!options.TryGetValue("build_metadata.LexiconDocument.IsLexiconDocument", out var metaValue) ||
                    !bool.TryParse(metaValue, out var isLexiconDocument))
                {
                    var message = string.IsNullOrEmpty(metaValue)
                        ? $"The MSBuild metadata 'IsLexiconDocument' is not set on '{additionalText.Path}'. The file will be skipped."
                        : $"The MSBuild metadata 'IsLexiconDocument' has an invalid value '{metaValue}' on '{additionalText.Path}'. It must be 'true' or 'false'. The file will be skipped.";

                    return new LexiconFileResult(null, new Core.Diagnostic(Core.DiagnosticSeverity.Warning, message, additionalText.Path, null, null));
                }

                if (!isLexiconDocument)
                {
                    return LexiconFileResult.Skip;
                }

                var sourceText = additionalText.GetText(cancellationToken);
                return sourceText is null
                    ? new LexiconFileResult(null, new Core.Diagnostic(
                        Core.DiagnosticSeverity.Warning,
                        $"Could not read the content of '{additionalText.Path}'. The file will be skipped.",
                        additionalText.Path, null, null))
                    : new LexiconFileResult(LexiconGenerator.Parse(sourceText.ToString(), additionalText.Path), null);
            })
            .Where(static item => !item.IsSkip);

        var generatedCodeNamespaceProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (opts, _) =>
            {
                string? value = null;
                Core.Diagnostic? diagnostic = null;

                if (opts.GlobalOptions.TryGetValue("build_property.BlueBlazeGeneratedCodeNamespace", out var v) &&
                    !string.IsNullOrEmpty(v))
                {
                    value = v;
                }
                else
                {
                    diagnostic = new Core.Diagnostic(
                        Core.DiagnosticSeverity.Error,
                        "The MSBuild property 'BlueBlazeGeneratedCodeNamespace' is required.",
                        null, null, null);
                }

                return (Value: value, Diagnostic: diagnostic);
            });

        context.RegisterSourceOutput(
            lexiconFilesProvider.Collect()
                .Combine(runAsBuildTaskProvider)
                .Combine(generatedCodeNamespaceProvider),
            static (spc, pair) =>
            {
                var ((fileResults, (runAsBuildTask, runAsBuildTaskDiag)), (generatedCodeNamespace, namespaceDiag)) = pair;

                if (runAsBuildTaskDiag is not null)
                {
                    ReportDiagnostic(spc, runAsBuildTaskDiag);
                }

                foreach (var fileResult in fileResults)
                {
                    if (fileResult.Diagnostic is not null)
                    {
                        ReportDiagnostic(spc, fileResult.Diagnostic);
                    }
                }

                if (namespaceDiag is not null)
                {
                    ReportDiagnostic(spc, namespaceDiag);
                }

                if (runAsBuildTask || runAsBuildTaskDiag is not null || namespaceDiag is not null)
                {
                    return;
                }

                var parseResults = new List<ParseResult>(fileResults.Length);
                foreach (var fileResult in fileResults)
                {
                    if (fileResult.ParseResult is not null)
                    {
                        parseResults.Add(fileResult.ParseResult);
                    }
                }

                var result = LexiconGenerator.Generate(parseResults, generatedCodeNamespace!);

                foreach (var diag in result.Diagnostics)
                {
                    ReportDiagnostic(spc, diag);
                }

                foreach (var file in result.Files)
                {
                    spc.AddSource(file.HintName, file.SourceText);
                }
            });
    }

    private static void ReportDiagnostic(SourceProductionContext spc, Core.Diagnostic diag)
    {
        var descriptor = diag.Severity == Core.DiagnosticSeverity.Error
            ? ErrorDescriptor
            : WarningDescriptor;
        spc.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(descriptor, Location.None, diag.Message));
    }
}
