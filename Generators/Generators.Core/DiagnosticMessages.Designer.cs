using System.Resources;

namespace BlueBlaze.Generators.Core;

internal static class DiagnosticMessages
{
    private static ResourceManager? _resourceManager;

    private static ResourceManager ResourceManager =>
        _resourceManager ??= new ResourceManager(
            "BlueBlaze.Generators.Core.DiagnosticMessages",
            typeof(DiagnosticMessages).Assembly);

    internal static string ParseError =>
        ResourceManager.GetString("ParseError") ?? "Failed to parse lexicon document '{0}': {1}";

    internal static string UnresolvedRef =>
        ResourceManager.GetString("UnresolvedRef") ?? "Cannot resolve ref '{0}' in '{1}#{2}'.";

    internal static string DuplicateDefKey =>
        ResourceManager.GetString("DuplicateDefKey") ?? "Duplicate def key '{0}' in '{1}'.";

    internal static string UnknownLexiconType =>
        ResourceManager.GetString("UnknownLexiconType") ?? "Unknown lexicon type '{0}' in '{1}#{2}'. The def will be skipped.";

    internal static string UnknownStringFormat =>
        ResourceManager.GetString("UnknownStringFormat") ?? "Unknown string format '{0}' in '{1}#{2}'. Falling back to 'string'.";

    internal static string UnknownExtensionData =>
        ResourceManager.GetString("UnknownExtensionData") ?? "Unknown field(s) '{0}' found in '{1}#{2}'. The lexicon schema may have been updated.";
}
