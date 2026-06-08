git submodule update --init --recursive

git config --local include.path "${WORKSPACE}/.hooks/hooks.gitconfig"

dotnet tool restore
