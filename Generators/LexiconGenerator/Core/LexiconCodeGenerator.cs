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
        string generatedCodeNamespace,
        GeneratorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(parseResults);

        options ??= GeneratorOptions.Default;

        var files = new List<GeneratedSourceFile>();
        var diagnostics = new List<Diagnostic>();
        var unionMemberImpls = new List<(string MemberTypePath, string InterfacePath)>();

        var documents = new List<LexiconDocumentWithInfo>(parseResults.Count);
        foreach (var pr in parseResults)
        {
            diagnostics.AddRange(pr.Diagnostics);
            if (pr.Document != null)
            {
                documents.Add(pr.Document);
            }
        }

        var nsidIndex = BuildNsidIndex(documents);
        var defIndex = BuildDefIndex(documents);

        // Phase 1: Emit DTO classes (DocumentEmitter)
        foreach (var docInfo in documents)
        {
            DocumentEmitter.Emit(
                docInfo, nsidIndex, generatedCodeNamespace,
                files, diagnostics, unionMemberImpls, defIndex,
                options.NullableAnnotationsEnabled);
        }

        // Phase 2: Emit union member partial class files
        foreach (var (memberPath, interfacePath) in unionMemberImpls)
        {
            DocumentEmitter.EmitUnionMemberImpl(
                memberPath, interfacePath, generatedCodeNamespace, files);
        }

        // Phase 3: Emit client support types and collect NSID prefixes
        var seenPrefixes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var docInfo in documents)
        {
            var nsid = docInfo.Document.Id;
            if (!docInfo.Document.Definitions.TryGetValue("main", out var mainDef))
            {
                continue;
            }

            var segments = LexiconNameHelper.NsidToSegments(nsid);
            bool isQuery;
            bool hasInput;
            bool hasOutput;
            bool hasParameters;

            if (mainDef is QueryDefinition queryDef)
            {
                isQuery = true;
                hasInput = false;
                hasOutput = queryDef.Output?.Schema is ObjectDefinition;
                hasParameters = queryDef.Parameters?.Properties?.Count > 0;

                if (hasParameters)
                {
                    ParametersEmitter.Emit(segments, queryDef.Parameters!, generatedCodeNamespace, files);
                }
            }
            else if (mainDef is ProcedureDefinition procDef)
            {
                isQuery = false;
                hasInput = procDef.Input?.Schema is ObjectDefinition;
                hasOutput = procDef.Output?.Schema is ObjectDefinition;
                hasParameters = false;

                if (hasInput)
                {
                    InputEmitter.Emit(nsid, options, generatedCodeNamespace, files);
                }
            }
            else
            {
                continue;
            }

            RequestEmitter.Emit(nsid, segments, isQuery, hasParameters, hasInput, generatedCodeNamespace, files);

            if (hasOutput)
            {
                DeserializerEmitter.Emit(segments, options, generatedCodeNamespace, files);
            }

            for (var i = 1; i < segments.Length; i++)
            {
                seenPrefixes.Add(string.Join(".", segments, 0, i));
            }
        }

        // Phase 4: Emit client prefix structs (shortest prefix first)
        var sortedPrefixes = new List<string>(seenPrefixes);
        sortedPrefixes.Sort(StringComparer.Ordinal);
        foreach (var prefix in sortedPrefixes)
        {
            ClientPrefixEmitter.Emit(prefix.Split('.'), generatedCodeNamespace, files);
        }

        // Phase 5: Emit client extension methods
        foreach (var docInfo in documents)
        {
            ClientMethodEmitter.Emit(docInfo, options, generatedCodeNamespace, files);
        }

        // Phase 6: Emit JSON serializer context (generateTypeInfo = true のみ)
        if (options.GenerateTypeInfo)
        {
            JsonSerializerContextEmitter.Emit(documents, generatedCodeNamespace, nsidIndex, files);
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
