# CLAUDE.md

## GitHub コンテンツの取得・検索
- GitHub 上のファイルを読み取る際は、GitHub MCP サーバーの `get_file_contents` ツールを使うこと。`WebFetch` や `gh api` は使わないこと。
- GitHub 上の issue の検索には `gh search issues` コマンド、または、GitHub MCP サーバーの `search_issues` ツールや `issue_read` ツールを使うこと。

## git hook について
`.devcontainer/postCreate.sh` で `.hooks/hooks.gitconfig` が git 設定に読み込まれるようになっています。
この設定は git 2.54 で追加された [config based hook](https://git-scm.com/docs/git-hook/2.54.0) です。
未対応の形式ではなくて、ちゃんと動いていますので、この設定自体は疑わないでください。

## 即時実行コマンドについて
Python はインストールされていません。
スクリプトを生成して実行する場合は、シェルスクリプト、JavaScript、C#、PowerShell のいずれかを使用してください。

## .NET プロジェクトのテストに関するルール

- C# コードのテストに関しては以下のように実行する。
- ソリューション全体のテストを実行する場合、ソリューション ディレクトリで `dotnet test` コマンドを使用する。
- 特定のテスト プロジェクトのテストを実行する場合、当該テスト プロジェクト ディレクトリで `dotnet run` コマンドを使用する。
- 特定のテストケースを実行する場合、当該テスト プロジェクト ディレクトリで `dotnet test --filter FullyQualifiedName={テスト メソッドの完全修飾名}` コマンドを使用する。

## ドキュメントの検索結果について

- Web 検索等で見つけた情報を提示する際は、必ず出典となる URL を併記してください。
