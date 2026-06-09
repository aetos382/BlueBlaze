using System.Globalization;
using System.Resources;

namespace BlueBlaze.Generators.Core;

internal static class DiagnosticMessages
{
    private static readonly ResourceManager ResourceManager =
        new(
            "BlueBlaze.Generators.Core.DiagnosticMessages",
            typeof(DiagnosticMessages).Assembly);

#pragma warning disable CA1304

    private static string ParseError =>
        ResourceManager.GetString(nameof(ParseError)) ?? "Failed to parse lexicon document '{0}': {1}";

    private static string UnresolvedRef =>
        ResourceManager.GetString(nameof(UnresolvedRef)) ?? "Cannot resolve ref '{0}' in '{1}#{2}'.";

    private static string DuplicateDefKey =>
        ResourceManager.GetString(nameof(DuplicateDefKey)) ?? "Duplicate def key '{0}' in '{1}'.";

    private static string UnknownLexiconType =>
        ResourceManager.GetString(nameof(UnknownLexiconType)) ?? "Unknown lexicon type '{0}' in '{1}#{2}'. The def will be skipped.";

    private static string UnknownStringFormat =>
        ResourceManager.GetString(nameof(UnknownStringFormat)) ?? "Unknown string format '{0}' in '{1}#{2}'. Falling back to 'string'.";

    private static string UnknownExtensionData =>
        ResourceManager.GetString(nameof(UnknownExtensionData)) ?? "Unknown field(s) '{0}' found in '{1}#{2}'. The lexicon schema may have been updated.";

#pragma warning restore CA1304

    internal static string FormatParseError(string path, string exMessage)
    {
        return string.Format(CultureInfo.InvariantCulture, ParseError, path, exMessage);
    }

    internal static string FormatUnresolvedRef(string refStr, string nsid, string defKey)
    {
        return string.Format(CultureInfo.InvariantCulture, UnresolvedRef, refStr, nsid, defKey);
    }

    internal static string FormatDuplicateDefKey(string defKey, string nsid)
    {
        return string.Format(CultureInfo.InvariantCulture, DuplicateDefKey, defKey, nsid);
    }

    internal static string FormatUnknownLexiconType(LexiconType type, string nsid, string defKey)
    {
        return string.Format(CultureInfo.InvariantCulture, UnknownLexiconType, type, nsid, defKey);
    }

    internal static string FormatUnknownStringFormat(string format, string nsid, string defKey)
    {
        return string.Format(CultureInfo.InvariantCulture, UnknownStringFormat, format, nsid, defKey);
    }

    internal static string FormatUnknownExtensionData(string keys, string nsid, string? defKey)
    {
        return string.Format(CultureInfo.InvariantCulture, UnknownExtensionData, keys, nsid, defKey ?? "");
    }
}
