set -e

mkdir -p ~/.claude
ln -sf "$(pwd)/.devcontainer/claude-template.md" ~/.claude/CLAUDE.md

mkdir -p ~/.local/share/bash-completion/completions
docker completion bash --no-descriptions > ~/.local/share/bash-completion/completions/docker
npm completion > ~/.local/share/bash-completion/completions/npm

# .devcontainer.json の features に書いても clone するだけで install してくれない
gh extension install github/gh-aw || gh extension upgrade github/gh-aw

if ! pwsh -File setup.ps1; then
  echo "ERROR: setup.ps1 が失敗しました。" >&2
  exit 1
fi

# MCP サーバーは project scope (.mcp.json) ではなく user scope で登録する。
# devcontainer/Codespaces では clone のたびに再登録が必要なため、ここで行う。
claude mcp remove github --scope user 2>/dev/null || true
claude mcp remove nuget --scope user 2>/dev/null || true
claude mcp remove context7 --scope user 2>/dev/null || true

failed=()

if ! claude mcp add --scope user nuget -- dnx NuGet.Mcp.Server --yes; then
  failed+=("nuget")
fi

if ! claude mcp add --scope user context7 -- npx -y @upstash/context7-mcp; then
  failed+=("context7")
fi

# client secret は claude mcp add 実行時にしか使わないため、コンテナ全体に
# 永続化する環境変数にはせず、この登録処理の実行中だけプロセスローカルに設定する。
if ! MCP_CLIENT_SECRET="$(op read 'op://Automation/GitHub MCP Server OAuth Client Secret/credential' --no-newline)" || [ -z "$MCP_CLIENT_SECRET" ]; then
  echo "ERROR: 1Password から GitHub MCP Server の Client Secret 取得に失敗しました。" >&2
  failed+=("github")
else
  export MCP_CLIENT_SECRET
  if ! claude mcp add --transport http --scope user --client-id Ov23li60UN7SPpfSmeTX --client-secret --callback-port 8080 github https://api.githubcopilot.com/mcp; then
    failed+=("github")
  fi
  unset MCP_CLIENT_SECRET
fi

if [ "${#failed[@]}" -gt 0 ]; then
  echo "ERROR: 以下の MCP サーバー登録に失敗しました: ${failed[*]}" >&2
  exit 1
fi

echo "MCP サーバーを登録しました。GitHub MCP Server の認可がまだの場合は 'claude mcp login github' (ヘッドレス環境では --no-browser) を実行してください。"
