using System.Collections.Generic;
using System.Text.Json;

using BlueBlaze.Generators.Core.Generation;

namespace BlueBlaze.Generators.Core;

public sealed class LexiconGenerator
{
    public static LexiconDocumentWithInfo Parse(
        string text,
        string path)
    {
        var document = JsonSerializer.Deserialize(text, LexiconSerializerContext.Default.LexiconDocument)!;
        return new(path, document);
    }

    public static GenerateResult Generate(
        IReadOnlyList<LexiconDocumentWithInfo> documents,
        string? generatedModelNamespace = null)
    {
        var files = new List<GeneratedSourceFile>();
        var diagnostics = new List<Diagnostic>();
        var unionMemberImpls = new List<(string MemberTypePath, string InterfacePath)>();

        // Phase 1: Build NSID index (nsid -> main def type or null for defs-only)
        var nsidIndex = BuildNsidIndex(documents);


        // Phase 2: Emit source files per document, collect refs and extension data warnings
        _ = new List<(string RefStr, string SourceNsid, string? FilePath)>();
        foreach (var docInfo in documents)
        {
            DocumentEmitter.Emit(
                docInfo, nsidIndex, generatedModelNamespace,
                files, diagnostics, unionMemberImpls);
        }

        // Phase 3: Emit union member partial class files
        foreach (var (memberPath, interfacePath) in unionMemberImpls)
        {
            DocumentEmitter.EmitUnionMemberImpl(
                memberPath, interfacePath, generatedModelNamespace, files);
        }

        // Phase 4: Validate refs (check duplicate def keys were already handled during emit)
        // Ref validation: all refs collected during emit are checked against nsidIndex
        // (This is handled inline in DocumentEmitter; unresolved refs produce Error diagnostics there.)

        return new GenerateResult(files, diagnostics);
    }

    private static Dictionary<string, string?> BuildNsidIndex(
        IReadOnlyList<LexiconDocumentWithInfo> documents)
    {
        var index = new Dictionary<string, string?>();
        foreach (var docInfo in documents)
        {
            var nsid = docInfo.Document.Id;
            if (index.ContainsKey(nsid))
            {
                continue;
            }

            string? mainType = null;
            if (docInfo.Document.Definitions.TryGetValue("main", out var mainDef))
            {
                mainType = mainDef.Type switch
                {
                    LexiconType.Record => "record",
                    LexiconType.Object => "object",
                    LexiconType.Query => "query",
                    LexiconType.Procedure => "procedure",
                    LexiconType.Subscription => "subscription",
                    _ => mainDef.Type.ToString().ToLowerInvariant()
                };
            }

            index[nsid] = mainType;
        }
        return index;
    }
}
