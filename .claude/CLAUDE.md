# CLAUDE.md

## GitHub コンテンツの取得・検索

- GitHub 上のファイルを読み取る際は、GitHub MCP サーバーの `get_file_contents` ツールを使う。`WebFetch` や `gh api` は使わない。
- GitHub 上の issue の検索には `gh search issues` コマンド、または、GitHub MCP サーバーの `search_issues` ツールや `issue_read` ツールを使う。

## Pull Request の作成

- Renovate が作成した issue や PR を `close #xx` / `fix #xx` 等のキーワードで参照しない。

## 即時実行コマンド

Python は `python` も `python3` も禁止。
スクリプトを生成して実行する場合は、以下のいずれかのうち、現在の環境で使用可能なものを使用する。
- PowerShell
- C# (dotnet run)
- JavaScript (node.js)
- bash

## git hook

`.devcontainer/postCreate.sh` で `.hooks/hooks.gitconfig` が git 設定に読み込まれるようになっています。
この設定は git 2.54 で追加された [config based hook](https://git-scm.com/docs/git-hook/2.54.0) です。
未対応の形式ではなくて、ちゃんと動いていますので、この設定自体は疑わないでください。

## .NET プロジェクトのテスト

- C# コードのテストに関しては以下のように実行する。
- ソリューション全体のテストを実行する場合、ソリューション ディレクトリで `dotnet test` コマンドを使用する。
- 特定のテスト プロジェクトのテストを実行する場合、当該テスト プロジェクト ディレクトリで `dotnet test` または `dotnet run` コマンドを使用する。
- 特定のテストケースを実行する場合、当該テスト プロジェクト ディレクトリで `dotnet test --filter FullyQualifiedName={テスト メソッドの完全修飾名}` コマンドを使用する。
