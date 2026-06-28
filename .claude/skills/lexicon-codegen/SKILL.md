---
name: lexicon-codegen
description: AT Protocol の Lexicon JSON ファイルから C# コードを生成する手順
---

## LexiconGenerator の使い方

Lexicon JSON ファイルは `submodules/atproto/lexicons/` 以下にある。

### アドホック実行（特定ファイルを対象に生成）

`dotnet run` で直接 CLI を起動できる。

```
dotnet run --project Generators/LexiconGenerator/Cli/LexiconGenerator.Cli.csproj -- \
  <input> \
  --output <出力ディレクトリ> \
  --namespace <名前空間>
```

主なオプション:

| オプション | 説明 |
|---|---|
| `<input>` | Lexicon JSON ファイル、ディレクトリ、またはグロブパターン（複数指定可） |
| `--output` / `-o` | 生成コードの出力先ディレクトリ（必須） |
| `--namespace` / `-n` | 生成コードの名前空間（必須） |
| `--generate-type-info` | `JsonTypeInfo` ベースのデシリアライザーを生成（AOT 対応） |
| `--target-framework` / `-f` | ターゲット フレームワーク（例: `net10.0`） |
| `--emit-metadata-attributes` | BlueBlaze.Core のメタデータ属性を出力 |

### ソリューション全体のビルド（コード生成込み）

通常の `dotnet build` は `BlueBlazeGeneratorCliExe` プロパティが未設定だとコード生成をスキップする。
ローカルでコード生成込みのビルドを行うには、まず CLI を `Temp/bbgen` に発行してからビルドする。

```
dotnet publish Generators/LexiconGenerator/Cli/LexiconGenerator.Cli.csproj -o Temp/bbgen
dotnet build BlueBlaze.slnx -p:BlueBlazeGeneratorCliExe=Temp/bbgen/BlueBlaze.LexiconGenerator.Cli
```

`Temp/` は `.gitignore` に含まれているのでコミットされない。
