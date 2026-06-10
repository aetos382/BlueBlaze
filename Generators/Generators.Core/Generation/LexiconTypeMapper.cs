using System.Collections.Generic;

namespace BlueBlaze.Generators.Core.Generation;

internal static class LexiconTypeMapper
{
    // Maps a lexicon definition to its C# type string.
    // Returns null if the type is a union (handled separately by the caller) or unsupported.
    internal static MapResult? Map(
        LexiconDefinition def,
        bool isRequired,
        bool isNullable,
        string currentNsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        out string? unknownFormatName,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex,
        string? generatedCodeNamespace)
    {
        unknownFormatName = null;

        var nullable = !isRequired || isNullable;

        switch (def)
        {
            case BooleanDefinition:
                return new MapResult(nullable ? "bool?" : "bool", nullable);

            case IntegerDefinition:
                return new MapResult(nullable ? "int?" : "int", nullable);

            case StringDefinition sd:
                return MapString(sd, nullable, ref unknownFormatName);

            case TokenDefinition:
                return new MapResult(nullable ? "string?" : "string", nullable);

            case BytesDefinition:
                return new MapResult(nullable ? "byte[]?" : "byte[]", nullable);

            case CidLinkDefinition:
                return new MapResult(nullable ? "string?" : "string", nullable);

            case BlobDefinition:
                return new MapResult(nullable ? "object?" : "object", nullable);

            case ArrayDefinition ad:
                return MapArray(ad, isRequired, currentNsid, nsidIndex, defIndex, generatedCodeNamespace, out unknownFormatName);

            case ReferenceDefinition rd:
                // ref ターゲットが非クラス型 (string/token/integer 等) の場合はプリミティブ型を返す
                if (defIndex != null)
                {
                    var (targetNsid, targetDefKey) = LexiconNameHelper.ParseRef(rd.Ref, currentNsid);
                    var defKey = targetNsid + "#" + targetDefKey;
                    if (defIndex.TryGetValue(defKey, out var targetDef) && IsNonClassDef(targetDef))
                    {
                        return Map(targetDef, isRequired, isNullable, targetNsid, nsidIndex, out unknownFormatName, defIndex, generatedCodeNamespace);
                    }
                }
                var resolved = LexiconNameHelper.ResolveRef(currentNsid, rd.Ref, nsidIndex);
                var globalResolved = LexiconNameHelper.GlobalizeTypePath(resolved, generatedCodeNamespace);
                return new MapResult(nullable ? globalResolved + "?" : globalResolved, nullable);

            case UnionDefinition:
                return null; // 呼び出し側が union を個別に処理する

            case UnknownDefinition:
                return new MapResult(nullable ? "object?" : "object", nullable);

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
        bool nullable,
        ref string? unknownFormatName)
    {
        if (sd.Format == null)
        {
            return new MapResult(nullable ? "string?" : "string", nullable);
        }

        return sd.Format switch
        {
            StringFormat.DateTime => new MapResult(
                nullable ? "global::System.DateTimeOffset?" : "global::System.DateTimeOffset",
                nullable),

            StringFormat.Uri => new MapResult(
                nullable ? "global::System.Uri?" : "global::System.Uri",
                nullable),

            StringFormat.AtIdentifier or
            StringFormat.AtUri or
            StringFormat.Cid or
            StringFormat.Did or
            StringFormat.Handle or
            StringFormat.Nsid or
            StringFormat.Tid or
            StringFormat.RecordKey or
            StringFormat.Language =>
                new MapResult(nullable ? "string?" : "string", nullable),

            _ => FallbackString(sd.Format.ToString(), nullable, ref unknownFormatName)
        };
    }

    private static MapResult FallbackString(string formatName, bool nullable, ref string? unknownFormatName)
    {
        unknownFormatName = formatName;
        return new MapResult(nullable ? "string?" : "string", nullable);
    }

    private static MapResult? MapArray(
        ArrayDefinition ad,
        bool isRequired,
        string currentNsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex,
        string? generatedCodeNamespace,
        out string? unknownFormatName)
    {
        var itemResult = Map(ad.Items, isRequired: true, isNullable: false, currentNsid,
            nsidIndex, out unknownFormatName, defIndex, generatedCodeNamespace);

        if (itemResult == null)
        {
            return null; // Union items — caller must handle
        }

        var itemType = itemResult.IsNullable
            ? itemResult.CSharpType.TrimEnd('?')
            : itemResult.CSharpType;

        var listType = $"global::System.Collections.Generic.IReadOnlyList<{itemType}>";
        return new MapResult(isRequired ? listType : listType + "?", !isRequired);
    }
}

internal sealed record MapResult(string CSharpType, bool IsNullable);
