namespace BlueBlaze.CommandGenerator.Generation;

internal enum LexiconPrimitiveKind
{
    NotPrimitive,
    String,
    Int,
    Bool,
}

/// <summary>
/// Parameters/Input クラスの1プロパティに関する、CLI コマンド生成に必要な情報。
/// </summary>
/// <param name="CSharpName">生成済みクラスにおける C# プロパティ名(PascalCase)。</param>
/// <param name="JsonName">元の lexicon プロパティ名(camelCase)。CLI オプション名の kebab-case 化の元になる。</param>
/// <param name="IsRequired">
/// 生成済みコンストラクタの必須パラメータに対応するかどうか。
/// 注意: <c>LexiconGenerator.Core</c> の既存仕様では、required ではない値型(bool/int)プロパティでも
/// 明示的な default が無ければ <c>T?</c> ではなく <c>T</c> として生成される(<c>ObjectClassEmitter.ComputeType</c>)。
/// そのため「必須かどうか」はプロパティ型の Nullable Annotation では判定できず、
/// 実際のコンストラクタのパラメータ一覧(<see cref="LexiconSymbolReader.ReadProperties"/>)を見て判定する。
/// </param>
/// <param name="IsPropertyTypeNullable">
/// 実際の C# プロパティ型が Nullable(<c>T?</c>)かどうか。<see cref="IsRequired"/> が false でも、
/// 上記の理由で値型プロパティは non-nullable な <c>T</c> のままのことがあり、その場合は
/// optional な値を代入する際に <c>.Value</c> での unwrap が必要になる。
/// </param>
/// <param name="IsArray">配列(<c>IReadOnlyList&lt;T&gt;</c>)プロパティかどうか。</param>
/// <param name="PrimitiveKind">要素型(配列の場合は要素型)のプリミティブ種別。</param>
internal sealed record LexiconPropertyInfo(
    string CSharpName,
    string JsonName,
    bool IsRequired,
    bool IsPropertyTypeNullable,
    bool IsArray,
    LexiconPrimitiveKind PrimitiveKind)
{
    internal bool IsPrimitive => this.PrimitiveKind != LexiconPrimitiveKind.NotPrimitive;
}
