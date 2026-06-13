using System.Collections.Generic;
using System.Linq;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal static class DocumentEmitter
{
    // Emits all source files for one LexiconDocumentWithInfo.
    // Returns generated files and appends diagnostics.
    internal static void Emit(
        LexiconDocumentWithInfo docInfo,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedCodeNamespace,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        // Collects (unionInterfaceFqn, refStr) pairs for partial class generation
        List<(string MemberTypePath, string InterfacePath)> unionMemberImpls,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null,
        bool nullableAnnotationsEnabled = true,
        bool emitMetadataAttributes = false)
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
                    nsidIndex, generatedCodeNamespace,
                    filePath, files, diagnostics, unionMemberImpls,
                    emitJsonAttributes: emitJson, defIndex: defIndex,
                    nullableAnnotationsEnabled: nullableAnnotationsEnabled,
                    emitMetadataAttributes: emitMetadataAttributes);
            }
            else if (def is RecordDefinition recDef)
            {
                EmitObjectDef(
                    nsid, defKey, recDef.Record, mainType, isMain,
                    nsidIndex, generatedCodeNamespace,
                    filePath, files, diagnostics, unionMemberImpls,
                    emitJsonAttributes: true, defIndex: defIndex,
                    nullableAnnotationsEnabled: nullableAnnotationsEnabled,
                    emitMetadataAttributes: emitMetadataAttributes);
            }
            else if (def is QueryDefinition queryDef && isMain)
            {
                EmitQueryProcedure(nsid, queryDef.Parameters, null, queryDef.Output, queryDef.Errors,
                    nsidIndex, generatedCodeNamespace, filePath, files, diagnostics, unionMemberImpls, defIndex,
                    nullableAnnotationsEnabled, emitMetadataAttributes);
            }
            else if (def is ProcedureDefinition procDef && isMain)
            {
                EmitQueryProcedure(nsid, procDef.Parameters, procDef.Input, procDef.Output, procDef.Errors,
                    nsidIndex, generatedCodeNamespace, filePath, files, diagnostics, unionMemberImpls, defIndex,
                    nullableAnnotationsEnabled, emitMetadataAttributes);
            }
            else if (def is SubscriptionDefinition subDef && isMain)
            {
                EmitSubscription(nsid, subDef, nsidIndex, generatedCodeNamespace,
                    filePath, files, diagnostics, unionMemberImpls, defIndex,
                    nullableAnnotationsEnabled, emitMetadataAttributes);
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
        string? generatedCodeNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        bool emitJsonAttributes,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null,
        bool nullableAnnotationsEnabled = true,
        bool emitMetadataAttributes = false)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);
        string className;
        string classPath;
        string hintSuffix;

        if (isMain && mainType is LexiconType.Record or LexiconType.Object)
        {
            // Case 1: main class — last segment is the class name
            className = segments[^1];
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
            // Case 3: Output/Message プレフィックスを付けた兄弟クラスとして emit
            var responseOrMessage = mainType == LexiconType.Subscription ? "Message" : "Output";
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

        var isb = new IndentedStringBuilder();
        EmitFileHeader(isb, generatedCodeNamespace);

        if (isMain && mainType is LexiconType.Record or LexiconType.Object)
        {
            // Wrap in parent sealed classes (all segments except last)
            OpenSealedContainers(isb, segments, 0, segments.Length - 1);
            ObjectClassEmitter.EmitClass(isb, className, classPath, objDef, nsid, nsidIndex,
                diagnostics, filePath, defKey,
                isPartial: true, emitJsonAttributes: emitJsonAttributes,
                nullableAnnotationsEnabled: nullableAnnotationsEnabled,
                defIndex: defIndex, generatedCodeNamespace: generatedCodeNamespace,
                unionProperties: unionProps, emitMetadataAttributes: emitMetadataAttributes);
            CloseContainers(isb, segments.Length - 1);
        }
        else
        {
            // Case 1 sub-def, Case 2, Case 3, defs-only
            OpenSealedContainers(isb, segments, 0, segments.Length);
            ObjectClassEmitter.EmitClass(isb, className, classPath, objDef, nsid, nsidIndex,
                diagnostics, filePath, defKey,
                isPartial: true, emitJsonAttributes: emitJsonAttributes,
                nullableAnnotationsEnabled: nullableAnnotationsEnabled,
                defIndex: defIndex, generatedCodeNamespace: generatedCodeNamespace,
                unionProperties: unionProps, emitMetadataAttributes: emitMetadataAttributes);
            CloseContainers(isb, segments.Length);
        }

        var hintName = LexiconNameHelper.GetHintNameBase(generatedCodeNamespace, hintSuffix) + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, isb.ToString()));

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
        ErrorDefinition[]? errors,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedCodeNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null,
        bool nullableAnnotationsEnabled = true,
        bool emitMetadataAttributes = false)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);

        EmitExtensionDataWarnings(input?.ExtensionData, filePath, nsid, "main", diagnostics);
        EmitExtensionDataWarnings(output?.ExtensionData, filePath, nsid, "main", diagnostics);
        foreach (var error in errors ?? [])
        {
            EmitExtensionDataWarnings(error.ExtensionData, filePath, nsid, "main", diagnostics);
        }

        if (parameters != null && parameters.Properties != null)
        {
            EmitParametersClass(nsid, segments, parameters, nsidIndex,
                generatedCodeNamespace, filePath, files, diagnostics, defIndex,
                nullableAnnotationsEnabled);
        }

        if (input?.Schema is ObjectDefinition inputObj)
        {
            EmitOperationDataClass(nsid, segments, "Input", inputObj, nsidIndex,
                generatedCodeNamespace, filePath, files, diagnostics, unionMemberImpls,
                emitJsonAttributes: true, defIndex: defIndex,
                nullableAnnotationsEnabled: nullableAnnotationsEnabled,
                emitMetadataAttributes: emitMetadataAttributes);
        }

        if (output?.Schema is ObjectDefinition outputObj)
        {
            EmitOperationDataClass(nsid, segments, "Output", outputObj, nsidIndex,
                generatedCodeNamespace, filePath, files, diagnostics, unionMemberImpls,
                emitJsonAttributes: true, defIndex: defIndex,
                nullableAnnotationsEnabled: nullableAnnotationsEnabled,
                emitMetadataAttributes: emitMetadataAttributes);
        }
    }

    private static void EmitSubscription(
        string nsid,
        SubscriptionDefinition subDef,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedCodeNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null,
        bool nullableAnnotationsEnabled = true,
        bool emitMetadataAttributes = false)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);

        EmitExtensionDataWarnings(subDef.Message.ExtensionData, filePath, nsid, "main", diagnostics);
        foreach (var error in subDef.Errors ?? [])
        {
            EmitExtensionDataWarnings(error.ExtensionData, filePath, nsid, "main", diagnostics);
        }

        if (subDef.Parameters != null && subDef.Parameters.Properties != null)
        {
            EmitParametersClass(nsid, segments, subDef.Parameters, nsidIndex,
                generatedCodeNamespace, filePath, files, diagnostics, defIndex,
                nullableAnnotationsEnabled);
        }

        if (subDef.Message.Schema is ObjectDefinition msgObj)
        {
            // No JSON attributes for subscription message (CBOR encoded)
            EmitOperationDataClass(nsid, segments, "Message", msgObj, nsidIndex,
                generatedCodeNamespace, filePath, files, diagnostics, unionMemberImpls,
                emitJsonAttributes: false, defIndex: defIndex,
                nullableAnnotationsEnabled: nullableAnnotationsEnabled,
                emitMetadataAttributes: emitMetadataAttributes);
        }
    }

    private static void EmitParametersClass(
        string nsid,
        string[] segments,
        ParametersDefinition paramsDef,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedCodeNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null,
        bool nullableAnnotationsEnabled = true)
    {
        var isb = new IndentedStringBuilder();
        EmitFileHeader(isb, generatedCodeNamespace);
        OpenSealedContainers(isb, segments, 0, segments.Length);

        var requiredSet = new HashSet<string>(paramsDef.Required ?? []);
        var requiredProps = new List<(string JsonKey, string CsPropName, string CsType)>();

        isb.AppendLine("public sealed partial class Parameters");
        isb.AppendLine("{");
        using (isb.Indent())
        {
            if (paramsDef.Properties != null)
            {
                foreach (var kv in paramsDef.Properties)
                {
                    var propName = kv.Key;
                    var isRequired = requiredSet.Contains(propName);

                    var mapped = LexiconTypeMapper.Map(
                        kv.Value, nsid, nsidIndex, out var unknownFormat, defIndex, generatedCodeNamespace);

                    if (unknownFormat != null)
                    {
                        diagnostics.Add(new Diagnostic(
                            DiagnosticSeverity.Warning,
                            DiagnosticMessages.FormatUnknownStringFormat(unknownFormat, nsid, "main"),
                            filePath, nsid, "main"));
                    }

                    if (mapped == null)
                    {
                        continue;
                    }

                    // Non-required params always get T? so ToDictionary() can use HasValue/null checks
                    var csType = isRequired
                        ? ObjectClassEmitter.ComputeType(mapped.BaseType, mapped.IsValueType, isRequired: true, isInNullable: false, nullableAnnotationsEnabled)
                        : mapped.BaseType + "?";

                    var csPropName = LexiconNameHelper.ToPascalCase(propName);
                    if (csPropName == "Parameters")
                    {
                        csPropName += "Value";
                    }

                    // No [JsonPropertyName] for URL query string parameters
                    isb.AppendLine($"public {csType} {csPropName} {{ get; set; }}");
                    isb.AppendLine();

                    if (isRequired)
                    {
                        requiredProps.Add((propName, csPropName, csType));
                    }
                }
            }

            if (requiredProps.Count > 0)
            {
                var paramList = string.Join(", ", requiredProps.Select(p => $"{p.CsType} {p.JsonKey}"));
                isb.AppendLine($"public Parameters({paramList})");
                isb.AppendLine("{");
                using (isb.Indent())
                {
                    foreach (var (jsonKey, csPropName, _) in requiredProps)
                    {
                        isb.AppendLine($"this.{csPropName} = {jsonKey};");
                    }
                }
                isb.AppendLine("}");
                isb.AppendLine();
            }
        }
        isb.AppendLine("}");
        CloseContainers(isb, segments.Length);

        var classPath = string.Join(".", segments) + ".Parameters";
        var hintName = LexiconNameHelper.GetHintNameBase(generatedCodeNamespace, classPath) + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, isb.ToString()));
    }

    private static void EmitOperationDataClass(
        string nsid,
        string[] segments,
        string className,
        ObjectDefinition objDef,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedCodeNamespace,
        string? filePath,
        List<GeneratedSourceFile> files,
        List<Diagnostic> diagnostics,
        List<(string, string)> unionMemberImpls,
        bool emitJsonAttributes,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex = null,
        bool nullableAnnotationsEnabled = true,
        bool emitMetadataAttributes = false)
    {
        var unionProps = CollectUnionProperties(objDef);

        var isb = new IndentedStringBuilder();
        EmitFileHeader(isb, generatedCodeNamespace);
        OpenSealedContainers(isb, segments, 0, segments.Length);

        var classPath = string.Join(".", segments) + "." + className;
        ObjectClassEmitter.EmitClass(isb, className, classPath, objDef, nsid, nsidIndex,
            diagnostics, filePath, "main",
            isPartial: true, emitJsonAttributes: emitJsonAttributes,
            nullableAnnotationsEnabled: nullableAnnotationsEnabled,
            defIndex: defIndex, generatedCodeNamespace: generatedCodeNamespace,
            unionProperties: unionProps, emitMetadataAttributes: emitMetadataAttributes);

        CloseContainers(isb, segments.Length);

        var hintName = LexiconNameHelper.GetHintNameBase(generatedCodeNamespace, classPath) + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, isb.ToString()));

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
        string? generatedCodeNamespace,
        List<GeneratedSourceFile> files)
    {
        // memberTypePath: e.g. "App.Bsky.Embed.Images"
        // interfacePath:  e.g. "App.Bsky.Feed.Post.IEmbed"
        var parts = memberTypePath.Split('.');
        if (parts.Length < 2)
        {
            return;
        }

        var isb = new IndentedStringBuilder();
        EmitFileHeader(isb, generatedCodeNamespace);

        OpenSealedContainers(isb, parts, 0, parts.Length - 1);

        var className = parts[^1];
        var globalInterfacePath = LexiconNameHelper.GlobalizeTypePath(interfacePath, generatedCodeNamespace);
        isb.AppendLine($"public sealed partial class {className} : {globalInterfacePath} {{ }}");
        isb.AppendLine();

        CloseContainers(isb, parts.Length - 1);

        // HintName encodes both member path and interface
        var interfaceShort = interfacePath.Replace('.', '_');
        var hintName = LexiconNameHelper.GetHintNameBase(
            generatedCodeNamespace,
            memberTypePath + "." + interfaceShort) + ".g.cs";

        files.Add(new GeneratedSourceFile(hintName, isb.ToString()));
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

    private static void EmitFileHeader(IndentedStringBuilder isb, string? generatedCodeNamespace)
    {
        isb.AppendLine("// <auto-generated/>");
        isb.AppendLine("#nullable enable");
        isb.AppendLine("#pragma warning disable CS1591");
        isb.AppendLine();
        if (!string.IsNullOrEmpty(generatedCodeNamespace))
        {
            isb.AppendLine($"namespace {generatedCodeNamespace};");
            isb.AppendLine();
        }
    }

    private static void OpenSealedContainers(IndentedStringBuilder isb, string[] segments, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            isb.AppendLine($"public sealed partial class {segments[i]}");
            isb.AppendLine("{");
            isb.Indent();
        }
    }

    private static void CloseContainers(IndentedStringBuilder isb, int count)
    {
        for (var i = count - 1; i >= 0; i--)
        {
            isb.Dedent();
            isb.AppendLine("}");
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
