mkdir -p ~/.local/share/bash-completion/completions
docker completion bash --no-descriptions > ~/.local/share/bash-completion/completions/docker
npm completion > ~/.local/share/bash-completion/completions/npm

git config --local include.path ../.git-hooks/hooks.gitconfig
git submodule update --init --recursive

dotnet tool restore

GITHUB_PERSONAL_ACCESS_TOKEN="${GITHUB_PERSONAL_ACCESS_TOKEN:-${GH_PAT_FOR_MCP_SERVER}}"

if [ -n "${GITHUB_PERSONAL_ACCESS_TOKEN// /}" ]; then
  github_mcp_json=$(printf '{"type":"http","url":"https://api.githubcopilot.com/mcp","headers":{"Authorization":"Bearer %s"}}' "$GITHUB_PERSONAL_ACCESS_TOKEN")
  claude mcp remove github --scope user 2>/dev/null || true
  claude mcp add-json github "$github_mcp_json" --scope user
fi
