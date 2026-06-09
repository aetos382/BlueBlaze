using System.Collections.Generic;
using System.Text;

namespace BlueBlaze.Generators.Core.Generation;

internal static class DocumentEmitter
{
    // Emits all source files for one LexiconDocumentWithInfo.
    // Returns generated files and appends diagnostics.
    internal static void Emit(
        LexiconDocumentWithInfo docInfo,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedModelNamespace,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        // Collects (unionInterfaceFqn, refStr) pairs for partial class generation
        List<(string MemberTypePath, string InterfacePath)> unionMemberImpls,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null)
    {
        var doc = docInfo.Document;
        var nsid = doc.Id;
        var filePath = docInfo.Path;

        // Check for extension data warnings at document level
        EmitExtensionDataWarnings(doc.ExtensionData, filePath, nsid, null, diagnostics);

        foreach (var kv in doc.Definitions)
        {
            var defKey = kv.Key;
            var def = kv.Value;

            EmitExtensionDataWarnings(def.ExtensionData, filePath, nsid, defKey, diagnostics);

            // Determine main def type for dispatch
            nsidIndex.TryGetValue(nsid, out var mainType);
            var isMain = defKey == "main";

            if (def is ObjectDefinition objDef)
            {
                // Subscription sub-defs are CBOR encoded → no JSON attributes
                var emitJson = !(mainType == LexiconType.Subscription && !isMain);
                EmitObjectDef(
                    nsid, defKey, objDef, mainType, isMain,
                    nsidIndex, generatedModelNamespace,
                    filePath, files, diagnostics, unionMemberImpls,
                    emitJsonAttributes: emitJson, defIndex: defIndex);
            }
            else if (def is RecordDefinition recDef)
            {
                EmitObjectDef(
                    nsid, defKey, recDef.Record, mainType, isMain,
                    nsidIndex, generatedModelNamespace,
                    filePath, files, diagnostics, unionMemberImpls,
                    emitJsonAttributes: true, defIndex: defIndex);
            }
            else if (def is QueryDefinition queryDef && isMain)
            {
                EmitQueryProcedure(nsid, queryDef.Parameters, null, queryDef.Output,
                    nsidIndex, generatedModelNamespace, filePath, files, diagnostics, unionMemberImpls, defIndex);
            }
            else if (def is ProcedureDefinition procDef && isMain)
            {
                EmitQueryProcedure(nsid, procDef.Parameters, procDef.Input, procDef.Output,
                    nsidIndex, generatedModelNamespace, filePath, files, diagnostics, unionMemberImpls, defIndex);
            }
            else if (def is SubscriptionDefinition subDef && isMain)
            {
                EmitSubscription(nsid, subDef, nsidIndex, generatedModelNamespace,
                    filePath, files, diagnostics, unionMemberImpls, defIndex);
            }
            else if (isMain && def.Type == LexiconType.PermissionSet)
            {
                // Silently skip
            }
            else if (isMain && def is not RecordDefinition && def is not QueryDefinition
                     && def is not ProcedureDefinition && def is not SubscriptionDefinition
                     && def is not ObjectDefinition)
            {
                // Unknown main type
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Warning,
                    DiagnosticMessages.FormatUnknownLexiconType(def.Type, nsid, defKey),
                    filePath, nsid, defKey));
            }
        }
    }

    private static void EmitObjectDef(
        string nsid,
        string defKey,
        ObjectDefinition objDef,
        LexiconType? mainType,
        bool isMain,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedModelNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        bool emitJsonAttributes,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);
        string className;
        string classPath;
        string hintSuffix;

        if (isMain && mainType is LexiconType.Record or LexiconType.Object)
        {
            // Case 1: main class — last segment is the class name
            className = segments[segments.Length - 1];
            classPath = string.Join(".", segments);
            hintSuffix = classPath;
        }
        else if (!isMain && mainType is LexiconType.Record or LexiconType.Object)
        {
            // Case 1: sub-def nested inside main partial class
            className = LexiconNameHelper.ToPascalCase(defKey);
            classPath = string.Join(".", segments) + "." + className;
            hintSuffix = classPath;
        }
        else if (!isMain && mainType is LexiconType.Query or LexiconType.Procedure or LexiconType.Subscription)
        {
            // Case 3: Response/Message プレフィックスを付けた兄弟クラスとして emit
            var responseOrMessage = mainType == LexiconType.Subscription ? "Message" : "Response";
            className = responseOrMessage + LexiconNameHelper.ToPascalCase(defKey);
            classPath = string.Join(".", segments) + "." + className;
            hintSuffix = classPath;
        }
        else
        {
            // Case 2: defs-only (isMain && mainType==null) or non-main def
            className = LexiconNameHelper.ToPascalCase(defKey);
            classPath = string.Join(".", segments) + "." + className;
            hintSuffix = classPath;
        }

        // Collect union properties for interface generation
        var unionProps = CollectUnionProperties(objDef);

        var sb = new StringBuilder();
        EmitFileHeader(sb, generatedModelNamespace);

        if (isMain && mainType is LexiconType.Record or LexiconType.Object)
        {
            // Wrap in parent static classes (all segments except last)
            OpenStaticContainers(sb, segments, 0, segments.Length - 1);
            ObjectClassEmitter.EmitClass(sb, className, classPath, objDef, nsid, nsidIndex,
                diagnostics, filePath, defKey, segments.Length - 1,
                isPartial: true, emitJsonAttributes: emitJsonAttributes,
                defIndex: defIndex, generatedModelNamespace: generatedModelNamespace,
                unionProperties: unionProps);
            CloseContainers(sb, segments.Length - 1);
        }
        else
        {
            // Case 1 sub-def, Case 2, Case 3, defs-only
            OpenStaticContainers(sb, segments, 0, segments.Length);
            ObjectClassEmitter.EmitClass(sb, className, classPath, objDef, nsid, nsidIndex,
                diagnostics, filePath, defKey, segments.Length,
                isPartial: true, emitJsonAttributes: emitJsonAttributes,
                defIndex: defIndex, generatedModelNamespace: generatedModelNamespace,
                unionProperties: unionProps);
            CloseContainers(sb, segments.Length);
        }

        var hintName = LexiconNameHelper.GetHintNameBase(generatedModelNamespace, hintSuffix) + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, sb.ToString()));

        // Queue union member partial class files (interfacePath は相対パスで格納し EmitUnionMemberImpl でグローバル化する)
        foreach (var up in unionProps)
        {
            var interfacePath = classPath + ".I" + LexiconNameHelper.ToPascalCase(up.Key);
            foreach (var refStr in up.Value.Refs)
            {
                var memberPath = LexiconNameHelper.ResolveRef(nsid, refStr, nsidIndex);
                unionMemberImpls.Add((memberPath, interfacePath));
            }
        }
    }

    private static void EmitQueryProcedure(
        string nsid,
        ParametersDefinition? parameters,
        InputDefinition? input,
        OutputDefinition? output,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedModelNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);

        if (parameters != null && parameters.Properties != null)
        {
            EmitParametersClass(nsid, segments, parameters, nsidIndex,
                generatedModelNamespace, filePath, files, diagnostics, defIndex);
        }

        if (input?.Schema is ObjectDefinition inputObj)
        {
            EmitOperationDataClass(nsid, segments, "Request", inputObj, nsidIndex,
                generatedModelNamespace, filePath, files, diagnostics, unionMemberImpls,
                emitJsonAttributes: true, defIndex: defIndex);
        }

        if (output?.Schema is ObjectDefinition outputObj)
        {
            EmitOperationDataClass(nsid, segments, "Response", outputObj, nsidIndex,
                generatedModelNamespace, filePath, files, diagnostics, unionMemberImpls,
                emitJsonAttributes: true, defIndex: defIndex);
        }
    }

    private static void EmitSubscription(
        string nsid,
        SubscriptionDefinition subDef,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedModelNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);

        if (subDef.Parameters != null && subDef.Parameters.Properties != null)
        {
            EmitParametersClass(nsid, segments, subDef.Parameters, nsidIndex,
                generatedModelNamespace, filePath, files, diagnostics, defIndex);
        }

        if (subDef.Message.Schema is ObjectDefinition msgObj)
        {
            // No JSON attributes for subscription message (CBOR encoded)
            EmitOperationDataClass(nsid, segments, "Message", msgObj, nsidIndex,
                generatedModelNamespace, filePath, files, diagnostics, unionMemberImpls,
                emitJsonAttributes: false, defIndex: defIndex);
        }
    }

    private static void EmitParametersClass(
        string nsid,
        string[] segments,
        ParametersDefinition paramsDef,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedModelNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null)
    {
        var sb = new StringBuilder();
        EmitFileHeader(sb, generatedModelNamespace);
        OpenStaticContainers(sb, segments, 0, segments.Length);

        var indent = new string(' ', segments.Length * 4);
        var indent1 = new string(' ', (segments.Length + 1) * 4);

        sb.AppendLine($"{indent}public sealed class Parameters");
        sb.AppendLine($"{indent}{{");

        if (paramsDef.Properties != null)
        {
            var requiredSet = new HashSet<string>(paramsDef.Required ?? []);
            foreach (var kv in paramsDef.Properties)
            {
                var propName = kv.Key;
                var isReq = requiredSet.Contains(propName);
                var result = LexiconTypeMapper.Map(kv.Value, isReq, false, nsid, nsidIndex, out var unknownFormat, defIndex, generatedModelNamespace);

                if (unknownFormat != null)
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Warning,
                        DiagnosticMessages.FormatUnknownStringFormat(unknownFormat, nsid, "main"),
                        filePath, nsid, "main"));
                }

                if (result == null)
                {
                    continue;
                }

                var csPropName = LexiconNameHelper.ToPascalCase(propName);
                // No [JsonPropertyName] for URL query string parameters
                if (isReq)
                {
                    sb.AppendLine($"{indent1}public required {result.CSharpType} {csPropName} {{ get; init; }}");
                }
                else
                {
                    sb.AppendLine($"{indent1}public {result.CSharpType} {csPropName} {{ get; init; }}");
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}}}");
        CloseContainers(sb, segments.Length);

        var classPath = string.Join(".", segments) + ".Parameters";
        var hintName = LexiconNameHelper.GetHintNameBase(generatedModelNamespace, classPath) + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, sb.ToString()));
    }

    private static void EmitOperationDataClass(
        string nsid,
        string[] segments,
        string className,
        ObjectDefinition objDef,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedModelNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        bool emitJsonAttributes,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null)
    {
        var unionProps = CollectUnionProperties(objDef);

        var sb = new StringBuilder();
        EmitFileHeader(sb, generatedModelNamespace);
        OpenStaticContainers(sb, segments, 0, segments.Length);

        var classPath = string.Join(".", segments) + "." + className;
        ObjectClassEmitter.EmitClass(sb, className, classPath, objDef, nsid, nsidIndex,
            diagnostics, filePath, "main", segments.Length,
            isPartial: true, emitJsonAttributes: emitJsonAttributes,
            defIndex: defIndex, generatedModelNamespace: generatedModelNamespace,
            unionProperties: unionProps);

        CloseContainers(sb, segments.Length);

        var hintName = LexiconNameHelper.GetHintNameBase(generatedModelNamespace, classPath) + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, sb.ToString()));

        // Queue union member partial class files (interfacePath は相対パスで格納し EmitUnionMemberImpl でグローバル化する)
        foreach (var up in unionProps)
        {
            var interfacePath = classPath + ".I" + LexiconNameHelper.ToPascalCase(up.Key);
            foreach (var refStr in up.Value.Refs)
            {
                var memberPath = LexiconNameHelper.ResolveRef(nsid, refStr, nsidIndex);
                unionMemberImpls.Add((memberPath, interfacePath));
            }
        }
    }

    // Emits a partial class declaration for a union member type implementing an interface.
    internal static void EmitUnionMemberImpl(
        string memberTypePath,
        string interfacePath,
        string? generatedModelNamespace,
        List<GeneratedSourceFile> files)
    {
        // memberTypePath: e.g. "App.Bsky.Embed.Images"
        // interfacePath:  e.g. "App.Bsky.Feed.Post.IEmbed"
        var parts = memberTypePath.Split('.');
        if (parts.Length < 2)
        {
            return;
        }

        var sb = new StringBuilder();
        EmitFileHeader(sb, generatedModelNamespace);

        OpenStaticContainers(sb, parts, 0, parts.Length - 1);

        var indent = new string(' ', (parts.Length - 1) * 4);
        var className = parts[parts.Length - 1];
        var globalInterfacePath = LexiconNameHelper.GlobalizeTypePath(interfacePath, generatedModelNamespace);
        sb.AppendLine($"{indent}public sealed partial class {className} : {globalInterfacePath} {{ }}");
        sb.AppendLine();

        CloseContainers(sb, parts.Length - 1);

        // HintName encodes both member path and interface
        var interfaceShort = interfacePath.Replace(".", "_");
        var hintName = LexiconNameHelper.GetHintNameBase(
            generatedModelNamespace,
            memberTypePath + "." + interfaceShort) + ".g.cs";

        files.Add(new GeneratedSourceFile(hintName, sb.ToString()));
    }

    private static Dictionary<string, UnionDefinition> CollectUnionProperties(ObjectDefinition objDef)
    {
        var result = new Dictionary<string, UnionDefinition>();
        if (objDef.Properties == null)
        {
            return result;
        }

        foreach (var kv in objDef.Properties)
        {
            if (kv.Value is UnionDefinition ud)
            {
                result[kv.Key] = ud;
            }
        }
        return result;
    }

    private static void EmitFileHeader(StringBuilder sb, string? generatedModelNamespace)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(generatedModelNamespace))
        {
            sb.AppendLine($"namespace {generatedModelNamespace};");
            sb.AppendLine();
        }
    }

    private static void OpenStaticContainers(StringBuilder sb, string[] segments, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            var indent = new string(' ', i * 4);
            sb.AppendLine($"{indent}public static partial class {segments[i]}");
            sb.AppendLine($"{indent}{{");
        }
    }

    private static void CloseContainers(StringBuilder sb, int count)
    {
        for (var i = count - 1; i >= 0; i--)
        {
            var indent = new string(' ', i * 4);
            sb.AppendLine($"{indent}}}");
        }
    }

    private static void EmitExtensionDataWarnings(
        System.Collections.Generic.IDictionary<string, System.Text.Json.JsonElement>? extensionData,
        string? filePath,
        string nsid,
        string? defKey,
        List<Diagnostic> diagnostics)
    {
        if (extensionData == null || extensionData.Count == 0)
        {
            return;
        }

        var keys = string.Join(", ", extensionData.Keys);
        diagnostics.Add(new Diagnostic(
            DiagnosticSeverity.Warning,
            DiagnosticMessages.FormatUnknownExtensionData(keys, nsid, defKey),
            filePath, nsid, defKey));
    }
}
