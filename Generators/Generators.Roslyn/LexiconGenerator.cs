using Microsoft.CodeAnalysis;

namespace BlueBlaze.Generators.Roslyn;

[Generator(LanguageNames.CSharp)]
public sealed class LexiconGenerator :
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
            .Select(static (input, _) =>
            {
                var ((additionalText, optionsProvider), runAsBuildTask) = input;

                if (runAsBuildTask)
                {
                    return 0;
                }

                var options = optionsProvider.GetOptions(additionalText);
                if (!options.TryGetValue("build_metadata.LexiconDocument.IsLexiconDocument", out var stringValue) ||
                    !bool.TryParse(stringValue, out var boolValue) ||
                    !boolValue)
                {
                    return 0;
                }

                return 1;
            });
    }
}
