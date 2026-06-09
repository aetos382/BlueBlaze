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

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generatorRunAsBuildTaskProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (input, _) =>
                input.GlobalOptions.TryGetValue("build_property.BlueBlazeGeneratorRunAsBuildTask", out var stringValue) &&
                bool.TryParse(stringValue, out var boolValue) &&
                boolValue);

        var lexiconDocumentsProvider = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(generatorRunAsBuildTaskProvider)
            .Select(static (input, cancellationToken) =>
            {
                var ((additionalText, optionsProvider), runAsBuildTask) = input;

                if (runAsBuildTask)
                {
                    return default;
                }

                var options = optionsProvider.GetOptions(additionalText);
                if (!options.TryGetValue("build_metadata.LexiconDocument.IsLexiconDocument", out var stringValue) ||
                    !bool.TryParse(stringValue, out var boolValue) ||
                    !boolValue)
                {
                    return default;
                }

                var sourceText = additionalText.GetText(cancellationToken);
                if (sourceText is null)
                {
                    return default;
                }

                var text = sourceText.ToString();
                var result = (IsValidText: true, Text: text, Path: additionalText.Path);

                return result;
            })
            .Where(static item => item.IsValidText)
            .Select(static (input, _) =>
                LexiconGenerator.Parse(input.Text, input.Path));

        var generatedModelNamespaceProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (opts, _) =>
            {
                if (opts.GlobalOptions.TryGetValue("build_property.GeneratedModelNamespace", out var v) &&
                    !string.IsNullOrEmpty(v))
                {
                    return v;
                }

                opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNs);
                return string.IsNullOrEmpty(rootNs) ? null : rootNs + ".Generated";
            });

        context.RegisterSourceOutput(
            lexiconDocumentsProvider.Collect().Combine(generatedModelNamespaceProvider),
            static (spc, pair) =>
            {
                var (documents, generatedModelNamespace) = pair;
                var result = LexiconGenerator.Generate(documents, generatedModelNamespace);

                foreach (var diag in result.Diagnostics)
                {
                    var descriptor = diag.Severity == Core.DiagnosticSeverity.Error
                        ? ErrorDescriptor
                        : WarningDescriptor;
                    spc.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(descriptor, Location.None, diag.Message));
                }

                foreach (var file in result.Files)
                {
                    spc.AddSource(file.HintName, file.SourceText);
                }
            });
    }
}
