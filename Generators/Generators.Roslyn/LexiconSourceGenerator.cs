using BlueBlaze.Generators.Core;

using Microsoft.CodeAnalysis;

namespace BlueBlaze.Generators.Roslyn;

[Generator(LanguageNames.CSharp)]
public sealed class LexiconSourceGenerator :
    IIncrementalGenerator
{
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
    }
}
