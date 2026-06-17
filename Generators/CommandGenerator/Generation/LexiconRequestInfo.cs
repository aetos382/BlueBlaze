using Microsoft.CodeAnalysis;

namespace BlueBlaze.CommandGenerator.Generation;

/// <summary>
/// <c>[Lexicon(nsid, kind)]</c> 属性が付与された Request クラスと、
/// 同じコンテナ内に存在する関連クラスのシンボル情報。
/// </summary>
internal sealed record LexiconRequestInfo(
    string Nsid,
    bool IsQuery,
    INamedTypeSymbol RequestType,
    INamedTypeSymbol ContainerType,
    INamedTypeSymbol? ParametersType,
    INamedTypeSymbol? InputType,
    INamedTypeSymbol? OutputType,
    INamedTypeSymbol? DeserializerType);
