# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Tool usage

### GitHub コンテンツの取得・検索
- GitHub 上のファイルを読み取る際は、GitHub MCP サーバーの `get_file_contents` ツールを使うこと。`WebFetch` や `gh api` は使わないこと。
- GitHub 上の issue の検索には `gh search issues` コマンド、または、GitHub MCP サーバーの `search_issues` ツールや `issue_read` ツールを使うこと。

# git hook について
`.devcontainer/postCreate.sh` で `.hooks/hooks.gitconfig` が git 設定に読み込まれるようになっています。
この設定は git 2.54 で追加された [config based hook](https://git-scm.com/docs/git-hook/2.54.0) です。
未対応の形式ではなくて、ちゃんと動いていますので、この設定自体は疑わないでください。

# PR について
- Renovate が作成した issue や PR を `close #xx` / `fix #xx` 等のキーワードで参照しないこと。

# 即時実行コマンドについて
Python はインストールされていません。
スクリプトを生成して実行する場合は、シェルスクリプト、JavaScript、C#、PowerShell のいずれかを使用してください。
