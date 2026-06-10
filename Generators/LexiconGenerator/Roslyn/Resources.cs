using System.Globalization;
using System.Resources;

namespace BlueBlaze.LexiconGenerator.Roslyn;

internal static class Resources
{
    internal static readonly ResourceManager ResourceManager =
        new(
            "BlueBlaze.LexiconGenerator.Roslyn.Resources",
            typeof(Resources).Assembly);

#pragma warning disable CA1304

    internal static string ErrorDescriptorTitle =>
        ResourceManager.GetString(nameof(ErrorDescriptorTitle)) ?? "Lexicon generation error";

    internal static string WarningDescriptorTitle =>
        ResourceManager.GetString(nameof(WarningDescriptorTitle)) ?? "Lexicon generation warning";

    internal static string RunAsBuildTaskNotSet =>
        ResourceManager.GetString(nameof(RunAsBuildTaskNotSet)) ?? "The MSBuild property 'BlueBlazeGeneratorRunAsBuildTask' is not set. It must be set to 'true' or 'false'.";

    private static string RunAsBuildTaskInvalidValue =>
        ResourceManager.GetString(nameof(RunAsBuildTaskInvalidValue)) ?? "The MSBuild property 'BlueBlazeGeneratorRunAsBuildTask' has an invalid value '{0}'. It must be 'true' or 'false'.";

    private static string IsLexiconDocumentNotSet =>
        ResourceManager.GetString(nameof(IsLexiconDocumentNotSet)) ?? "The MSBuild metadata 'IsLexiconDocument' is not set on '{0}'. The file will be skipped.";

    private static string IsLexiconDocumentInvalidValue =>
        ResourceManager.GetString(nameof(IsLexiconDocumentInvalidValue)) ?? "The MSBuild metadata 'IsLexiconDocument' has an invalid value '{0}' on '{1}'. It must be 'true' or 'false'. The file will be skipped.";

    private static string CouldNotReadFile =>
        ResourceManager.GetString(nameof(CouldNotReadFile)) ?? "Could not read the content of '{0}'. The file will be skipped.";

    internal static string GeneratedCodeNamespaceRequired =>
        ResourceManager.GetString(nameof(GeneratedCodeNamespaceRequired)) ?? "The MSBuild property 'BlueBlazeGeneratedCodeNamespace' is required.";

#pragma warning restore CA1304

    internal static string FormatRunAsBuildTaskInvalidValue(string? value)
    {
        return string.Format(CultureInfo.InvariantCulture, RunAsBuildTaskInvalidValue, value);
    }

    internal static string FormatIsLexiconDocumentNotSet(string path)
    {
        return string.Format(CultureInfo.InvariantCulture, IsLexiconDocumentNotSet, path);
    }

    internal static string FormatIsLexiconDocumentInvalidValue(string? value, string path)
    {
        return string.Format(CultureInfo.InvariantCulture, IsLexiconDocumentInvalidValue, value, path);
    }

    internal static string FormatCouldNotReadFile(string path)
    {
        return string.Format(CultureInfo.InvariantCulture, CouldNotReadFile, path);
    }
}
