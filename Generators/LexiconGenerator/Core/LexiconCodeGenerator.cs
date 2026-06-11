using System;
using System.Collections.Generic;
using System.Text.Json;

using BlueBlaze.LexiconGenerator.Core.Generation;

namespace BlueBlaze.LexiconGenerator.Core;

public sealed class LexiconCodeGenerator
{
    public static ParseResult Parse(
        string text,
        string path)
    {
        try
        {
            var document = JsonSerializer.Deserialize(text, LexiconSerializerContext.Default.LexiconDocument);
            if (document is null)
            {
                var message = DiagnosticMessages.FormatParseError(path, "Document is null.");
                return new ParseResult(null, [new Diagnostic(DiagnosticSeverity.Error, message, path, null, null)]);
            }

            if (document.Lexicon != 1)
            {
                var message = DiagnosticMessages.FormatInvalidLexiconVersion(document.Lexicon, path);
                return new ParseResult(null, [new Diagnostic(DiagnosticSeverity.Error, message, path, null, null)]);
            }

            return new ParseResult(new LexiconDocumentWithInfo(path, document), []);
        }
        catch (JsonException ex)
        {
            var message = DiagnosticMessages.FormatParseError(path, ex.Message);
            return new ParseResult(null, [new Diagnostic(DiagnosticSeverity.Error, message, path, null, null)]);
        }
    }

    public static GenerateResult Generate(
        IReadOnlyList<ParseResult> parseResults,
        string generatedCodeNamespace)
    {
        ArgumentNullException.ThrowIfNull(parseResults);

        var files = new List<GeneratedSourceFile>();
        var diagnostics = new List<Diagnostic>();
        var unionMemberImpls = new List<(string MemberTypePath, string InterfacePath)>();

        // Collect parse diagnostics and extract successfully parsed documents
        var documents = new List<LexiconDocumentWithInfo>(parseResults.Count);
        foreach (var pr in parseResults)
        {
            diagnostics.AddRange(pr.Diagnostics);
            if (pr.Document != null)
            {
                documents.Add(pr.Document);
            }
        }

        // Phase 1: Build NSID index (nsid -> main def type or null for defs-only)
        var nsidIndex = BuildNsidIndex(documents);

        // Phase 1b: Build def index ("nsid#defKey" -> LexiconDefinition) for ref resolution
        var defIndex = BuildDefIndex(documents);

        // Phase 2: Emit source files per document
        foreach (var docInfo in documents)
        {
            DocumentEmitter.Emit(
                docInfo, nsidIndex, generatedCodeNamespace,
                files, diagnostics, unionMemberImpls, defIndex);
        }

        // Phase 3: Emit union member partial class files
        foreach (var (memberPath, interfacePath) in unionMemberImpls)
        {
            DocumentEmitter.EmitUnionMemberImpl(
                memberPath, interfacePath, generatedCodeNamespace, files);
        }

        return new GenerateResult(files, diagnostics);
    }

    private static Dictionary<string, LexiconType?> BuildNsidIndex(
        IReadOnlyList<LexiconDocumentWithInfo> documents)
    {
        var index = new Dictionary<string, LexiconType?>();

        foreach (var docInfo in documents)
        {
            var nsid = docInfo.Document.Id;
            if (index.ContainsKey(nsid))
            {
                continue;
            }

            LexiconType? mainType = null;
            if (docInfo.Document.Definitions.TryGetValue("main", out var mainDef))
            {
                mainType = mainDef.Type;
            }

            index[nsid] = mainType;
        }

        return index;
    }

    // "nsid#defKey" -> LexiconDefinition のインデックスを構築する
    private static Dictionary<string, LexiconDefinition> BuildDefIndex(
        IReadOnlyList<LexiconDocumentWithInfo> documents)
    {
        var index = new Dictionary<string, LexiconDefinition>();

        foreach (var docInfo in documents)
        {
            var nsid = docInfo.Document.Id;
            foreach (var kv in docInfo.Document.Definitions)
            {
                var key = nsid + "#" + kv.Key;
                if (!index.ContainsKey(key))
                {
                    index[key] = kv.Value;
                }
            }
        }

        return index;
    }
}
