using System.Collections.Generic;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

// Emits a single sealed partial class from an ObjectDefinition (or equivalent struct).
// Does NOT wrap in namespace or static container classes — DocumentEmitter handles that.
internal static class ObjectClassEmitter
{
    internal static void EmitClass(
        IndentedStringBuilder isb,
        string className,
        string classPath,
        ObjectDefinition def,
        string nsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        List<Diagnostic> diagnostics,
        string? filePath,
        string? defKey,
        bool isPartial,
        bool emitJsonAttributes,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex,
        string? generatedCodeNamespace,
        // Nested union interfaces to emit inside this class (propertyName -> UnionDefinition)
        IReadOnlyDictionary<string, UnionDefinition>? unionProperties = null)
    {
        if (!string.IsNullOrEmpty(def.Description))
        {
            isb.AppendLine($"/// <summary>{EscapeXml(def.Description)}</summary>");
        }

        var partialKeyword = isPartial ? "partial " : "";
        isb.AppendLine($"public sealed {partialKeyword}class {className}");
        isb.AppendLine("{");
        using (isb.Indent())
        {
            if (def.Properties != null)
            {
                var required = def.Required ?? [];
                var nullable = def.Nullable ?? [];
                var requiredSet = new HashSet<string>(required);
                var nullableSet = new HashSet<string>(nullable);

                foreach (var kv in def.Properties)
                {
                    var propName = kv.Key;
                    var propDef = kv.Value;
                    var isReq = requiredSet.Contains(propName);
                    var isNull = nullableSet.Contains(propName);

                    // Union property: emit interface reference
                    if (propDef is UnionDefinition)
                    {
                        var interfaceName = "I" + LexiconNameHelper.ToPascalCase(propName);
                        var propType = (isReq && !isNull) ? interfaceName : interfaceName + "?";
                        var csPropName = LexiconNameHelper.ToPascalCase(propName);
                        if (csPropName == className)
                        {
                            csPropName += "Value";
                        }

                        if (emitJsonAttributes)
                        {
                            isb.AppendLine($"[global::System.Text.Json.Serialization.JsonPropertyName(\"{propName}\")]");
                        }

                        isb.AppendLine(isReq
                            ? $"public required {propType} {csPropName} {{ get; init; }}"
                            : $"public {propType} {csPropName} {{ get; init; }}");

                        isb.AppendLine();
                        continue;
                    }

                    var result = LexiconTypeMapper.Map(
                        propDef, isReq, isNull, nsid, nsidIndex, out var unknownFormat, defIndex, generatedCodeNamespace);

                    if (unknownFormat != null)
                    {
                        diagnostics.Add(new Diagnostic(
                            DiagnosticSeverity.Warning,
                            DiagnosticMessages.FormatUnknownStringFormat(unknownFormat, nsid, defKey ?? ""),
                            filePath, nsid, defKey));
                    }

                    if (result == null)
                    {
                        // Unsupported type — skip
                        continue;
                    }

                    var csPropNameStr = LexiconNameHelper.ToPascalCase(propName);

                    // CS0542: プロパティ名が囲む型名と同じ場合はリネーム
                    if (csPropNameStr == className)
                    {
                        csPropNameStr += "Value";
                    }
                    else
                    {
                        // CS0102: プロパティ名が同クラス内のネスト型と同じ場合はリネーム
                        var fullTypePath = result.CSharpType.TrimEnd('?');
                        var globalClassPath = LexiconNameHelper.GlobalizeTypePath(classPath, generatedCodeNamespace);
                        if (fullTypePath == globalClassPath + "." + csPropNameStr)
                        {
                            csPropNameStr += "Value";
                        }
                    }

                    if (emitJsonAttributes)
                    {
                        isb.AppendLine($"[global::System.Text.Json.Serialization.JsonPropertyName(\"{propName}\")]");
                    }

                    isb.AppendLine(isReq
                        ? $"public required {result.CSharpType} {csPropNameStr} {{ get; init; }}"
                        : $"public {result.CSharpType} {csPropNameStr} {{ get; init; }}");

                    isb.AppendLine();
                }
            }

            // Emit nested union interfaces
            if (unionProperties != null)
            {
                foreach (var kv in unionProperties)
                {
                    var propName = kv.Key;
                    var ud = kv.Value;
                    var interfaceName = "I" + LexiconNameHelper.ToPascalCase(propName);

                    if (emitJsonAttributes)
                    {
                        isb.AppendLine($"[global::System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"$type\")]");
                        foreach (var refStr in ud.Refs)
                        {
                            var resolvedType = LexiconNameHelper.ResolveRef(nsid, refStr, nsidIndex);
                            var globalResolvedType = LexiconNameHelper.GlobalizeTypePath(resolvedType, generatedCodeNamespace);
                            var discriminator = LexiconNameHelper.GetTypeDiscriminator(refStr);
                            isb.AppendLine($"[global::System.Text.Json.Serialization.JsonDerivedType(typeof({globalResolvedType}), \"{discriminator}\")]");
                        }
                    }

                    isb.AppendLine($"public interface {interfaceName} {{ }}");
                    isb.AppendLine();
                }
            }
        }
        isb.AppendLine("}");
    }

    private static string EscapeXml(string s)
    {
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
