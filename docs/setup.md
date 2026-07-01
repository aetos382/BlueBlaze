# 開発環境のセットアップ

## GitHub Copilot MCP サーバー認証用 PAT について

GitHub Copilot MCP サーバー認証用の PAT は 1Password に保存し、コンテナ起動のたびに 1Password CLI (`op`) 経由で動的に取得します（`.devcontainer/postStart.sh` → `Register-GitHubCopilotMcp.ps1`）。参照先は `op://Private/BlueBlaze GitHub PAT/credential` を前提としているため、実際の Vault・アイテム名に合わせて `postStart.sh` を調整してください。

各環境で保持するのは PAT 本体ではなく、ローテーションしない **1Password Service Account トークン** (`OP_SERVICE_ACCOUNT_TOKEN`) のみです。PAT を再発行した場合は 1Password 側のアイテムを更新するだけで、以下のどの環境も自動的に最新の PAT を使うようになります。

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
git config --local include.path ../.git-hooks/hooks.gitconfig
dotnet tool restore
npm ci
```

**GitHub PAT の自動更新（マシン固有設定）**

コンテナ経由の環境と異なり、Windows ベアメタルにはコンテナ起動のようなフックがないため、代わりに **OS ログオン時にタスク スケジューラーで PAT を更新する**運用にします。この設定はマシン固有のグローバル設定であり、リポジトリでは管理しません。各自のマシンで以下のように設定してください。

1. [1Password CLI](https://developer.1password.com/docs/cli/get-started/) をインストールし、`OP_SERVICE_ACCOUNT_TOKEN` をユーザー環境変数として設定する。
2. `op read` で PAT を取得し、`GITHUB_PERSONAL_ACCESS_TOKEN` をユーザー環境変数として永続化するスクリプトを用意する（例）。
   ```powershell
   $pat = op read 'op://Private/BlueBlaze GitHub PAT/credential' --no-newline
   [Environment]::SetEnvironmentVariable('GITHUB_PERSONAL_ACCESS_TOKEN', $pat, 'User')
   ```
3. 上記スクリプトを「ログオン時」トリガーでタスク スケジューラーに登録する（例）。
   ```powershell
   $action = New-ScheduledTaskAction -Execute 'pwsh.exe' -Argument '-File "C:\path\to\Update-GitHubPatEnv.ps1"'
   $trigger = New-ScheduledTaskTrigger -AtLogOn
   Register-ScheduledTask -TaskName 'Update GitHub PAT (BlueBlaze)' -Action $action -Trigger $trigger
   ```

これにより、`setup.ps1` を実行する時点では `GITHUB_PERSONAL_ACCESS_TOKEN` が直近のログオン時点の最新の値に更新済みの状態になります。
