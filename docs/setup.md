# 開発環境のセットアップ

## GitHub Codespaces

[Codespaces シークレット](https://github.com/settings/codespaces) に以下を設定してください。

| シークレット名 | 説明 |
|---|---|
| `GH_PAT_FOR_MCP_SERVER` | GitHub MCP サーバー認証用 PAT。コンテナ内では `GITHUB_PERSONAL_ACCESS_TOKEN` にマップされます。 |

> [!NOTE]
> Codespaces シークレットには `GITHUB_` で始まる名前を登録できないため、専用の名前を使用しています。

## ローカル DevContainer

**前提条件**

- Docker Desktop（Linux の場合は Docker Engine）
- VS Code + [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) 拡張機能

**環境変数（ホストマシン）**

| 変数名 | 説明 |
|---|---|
| `GH_PAT_FOR_MCP_SERVER` | GitHub MCP サーバー認証用 PAT。コンテナ内では `GITHUB_PERSONAL_ACCESS_TOKEN` にマップされます。 |

**注意事項**

- **ARM ホスト（Apple Silicon Mac など）：** Dockerfile は `x86_64` バイナリ（Node.js・goat・apm）をダウンロードするため、Docker Desktop の Rosetta エミュレーションが必要です。動作は未確認です。
- **APT ミラー：** Dockerfile のビルド時に日本のミラー（`ftp.udx.icscoe.jp`）を使用します。海外ネットワーク環境ではビルドが遅くなる場合があります。

## Linux ホスト上での直接開発

**前提条件**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（バージョンは [global.json](../global.json) 参照）
- [Node.js](https://nodejs.org/) LTS 偶数バージョン（バージョンは [.devcontainer/Dockerfile](../.devcontainer/Dockerfile) の `NODE_VERSION` 参照）
- Git

**セットアップ**

```bash
git submodule update --init --recursive
git config --local include.path ../.hooks/hooks.gitconfig
dotnet tool restore
npm ci
```

タブ補完を有効にする場合（任意）:

```bash
mkdir -p ~/.local/share/bash-completion/completions
node --completion-bash > ~/.local/share/bash-completion/completions/node
docker completion bash > ~/.local/share/bash-completion/completions/docker
npm completion > ~/.local/share/bash-completion/completions/npm
```

**環境変数**

| 変数名 | 説明 |
|---|---|
| `GITHUB_PERSONAL_ACCESS_TOKEN` | GitHub MCP サーバー認証用 PAT。 |

## Windows ホスト上での直接開発

**前提条件**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（バージョンは [global.json](../global.json) 参照）
- [Node.js](https://nodejs.org/) LTS 偶数バージョン（バージョンは [.devcontainer/Dockerfile](../.devcontainer/Dockerfile) の `NODE_VERSION` 参照）
- [Git for Windows](https://gitforwindows.org/)
- [PowerShell 7.x](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows)

**セットアップ**

```powershell
git submodule update --init --recursive
git config --local include.path ../.hooks/hooks.gitconfig
dotnet tool restore
npm ci
```

**環境変数**

| 変数名 | 説明 |
|---|---|
| `GITHUB_PERSONAL_ACCESS_TOKEN` | GitHub MCP サーバー認証用 PAT。 |
