mkdir -p ~/.claude
ln -s "$(pwd)/.devcontainer/claude-template.md" ~/.claude/CLAUDE.md

mkdir -p ~/.local/share/bash-completion/completions
docker completion bash --no-descriptions > ~/.local/share/bash-completion/completions/docker
npm completion > ~/.local/share/bash-completion/completions/npm

pwsh -File setup.ps1 -SkipMcpConfig
