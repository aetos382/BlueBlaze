using System.Collections.Generic;

using BlueBlaze.LexiconGenerator.Core;

using Microsoft.CodeAnalysis;

namespace BlueBlaze.LexiconGenerator.Roslyn;

[Generator(LanguageNames.CSharp)]
public sealed class LexiconSourceGenerator :
    IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor ErrorDescriptor = new(
        id: "BB0001",
        title: new LocalizableResourceString(nameof(Resources.ErrorDescriptorTitle), Resources.ResourceManager, typeof(Resources)),
        messageFormat: "{0}",
        category: "LexiconGenerator",
        defaultSeverity: Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor WarningDescriptor = new(
        id: "BB0002",
        title: new LocalizableResourceString(nameof(Resources.WarningDescriptorTitle), Resources.ResourceManager, typeof(Resources)),
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
                        ? Resources.RunAsBuildTaskNotSet
                        : Resources.FormatRunAsBuildTaskInvalidValue(stringValue);

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
                        ? Resources.FormatIsLexiconDocumentNotSet(additionalText.Path)
                        : Resources.FormatIsLexiconDocumentInvalidValue(metaValue, additionalText.Path);

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
                        Resources.FormatCouldNotReadFile(additionalText.Path),
                        additionalText.Path, null, null))
                    : new LexiconFileResult(LexiconCodeGenerator.Parse(sourceText.ToString(), additionalText.Path), null);
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
                        Resources.GeneratedCodeNamespaceRequired,
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

                var result = LexiconCodeGenerator.Generate(parseResults, generatedCodeNamespace!);

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
