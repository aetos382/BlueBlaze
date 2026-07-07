# Generators

このディレクトリにはコード ジェネレーターが置かれる。

- `CommandGenerator`: `Sources/Applications/Cli` で使用するコマンドを生成するプロジェクト
- `LexiconGenerator`: AtProtocol および Bluesky 用の Lexicon JSON から C# 用のコードを生成するプロジェクト
- `ResxSourceGenerator`: `*.resx` ファイルから型付けされたリソース文字列を取得するためのコードを生成するプロジェクト

`LexiconGenerator` 以外はシンプルな Roslyn Source Generator である。

## LexiconGenerator

JSON API の場合、NativeAOT 対応のため、[JsonSerializerContext](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonserializercontext) 派生クラスを生成したい。

しかし、Roslyn Source Generator の制約上、ある Source Generator が生成したコードを入力として別の Source Generator が動くことができない。

```cs
/*
FooRequest / FooResponse 自体は Roslyn Source Generator 問題なく生成されるが、以下のコードも Source Generator で生成すると、System.Text.Json 組み込みの Source Generator が動かない。
このコードを手書きしなければならないのでは片手落ちである。
*/

[JsonSerializable(typeof(FooRequest))]
[JsonSerializable(typeof(FooResponse))]
partial class LexiconSerializerContext : JsonSerializerContext;
```

そのため、`JsonSerializerContext` を生成するコードジェネレーターは Roslyn Source Generator とは異なる仕組みで `Compile` フェーズより前に生成を完了せねばならない。

そこで `LexiconGenerator` では、生成ロジックは `Core` としてアーキテクチャ中立にしつつ、Roslyn Source Generator の他に MSBuild Tasks としても動作できるようにしてある。このデザインは [CsWin32](https://github.com/microsoft/cswin32) プロジェクトを参考にしている。

各ジェネレーターは Roslyn Package としても配布されるが、`LexiconGenerator` に関しては、配布されるのは `Cli` および `Package` プロジェクトの生成物のみ。

なお、同ソリューション内の MSBuild Task をプロジェクト依存関係として直接参照するとビルド時にロックが発生してうまく動かないことが多いため、プロジェクト内での事前ソース生成には `Cli` プロジェクトをビルドして用いている。

その他のジェネレーターに関してはこのような要件がないため、シンプルな Roslyn Source Generator となっている。
