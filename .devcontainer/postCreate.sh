git submodule update --init --recursive

git config --local include.path ../.hooks/hooks.gitconfig

dotnet tool restore

apm install
