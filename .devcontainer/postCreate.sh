mkdir -p ~/.local/share/bash-completion/completions
docker completion bash --no-descriptions > ~/.local/share/bash-completion/completions/docker
npm completion > ~/.local/share/bash-completion/completions/npm

git config --local include.path ../.hooks/hooks.gitconfig
git submodule update --init --recursive

dotnet tool restore

node .devcontainer/setup-claude-plugins.mts
