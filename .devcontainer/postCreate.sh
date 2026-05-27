git submodule update --init --recursive

git config --local include.path "${WORKSPACE}/.gitconfig"

go install github.com/bluesky-social/goat@latest

