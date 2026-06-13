using System.Collections.Generic;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal static class LexiconTypeMapper
{
    // Maps a lexicon definition to its base C# type (without ? annotation).
    // Nullability is determined by the emitter based on context.
    // Returns null for UnionDefinition (caller handles separately).
    // StringDefinition with Enum is NOT handled here — caller checks directly.
    internal static MapResult? Map(
        LexiconDefinition def,
        string currentNsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        out string? unknownFormatName,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex,
        string? generatedCodeNamespace)
    {
        unknownFormatName = null;

        switch (def)
        {
            case BooleanDefinition:
                return new MapResult("bool", IsValueType: true);

            case IntegerDefinition:
                return new MapResult("int", IsValueType: true);

            case StringDefinition sd:
                return MapString(sd, ref unknownFormatName);

            case TokenDefinition:
                return new MapResult("string", IsValueType: false);

            case BytesDefinition:
                return new MapResult("byte[]", IsValueType: false);

            case CidLinkDefinition:
                return new MapResult("string", IsValueType: false);

            case BlobDefinition:
                return new MapResult("object", IsValueType: false);

            case ArrayDefinition ad:
                return MapArray(ad, currentNsid, nsidIndex, defIndex, generatedCodeNamespace, out unknownFormatName);

            case ReferenceDefinition rd:
                if (defIndex != null)
                {
                    var (targetNsid, targetDefKey) = LexiconNameHelper.ParseRef(rd.Ref, currentNsid);
                    var defKey = targetNsid + "#" + targetDefKey;
                    if (defIndex.TryGetValue(defKey, out var targetDef) && IsNonClassDef(targetDef))
                    {
                        return Map(targetDef, targetNsid, nsidIndex, out unknownFormatName, defIndex, generatedCodeNamespace);
                    }
                }
                var resolved = LexiconNameHelper.ResolveRef(currentNsid, rd.Ref, nsidIndex);
                var globalResolved = LexiconNameHelper.GlobalizeTypePath(resolved, generatedCodeNamespace);
                return new MapResult(globalResolved, IsValueType: false);

            case UnionDefinition:
                return null;

            case UnknownDefinition:
                return new MapResult("object", IsValueType: false);

            default:
                return null;
        }
    }

    private static bool IsNonClassDef(LexiconDefinition def)
    {
        return def is not ObjectDefinition and not RecordDefinition;
    }

    private static MapResult MapString(
        StringDefinition sd,
        ref string? unknownFormatName)
    {
        // StringDefinition with Enum is handled by the emitter, not here.
        if (sd.Format == null)
        {
            return new MapResult("string", IsValueType: false);
        }

        return sd.Format switch
        {
            StringFormat.DateTime => new MapResult("global::System.DateTimeOffset", IsValueType: true),

            StringFormat.Uri => new MapResult("global::System.Uri", IsValueType: false),

            StringFormat.AtIdentifier or
            StringFormat.AtUri or
            StringFormat.Cid or
            StringFormat.Did or
            StringFormat.Handle or
            StringFormat.Nsid or
            StringFormat.Tid or
            StringFormat.RecordKey or
            StringFormat.Language =>
                new MapResult("string", IsValueType: false),

            _ => FallbackString($"{sd.Format}", ref unknownFormatName)
        };
    }

    private static MapResult FallbackString(string formatName, ref string? unknownFormatName)
    {
        unknownFormatName = formatName;
        return new MapResult("string", IsValueType: false);
    }

    private static MapResult? MapArray(
        ArrayDefinition ad,
        string currentNsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex,
        string? generatedCodeNamespace,
        out string? unknownFormatName)
    {
        var itemResult = Map(ad.Items, currentNsid, nsidIndex, out unknownFormatName, defIndex, generatedCodeNamespace);

        if (itemResult == null)
        {
            return null;
        }

        var listType = $"global::System.Collections.Generic.IReadOnlyList<{itemResult.BaseType}>";
        return new MapResult(listType, IsValueType: false);
    }
}

internal sealed record MapResult(string BaseType, bool IsValueType);
