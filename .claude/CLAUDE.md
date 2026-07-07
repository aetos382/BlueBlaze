# CLAUDE.md

## Pull Request の作成

- Renovate が作成した issue や PR を `close #xx` / `fix #xx` 等のキーワードで参照しない。

## 即時実行コマンド

Python は `python` も `python3` も禁止。
スクリプトを生成して実行する場合は、以下のいずれかのうち、現在の環境で使用可能なものを使用する。
- PowerShell
- C# (dotnet run)
- JavaScript (node.js)
- bash

## git hook

`.devcontainer/postCreate.sh` で `.gitconfig` が git 設定に読み込まれるようになっている。
この設定は git 2.54 で追加された [config based hook](https://git-scm.com/docs/git-hook/2.54.0) 。
未対応の形式ではなく、ちゃんと動いてるので、この設定自体は疑わない。
