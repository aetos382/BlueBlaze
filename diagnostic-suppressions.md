# 診断抑制メモ

フレームワークまたはアナライザーの制約により `#pragma warning disable` で抑制している診断の一覧。
各項目には issue URL と解除条件を記載しているので、折に触れて確認すること。

## CA2227 — コレクション プロパティは読み取り専用にする必要があります

| ファイル | 対象メンバー |
|---|---|
| [Generators/Generators.Core/LexiconDocument.cs](Generators/Generators.Core/LexiconDocument.cs) | `LexiconDocument.ExtensionData` 他 6 プロパティ |

**理由**: `[JsonExtensionData]` プロパティは Source Generated Serializer でデシリアライズするために
`set` アクセサーが必要。`RespectRequiredConstructorParameters = true` と `init` の組み合わせが
非対応のため `set` のままにしている。

**Issue**: <https://github.com/dotnet/runtime/issues/107898>

**解除条件**: dotnet/runtime PR #124650 の修正が含まれる **.NET 11 以降**をターゲットにしたら、
`set` を `init` に変更して各プロパティの `#pragma warning disable CA2227` を削除できる。

---

## IDE0051 — 使用されていないプライベート メンバーを削除する

| ファイル | 対象メンバー |
|---|---|
| [Polyfills/ArgumentNullExceptionExtensions.cs](Polyfills/ArgumentNullExceptionExtensions.cs) | `ArgumentNullExceptionExtensions.Throw` |
| [Polyfills/ArgumentOutOfRangeExceptionExtensions.cs](Polyfills/ArgumentOutOfRangeExceptionExtensions.cs) | `ArgumentOutOfRangeExceptionExtensions.ThrowIf` |

**理由**: C# 14 `extension` ブロック内から呼び出されるプライベートメンバーが、
Roslyn のアナライザーに未使用と誤検知される。

**Issue**: <https://github.com/dotnet/roslyn/issues/82691>

**解除条件**: Roslyn が修正されたら `#pragma warning disable IDE0051` を削除できる。
修正が取り込まれたバージョンの Roslyn / Visual Studio / .NET SDK にアップグレードした後に確認すること。
