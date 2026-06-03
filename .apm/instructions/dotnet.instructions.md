---
description: .csproj ファイルに関するルール
applyTo: '**/*.csproj,Directory.Packages.props'
---

# .csproj ファイルに関するルール

## `dotnet` コマンドの使用
- プロジェクトの操作は可能な限り適切な `dotnet` コマンドを通じて行う。
  - プロジェクトの作成は `dotnet new` コマンドおよび `dotnet solution add` コマンドを使用する。
  - プロジェクトへの NuGet パッケージ参照の追加は `dotnet package add` コマンド、削除は `dotnet package remove` コマンドを使用する。
  - プロジェクトへのプロジェクト参照の追加は `dotnet reference add` コマンド、削除は `dotnet reference remove` コマンドを使用する。
- これらのコマンドを使用することで達成できる目標については、*.csproj ファイルや Directory.Packages.props ファイルを直接編集してはならない。

## `dotnet` コマンドで達成できない目標

- `dotnet` コマンドがサポートしていない操作に関しては、ファイルを直接変更する。
- `dotnet package remove` コマンドでパッケージ参照を削除した際、そのパッケージをソリューション内の他のプロジェクトで使用していない場合は、`Directory.Packages.props` ファイルからも削除する。
