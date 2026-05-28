# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Tool usage

### GitHub コンテンツの取得・検索

- GitHub 上のファイルを読み取る際は、GitHub MCP サーバーの `get_file_contents` ツールを使うこと。`WebFetch` や `gh api` は使わないこと。
- GitHub 上の issue の検索には `gh search issues` コマンド、または、GitHub MCP サーバーの `search_issues` ツールや `issue_read` ツールを使うこと。
