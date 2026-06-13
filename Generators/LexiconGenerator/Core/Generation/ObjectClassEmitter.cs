using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal static class ObjectClassEmitter
{
    private sealed record PropInfo(
        string JsonKey,
        string CsPropName,
        string CsType,
        bool IsRequired,
        bool IsValueType,
        string? Initializer,
        string[]? EnumValues,
        string? EnumTypeName);

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
        bool nullableAnnotationsEnabled,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex,
        string? generatedCodeNamespace,
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
            var propInfos = CollectProperties(
                def, className, classPath, nsid, nsidIndex,
                diagnostics, filePath, defKey, defIndex, generatedCodeNamespace,
                nullableAnnotationsEnabled);

            EmitNestedEnumTypes(isb, propInfos, emitJsonAttributes);
            EmitProperties(isb, propInfos, emitJsonAttributes);
            EmitConstructor(isb, className, propInfos, emitJsonAttributes);
            EmitNestedUnionInterfaces(isb, unionProperties, nsid, nsidIndex, generatedCodeNamespace, emitJsonAttributes);
        }
        isb.AppendLine("}");
    }

    private static List<PropInfo> CollectProperties(
        ObjectDefinition def,
        string className,
        string classPath,
        string nsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        List<Diagnostic> diagnostics,
        string? filePath,
        string? defKey,
        IReadOnlyDictionary<string, LexiconDefinition>? defIndex,
        string? generatedCodeNamespace,
        bool nullableAnnotationsEnabled)
    {
        var result = new List<PropInfo>();

        if (def.Properties == null)
        {
            return result;
        }

        var requiredSet = new HashSet<string>(def.Required ?? []);
        var nullableSet = new HashSet<string>(def.Nullable ?? []);

        foreach (var kv in def.Properties)
        {
            var propName = kv.Key;
            var propDef = kv.Value;
            var isRequired = requiredSet.Contains(propName);
            var isInNullable = nullableSet.Contains(propName);

            string baseType;
            bool isValueType;
            string[]? enumValues = null;
            string? enumTypeName = null;

            if (propDef is UnionDefinition)
            {
                var interfaceName = "I" + LexiconNameHelper.ToPascalCase(propName);
                baseType = interfaceName;
                isValueType = false;
            }
            else if (propDef is StringDefinition { Enum: { Length: > 0 } enumVals })
            {
                enumValues = enumVals;
                enumTypeName = LexiconNameHelper.ToPascalCase(propName);
                baseType = enumTypeName;
                isValueType = true;
            }
            else
            {
                var mapped = LexiconTypeMapper.Map(
                    propDef, nsid, nsidIndex, out var unknownFormat, defIndex, generatedCodeNamespace);

                if (unknownFormat != null)
                {
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Warning,
                        DiagnosticMessages.FormatUnknownStringFormat(unknownFormat, nsid, defKey ?? ""),
                        filePath, nsid, defKey));
                }

                if (mapped == null)
                {
                    continue;
                }

                baseType = mapped.BaseType;
                isValueType = mapped.IsValueType;
            }

            var csPropName = LexiconNameHelper.ToPascalCase(propName);

            // CS0542: プロパティ名が囲む型名と同じ場合はリネーム
            if (csPropName == className)
            {
                csPropName += "Value";
            }
            else if (enumTypeName == null)
            {
                // CS0102: プロパティ名が同クラス内のネスト型と同じ場合はリネーム（non-enum のみ）
                var globalClassPath = LexiconNameHelper.GlobalizeTypePath(classPath, generatedCodeNamespace);
                if (baseType == globalClassPath + "." + csPropName)
                {
                    csPropName += "Value";
                }
            }
            else
            {
                // enum プロパティ: 型名とプロパティ名が常に衝突するためリネーム
                csPropName += "Value";
            }

            var csType = ComputeType(baseType, isValueType, isRequired, isInNullable, nullableAnnotationsEnabled);
            var initializer = ComputeInitializer(propDef, enumTypeName);

            result.Add(new PropInfo(propName, csPropName, csType, isRequired, isValueType, initializer, enumValues, enumTypeName));
        }

        return result;
    }

    // nullable rules:
    //   isInNullable: value type → T?, ref + enabled → T?, ref + disabled → T
    //   not required, not nullable: value type → T, ref + enabled → T?, ref + disabled → T
    //   required, not nullable: always T
    internal static string ComputeType(
        string baseType,
        bool isValueType,
        bool isRequired,
        bool isInNullable,
        bool nullableAnnotationsEnabled)
    {
        bool addQuestion;

        if (isInNullable)
        {
            addQuestion = isValueType || nullableAnnotationsEnabled;
        }
        else if (!isRequired)
        {
            addQuestion = !isValueType && nullableAnnotationsEnabled;
        }
        else
        {
            addQuestion = false;
        }

        return addQuestion ? baseType + "?" : baseType;
    }

    private static string? ComputeInitializer(LexiconDefinition propDef, string? enumTypeName)
    {
        switch (propDef)
        {
            case BooleanDefinition { Const: bool boolConst }:
                return $" = {(boolConst ? "true" : "false")};";
            case BooleanDefinition { Default: bool boolDefault }:
                return $" = {(boolDefault ? "true" : "false")};";

            case IntegerDefinition { Const: int intConst }:
                return $" = {intConst};";
            case IntegerDefinition { Default: int intDefault }:
                return $" = {intDefault};";

            case StringDefinition sd:
                var constVal = sd.Const ?? sd.Default;
                if (constVal == null)
                {
                    return null;
                }
                if (enumTypeName != null)
                {
                    return $" = {enumTypeName}.{LexiconNameHelper.ToPascalCase(constVal)};";
                }
                return $" = \"{EscapeString(constVal)}\";";

            default:
                return null;
        }
    }

    private static void EmitNestedEnumTypes(
        IndentedStringBuilder isb,
        List<PropInfo> propInfos,
        bool emitJsonAttributes)
    {
        foreach (var prop in propInfos)
        {
            if (prop.EnumValues == null || prop.EnumTypeName == null)
            {
                continue;
            }

            if (emitJsonAttributes)
            {
                isb.AppendLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof(global::System.Text.Json.Serialization.JsonStringEnumConverter<{prop.EnumTypeName}>))]");
            }

            isb.AppendLine($"public enum {prop.EnumTypeName}");
            isb.AppendLine("{");
            using (isb.Indent())
            {
                foreach (var val in prop.EnumValues)
                {
                    var memberName = LexiconNameHelper.ToPascalCase(val);
                    if (emitJsonAttributes)
                    {
                        isb.AppendLine($"[global::System.Text.Json.Serialization.JsonStringEnumMemberName(\"{val}\")]");
                    }
                    isb.AppendLine($"{memberName},");
                }
            }
            isb.AppendLine("}");
            isb.AppendLine();
        }
    }

    private static void EmitProperties(
        IndentedStringBuilder isb,
        List<PropInfo> propInfos,
        bool emitJsonAttributes)
    {
        foreach (var prop in propInfos)
        {
            if (emitJsonAttributes)
            {
                isb.AppendLine($"[global::System.Text.Json.Serialization.JsonPropertyName(\"{prop.JsonKey}\")]");
            }

            var initPart = prop.Initializer ?? "";
            isb.AppendLine($"public {prop.CsType} {prop.CsPropName} {{ get; set; }}{initPart}");
            isb.AppendLine();
        }
    }

    private static void EmitConstructor(
        IndentedStringBuilder isb,
        string className,
        List<PropInfo> propInfos,
        bool emitJsonAttributes)
    {
        var requiredProps = propInfos.Where(p => p.IsRequired).ToList();
        if (requiredProps.Count == 0)
        {
            return;
        }

        if (emitJsonAttributes)
        {
            isb.AppendLine("[global::System.Text.Json.Serialization.JsonConstructor]");
        }

        var paramList = string.Join(", ", requiredProps.Select(p => $"{p.CsType} {p.JsonKey}"));
        isb.AppendLine($"public {className}({paramList})");
        isb.AppendLine("{");
        using (isb.Indent())
        {
            foreach (var prop in requiredProps)
            {
                isb.AppendLine($"this.{prop.CsPropName} = {prop.JsonKey};");
            }
        }
        isb.AppendLine("}");
        isb.AppendLine();
    }

    private static void EmitNestedUnionInterfaces(
        IndentedStringBuilder isb,
        IReadOnlyDictionary<string, UnionDefinition>? unionProperties,
        string nsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedCodeNamespace,
        bool emitJsonAttributes)
    {
        if (unionProperties == null)
        {
            return;
        }

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

    private static string EscapeXml(string? s)
    {
        if (s == null)
        {
            return "";
        }

        return new StringBuilder(s)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .ToString();
    }

    private static string EscapeString(string s)
    {
#pragma warning disable CA1307
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
#pragma warning restore CA1307
    }
}
