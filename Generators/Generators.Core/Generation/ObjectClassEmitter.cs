using System.Collections.Generic;
using System.Text;

namespace BlueBlaze.Generators.Core.Generation;

// Emits a single sealed partial class from an ObjectDefinition (or equivalent struct).
// Does NOT wrap in namespace or static container classes — DocumentEmitter handles that.
internal static class ObjectClassEmitter
{
    internal static void EmitClass(
        StringBuilder sb,
        string className,
        ObjectDefinition def,
        string nsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        List<Diagnostic> diagnostics,
        string? filePath,
        string? defKey,
        int indentLevel,
        bool isPartial,
        bool emitJsonAttributes,
        // Nested union interfaces to emit inside this class (propertyName -> UnionDefinition)
        IReadOnlyDictionary<string, UnionDefinition>? unionProperties = null)
    {
        var indent = new string(' ', indentLevel * 4);
        var indent1 = new string(' ', (indentLevel + 1) * 4);

        if (!string.IsNullOrEmpty(def.Description))
        {
            sb.AppendLine($"{indent}/// <summary>{EscapeXml(def.Description)}</summary>");
        }

        var partialKeyword = isPartial ? "partial " : "";
        sb.AppendLine($"{indent}public sealed {partialKeyword}class {className}");
        sb.AppendLine($"{indent}{{");

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
                    var propType = isReq ? interfaceName : interfaceName + "?";
                    var csPropName = LexiconNameHelper.ToPascalCase(propName);

                    if (emitJsonAttributes)
                    {
                        sb.AppendLine($"{indent1}[global::System.Text.Json.Serialization.JsonPropertyName(\"{propName}\")]");
                    }

                    if (isReq)
                    {
                        sb.AppendLine($"{indent1}public required {propType} {csPropName} {{ get; init; }}");
                    }
                    else
                    {
                        sb.AppendLine($"{indent1}public {propType} {csPropName} {{ get; init; }}");
                    }

                    sb.AppendLine();
                    continue;
                }

                var result = LexiconTypeMapper.Map(
                    propDef, isReq, isNull, nsid, nsidIndex, out var unknownFormat);

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

                if (emitJsonAttributes)
                {
                    sb.AppendLine($"{indent1}[global::System.Text.Json.Serialization.JsonPropertyName(\"{propName}\")]");
                }

                if (isReq && !isNull)
                {
                    sb.AppendLine($"{indent1}public required {result.CSharpType} {csPropNameStr} {{ get; init; }}");
                }
                else
                {
                    sb.AppendLine($"{indent1}public {result.CSharpType} {csPropNameStr} {{ get; init; }}");
                }

                sb.AppendLine();
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

                sb.AppendLine($"{indent1}[global::System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"$type\")]");
                foreach (var refStr in ud.Refs)
                {
                    var resolvedType = LexiconNameHelper.ResolveRef(nsid, refStr, nsidIndex);
                    var discriminator = LexiconNameHelper.GetTypeDiscriminator(refStr);
                    sb.AppendLine($"{indent1}[global::System.Text.Json.Serialization.JsonDerivedType(typeof({resolvedType}), \"{discriminator}\")]");
                }

                _ = (ud.Closed == true) ? "" : "";
                sb.AppendLine($"{indent1}public interface {interfaceName} {{ }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}}}");
    }

    private static string EscapeXml(string s)
    {
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
