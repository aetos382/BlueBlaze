# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Tool usage

### GitHub ファイルの取得

GitHub 上のファイルを読み取る際は、`WebFetch` や `gh api` ではなく GitHub MCP サーバーの `get_file_contents` ツールを使うこと。

- `gh api` は書き込みも可能な低レベルコマンドのため、読み取りには使わない
- `mcp__plugin_github_github__get_file_contents` を使う（プライベートリポジトリも対応）
