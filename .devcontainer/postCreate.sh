mkdir -p ~/.claude
ln -s "$(pwd)/.devcontainer/claude-template.md" ~/.claude/CLAUDE.md

mkdir -p ~/.local/share/bash-completion/completions
docker completion bash --no-descriptions > ~/.local/share/bash-completion/completions/docker
npm completion > ~/.local/share/bash-completion/completions/npm

# .devcontainer.json の features に書いても clone するだけで install してくれない
gh extension install github/gh-aw

pwsh -File setup.ps1 -SkipMcpConfig
