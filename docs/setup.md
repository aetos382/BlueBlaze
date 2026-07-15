# 開発環境のセットアップ

## MCP サーバーについて

nuget・context7・GitHub MCP Server (`https://api.githubcopilot.com/mcp`) は project scope (`.mcp.json`) ではなく **user scope** で登録する運用です。1度登録すれば、このマシン上のどのプロジェクトを開いても有効になります。

devcontainer/GitHub Codespaces ではコンテナ作成時 (`.devcontainer/postCreate.sh`) に自動で user scope 登録されます。GitHub MCP Server は OAuth (GitHub OAuth App) 方式のため、コンテナ作成後に一度だけ次のコマンドで認可してください。

```bash
claude mcp login github
# ブラウザが開けないヘッドレス環境の場合
claude mcp login github --no-browser
```

認可情報は `~/.claude` (`claude-code-config-${devcontainerId}` ボリューム)に保存されるため、同じコンテナ/Codespace であればコンテナの再作成 (Rebuild) を跨いでも再認可は不要です。

GitHub MCP Server の OAuth client secret は 1Password の Automation vault (`op://Automation/GitHub MCP Server OAuth Client Secret/credential`) から `postCreate.sh` 実行時に取得します。devcontainer/Codespaces で `op` を非対話的に認証するには `OP_SERVICE_ACCOUNT_TOKEN` が必要です（ローテーションしない 1Password Service Account トークンのため、client secret 自体を各環境に配布する必要はありません）。

ローカルホスト(devcontainer を使わない直接開発)では、この自動登録の対象外です。初回のみ手動で以下を実行してください(以後はこのマシン上のどのプロジェクトでも有効です)。

```bash
claude mcp add --scope user nuget -- dnx NuGet.Mcp.Server --yes
claude mcp add --scope user context7 -- npx -y @upstash/context7-mcp
claude mcp add --transport http --scope user --client-id Ov23li60UN7SPpfSmeTX --client-secret --callback-port 8080 github https://api.githubcopilot.com/mcp
claude mcp login github
```

## GitHub Codespaces

[Codespaces シークレット](https://github.com/settings/codespaces) に以下を設定してください。

| シークレット名 | 説明 |
|---|---|
| `OP_SERVICE_ACCOUNT_TOKEN` | 1Password Service Account トークン（該当 Vault への読み取り権限のみ付与）。 |

## ローカル DevContainer

**前提条件**

- Docker Desktop（Linux の場合は Docker Engine）
- VS Code + [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) 拡張機能

**環境変数（ホストマシン）**

| 変数名 | 説明 |
|---|---|
| `OP_SERVICE_ACCOUNT_TOKEN` | 1Password Service Account トークン（該当 Vault への読み取り権限のみ付与）。 |

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
git config --local include.path ../.git-hooks/hooks.gitconfig
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

## Windows ホスト上での直接開発

**前提条件**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（バージョンは [global.json](../global.json) 参照）
- [Node.js](https://nodejs.org/) LTS 偶数バージョン（バージョンは [.devcontainer/Dockerfile](../.devcontainer/Dockerfile) の `NODE_VERSION` 参照）
- [Git for Windows](https://gitforwindows.org/)
- [PowerShell 7.x](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows)

**セットアップ**

```powershell
git submodule update --init --recursive
git config --local include.path ../.git-hooks/hooks.gitconfig
dotnet tool restore
npm ci
```

MCP サーバーの登録については上記「MCP サーバーについて」の「ローカルホスト」の手順を参照してください。
