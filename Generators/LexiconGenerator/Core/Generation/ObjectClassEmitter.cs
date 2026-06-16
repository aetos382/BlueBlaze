using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal static class ObjectClassEmitter
{
    private const string MetadataNs = "global::BlueBlaze.Core";

    private sealed record PropInfo(
        string JsonKey,
        string CsPropName,
        string CsType,
        bool IsRequired,
        bool IsValueType,
        string? Initializer,
        IReadOnlyList<string> MetadataAttributeLines);

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
        IReadOnlyDictionary<string, UnionDefinition>? unionProperties = null,
        bool emitMetadataAttributes = false)
    {
        if (!string.IsNullOrEmpty(def.Description))
        {
            isb.AppendLine($"/// <summary>{EscapeXml(def.Description)}</summary>");
        }

        if (emitMetadataAttributes && def.Description is { Length: > 0 } classDesc)
        {
            isb.AppendLine($"[{MetadataNs}.LexiconDescription(\"{EscapeString(classDesc)}\")]");
        }

        var partialKeyword = isPartial ? "partial " : "";
        isb.AppendLine($"public sealed {partialKeyword}class {className}");
        isb.AppendLine("{");
        using (isb.Indent())
        {
            var propInfos = CollectProperties(
                def, className, classPath, nsid, nsidIndex,
                diagnostics, filePath, defKey, defIndex, generatedCodeNamespace,
                nullableAnnotationsEnabled, emitMetadataAttributes);

            EmitProperties(isb, propInfos, emitJsonAttributes);
            EmitConstructor(isb, className, propInfos, emitJsonAttributes);
            EmitNestedUnionInterfaces(isb, unionProperties, nsid, nsidIndex, generatedCodeNamespace,
                emitJsonAttributes, emitMetadataAttributes);
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
        bool nullableAnnotationsEnabled,
        bool emitMetadataAttributes)
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

            if (propDef is UnionDefinition)
            {
                var interfaceName = "I" + LexiconNameHelper.ToPascalCase(propName);
                baseType = interfaceName;
                isValueType = false;
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
            else
            {
                // CS0102: プロパティ名が同クラス内のネスト型と同じ場合はリネーム
                var globalClassPath = LexiconNameHelper.GlobalizeTypePath(classPath, generatedCodeNamespace);
                if (baseType == globalClassPath + "." + csPropName)
                {
                    csPropName += "Value";
                }
            }

            var csType = ComputeType(baseType, isValueType, isRequired, isInNullable, nullableAnnotationsEnabled);
            var initializer = ComputeInitializer(propDef);
            var metadataLines = emitMetadataAttributes
                ? BuildMetadataAttributeLines(propDef, nsid, nsidIndex, generatedCodeNamespace)
                : [];

            result.Add(new PropInfo(propName, csPropName, csType, isRequired, isValueType, initializer, metadataLines));
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

    private static string? ComputeInitializer(LexiconDefinition propDef)
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
                return $" = \"{EscapeString(constVal)}\";";

            default:
                return null;
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

            foreach (var attrLine in prop.MetadataAttributeLines)
            {
                isb.AppendLine(attrLine);
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
        bool emitJsonAttributes,
        bool emitMetadataAttributes)
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

            if (emitMetadataAttributes)
            {
                var typeArgs = string.Join(", ", ud.Refs.Select(r =>
                {
                    var resolved = LexiconNameHelper.ResolveRef(nsid, r, nsidIndex);
                    return $"typeof({LexiconNameHelper.GlobalizeTypePath(resolved, generatedCodeNamespace)})";
                }));
                var closedPart = ud.Closed == true ? ", Closed = true" : "";
                isb.AppendLine($"[{MetadataNs}.LexiconUnion({typeArgs}{closedPart})]");
            }

            isb.AppendLine($"public interface {interfaceName} {{ }}");
            isb.AppendLine();
        }
    }

    private static List<string> BuildMetadataAttributeLines(
        LexiconDefinition def,
        string nsid,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex,
        string? generatedCodeNamespace)
    {
        var lines = new List<string>();

        if (def.Description is { Length: > 0 } propDesc)
        {
            lines.Add($"[{MetadataNs}.LexiconDescription(\"{EscapeString(propDesc)}\")]");
        }

        switch (def)
        {
            case BooleanDefinition bd:
                if (bd.Const.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconConst(typeof(bool), {(bd.Const.Value ? "true" : "false")})]");
                }
                else if (bd.Default.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconDefault(typeof(bool), {(bd.Default.Value ? "true" : "false")})]");
                }
                break;

            case IntegerDefinition id:
                if (id.Minimum.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMinimum(typeof(int), {id.Minimum.Value})]");
                }
                if (id.Maximum.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMaximum(typeof(int), {id.Maximum.Value})]");
                }
                if (id.Const.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconConst(typeof(int), {id.Const.Value})]");
                }
                else if (id.Default.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconDefault(typeof(int), {id.Default.Value})]");
                }
                if (id.Enum is { Length: > 0 } intEnum)
                {
                    lines.Add($"[{MetadataNs}.LexiconEnum(typeof(int), {string.Join(", ", intEnum)})]");
                }
                break;

            case StringDefinition sd:
                if (sd.Format.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconStringFormat(\"{FormatToString(sd.Format.Value)}\")]");
                }
                if (sd.MinLength.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMinLength({sd.MinLength.Value})]");
                }
                if (sd.MaxLength.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMaxLength({sd.MaxLength.Value})]");
                }
                if (sd.MinGraphemes.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMinGraphemes({sd.MinGraphemes.Value})]");
                }
                if (sd.MaxGraphemes.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMaxGraphemes({sd.MaxGraphemes.Value})]");
                }
                if (sd.Const != null)
                {
                    lines.Add($"[{MetadataNs}.LexiconConst(typeof(string), \"{EscapeString(sd.Const)}\")]");
                }
                else if (sd.Default != null)
                {
                    lines.Add($"[{MetadataNs}.LexiconDefault(typeof(string), \"{EscapeString(sd.Default)}\")]");
                }
                if (sd.KnownValues is { Length: > 0 } knownVals)
                {
                    lines.Add($"[{MetadataNs}.LexiconKnownValues({string.Join(", ", knownVals.Select(v => $"\"{EscapeString(v)}\""))})]");
                }
                if (sd.Enum is { Length: > 0 } strEnum)
                {
                    lines.Add($"[{MetadataNs}.LexiconEnum(typeof(string), {string.Join(", ", strEnum.Select(v => $"\"{EscapeString(v)}\""))})]");
                }
                break;

            case ArrayDefinition ad:
                if (ad.MinLength.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMinLength({ad.MinLength.Value})]");
                }
                if (ad.MaxLength.HasValue)
                {
                    lines.Add($"[{MetadataNs}.LexiconMaxLength({ad.MaxLength.Value})]");
                }
                break;

            case UnionDefinition ud:
            {
                var typeArgs = string.Join(", ", ud.Refs.Select(r =>
                {
                    var resolved = LexiconNameHelper.ResolveRef(nsid, r, nsidIndex);
                    return $"typeof({LexiconNameHelper.GlobalizeTypePath(resolved, generatedCodeNamespace)})";
                }));
                var closedPart = ud.Closed == true ? ", Closed = true" : "";
                lines.Add($"[{MetadataNs}.LexiconUnion({typeArgs}{closedPart})]");
                break;
            }

            default:
                break;
        }

        return lines;
    }

    private static string FormatToString(StringFormat format)
    {
        return format switch
        {
            StringFormat.AtIdentifier => "at-identifier",
            StringFormat.AtUri => "at-uri",
            StringFormat.Cid => "cid",
            StringFormat.DateTime => "datetime",
            StringFormat.Did => "did",
            StringFormat.Handle => "handle",
            StringFormat.Language => "language",
            StringFormat.Nsid => "nsid",
            StringFormat.RecordKey => "record-key",
            StringFormat.Tid => "tid",
            StringFormat.Uri => "uri",
            _ => $"{format}"
        };
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
