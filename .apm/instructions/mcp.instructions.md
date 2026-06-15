---
applyTo: 'apm.yml, apm.lock.yaml, .mcp.json'
---

# MCP に関するルール
- プロジェクトに MCP サーバーを追加する際は、`apm` CLI を使用する。`apm.yaml`, `apm.lock.yaml`, `.mcp.json` を直接編集してはならない。
- プロジェクトから MCP サーバーを削除するには、`apm` CLI がアンインストールを現状サポートしていないので、`apm.yaml`, `apm.lock.yaml`, `.mcp.json` を直接編集する。
- プロジェクトに既に構成されている MCP サーバーを再インストールする際は `apm --frozen` コマンドを使用する。
