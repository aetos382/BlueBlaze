using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace BlueBlaze.CommandGenerator.Generation;

/// <summary>
/// <c>Compilation</c>(自身 + 参照アセンブリ)を Symbol 解析し、
/// <c>[Lexicon(nsid, kind)]</c> が付与された Request クラスとその Parameters/Input のプロパティ構造を発見する。
///
/// lexicon JSON を再パースするのではなく、<c>LexiconGenerator</c> がすでに生成した型を
/// Compilation 上で読み取ることで、実際の生成結果と完全に同期させる。
/// </summary>
internal static class LexiconSymbolReader
{
    private const string LexiconAttributeMetadataName = "BlueBlaze.Core.LexiconAttribute";
    private const string JsonPropertyNameAttributeMetadataName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";

    internal static List<LexiconRequestInfo> FindRequests(Compilation compilation)
    {
        var results = new List<LexiconRequestInfo>();

        // Analyzer 自身は Core.dll を参照しないため、対象コンパイル側のシンボルを
        // メタデータ名(文字列)で解決する。型が見つからない = まだ Lexicon が生成されていない。
        var lexiconAttributeSymbol = compilation.GetTypeByMetadataName(LexiconAttributeMetadataName);
        if (lexiconAttributeSymbol is null)
        {
            return results;
        }

        var visitedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        VisitAssembly(compilation.Assembly, lexiconAttributeSymbol, visitedAssemblies, results);

        foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            VisitAssembly(referencedAssembly, lexiconAttributeSymbol, visitedAssemblies, results);
        }

        // 同じ NSID が複数アセンブリから見つかる場合がある(例: Client.Bluesky が
        // com/atproto を自己完結的に再生成しているため、推移参照先の Client.AtProtocol
        // からも同じ NSID が見つかる)。NSID 単位で重複を除去し、決定的な順序で1件だけ残す。
        var deduped = new Dictionary<string, LexiconRequestInfo>(StringComparer.Ordinal);
        foreach (var result in results)
        {
            if (!deduped.TryGetValue(result.Nsid, out var existing) ||
                ShouldPreferOver(result, existing))
            {
                deduped[result.Nsid] = result;
            }
        }

        return deduped.Values.ToList();
    }

    // 重複時にどちらを残すかを決定的に決める(アセンブリ名の辞書順が早い方を優先する)。
    private static bool ShouldPreferOver(LexiconRequestInfo candidate, LexiconRequestInfo existing)
    {
        var candidateAssembly = candidate.RequestType.ContainingAssembly.Name;
        var existingAssembly = existing.RequestType.ContainingAssembly.Name;
        return string.CompareOrdinal(candidateAssembly, existingAssembly) < 0;
    }

    private static void VisitAssembly(
        IAssemblySymbol assembly,
        INamedTypeSymbol lexiconAttributeSymbol,
        HashSet<IAssemblySymbol> visitedAssemblies,
        List<LexiconRequestInfo> results)
    {
        if (!visitedAssemblies.Add(assembly))
        {
            return;
        }

        VisitNamespace(assembly.GlobalNamespace, lexiconAttributeSymbol, results);
    }

    private static void VisitNamespace(
        INamespaceSymbol ns,
        INamedTypeSymbol lexiconAttributeSymbol,
        List<LexiconRequestInfo> results)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                VisitNamespace(childNs, lexiconAttributeSymbol, results);
            }
            else if (member is INamedTypeSymbol type)
            {
                VisitType(type, lexiconAttributeSymbol, results);
            }
        }
    }

    private static void VisitType(
        INamedTypeSymbol type,
        INamedTypeSymbol lexiconAttributeSymbol,
        List<LexiconRequestInfo> results)
    {
        // Request は常に何らかのコンテナ型(NSID の末尾セグメント)にネストされているため、
        // ネスト型も再帰的に辿る。
        foreach (var nested in type.GetTypeMembers())
        {
            VisitType(nested, lexiconAttributeSymbol, results);
        }

        AttributeData? attribute = null;
        foreach (var candidate in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, lexiconAttributeSymbol))
            {
                attribute = candidate;
                break;
            }
        }

        if (attribute is null || attribute.ConstructorArguments.Length < 2)
        {
            return;
        }

        if (attribute.ConstructorArguments[0].Value is not string nsid)
        {
            return;
        }

        // LexiconOperationKind.Query = 0, .Procedure = 1 (定義順)
        var isQuery = attribute.ConstructorArguments[1].Value is int kindValue && kindValue == 0;

        var container = type.ContainingType;
        if (container is null)
        {
            return;
        }

        var parametersType = container.GetTypeMembers("Parameters").FirstOrDefault();
        var inputType = container.GetTypeMembers("Input").FirstOrDefault();
        var outputType = container.GetTypeMembers("Output").FirstOrDefault();
        var deserializerType = container.GetTypeMembers("Deserializer").FirstOrDefault();

        results.Add(new LexiconRequestInfo(
            nsid,
            isQuery,
            type,
            container,
            parametersType,
            inputType,
            outputType,
            deserializerType));
    }

    /// <summary>
    /// Parameters/Input クラスの公開インスタンスプロパティを読み取り、CLI 生成に必要な情報に変換する。
    /// </summary>
    internal static List<LexiconPropertyInfo> ReadProperties(INamedTypeSymbol type, Compilation compilation)
    {
        var jsonPropertyNameAttributeSymbol = compilation.GetTypeByMetadataName(JsonPropertyNameAttributeMetadataName);

        // 生成済みクラスは required なプロパティのみをコンストラクタパラメータとして持つ
        // (ObjectClassEmitter.EmitConstructor / DocumentEmitter.EmitParametersClass)。
        // required プロパティが無い場合はコンストラクタ自体が生成されない(暗黙のパラメータレス
        // コンストラクタのみ)。パラメータ名は元の JSON key(camelCase)と一致する。
        var requiredParamNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ctor in type.Constructors)
        {
            foreach (var parameter in ctor.Parameters)
            {
                requiredParamNames.Add(parameter.Name);
            }
        }

        var result = new List<LexiconPropertyInfo>();

        foreach (var member in type.GetMembers())
        {
            if (member is not IPropertySymbol property ||
                property.DeclaredAccessibility != Accessibility.Public ||
                property.IsStatic)
            {
                continue;
            }

            // Parameters クラスには [JsonPropertyName] が付与されない(URL クエリ文字列のため)。
            // その場合は PascalCase 名の先頭文字を小文字化したものを使う。これは
            // LexiconNameHelper.ToPascalCase が単純な先頭大文字化のみを行うため、
            // ParametersEmitter/DocumentEmitter が生成するコンストラクタの引数名(元のJSON key)と一致する。
            var jsonName = ToCamelCase(property.Name);
            if (jsonPropertyNameAttributeSymbol is not null)
            {
                foreach (var attr in property.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, jsonPropertyNameAttributeSymbol) &&
                        attr.ConstructorArguments.Length == 1 &&
                        attr.ConstructorArguments[0].Value is string jn)
                    {
                        jsonName = jn;
                        break;
                    }
                }
            }

            var (isArray, elementType, isPropertyTypeNullable) = AnalyzeType(property.Type);
            var primitiveKind = ClassifyPrimitive(elementType);
            var isRequired = requiredParamNames.Contains(jsonName);

            result.Add(new LexiconPropertyInfo(
                property.Name,
                jsonName,
                isRequired,
                isPropertyTypeNullable,
                isArray,
                primitiveKind));
        }

        return result;
    }

    private static (bool IsArray, ITypeSymbol ElementType, bool IsNullable) AnalyzeType(ITypeSymbol type)
    {
        var isNullable = false;
        var workingType = type;

        if (workingType is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            // 値型の Nullable<T> (int?, bool? など)
            isNullable = true;
            workingType = namedType.TypeArguments[0];
        }
        else if (workingType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            // 参照型の nullable annotation (string? など)
            isNullable = true;
        }

        if (workingType is INamedTypeSymbol listType &&
            listType.Name == "IReadOnlyList" &&
            listType.TypeArguments.Length == 1 &&
            listType.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
        {
            return (true, listType.TypeArguments[0], isNullable);
        }

        return (false, workingType, isNullable);
    }

    private static string ToCamelCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase))
        {
            return pascalCase;
        }

        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }

    private static LexiconPrimitiveKind ClassifyPrimitive(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => LexiconPrimitiveKind.String,
            SpecialType.System_Int32 => LexiconPrimitiveKind.Int,
            SpecialType.System_Boolean => LexiconPrimitiveKind.Bool,
            _ => LexiconPrimitiveKind.NotPrimitive,
        };
    }
}
