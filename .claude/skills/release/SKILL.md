---
name: release
description: バージョン番号を指定して GitHub Actions のリリース ワークフローをトリガーする
disable-model-invocation: true
---

引数として渡されたバージョン番号 `{args}` を使い、以下の手順でリリースを実行する。

1. バージョン形式を検証する。`X.Y.Z` または `X.Y.Z-suffix` の形式でなければエラーを表示して終了する。
2. `git branch --show-current` で現在のブランチを確認し、`main` でなければ警告を表示してユーザーに続行するか確認する。
3. ユーザーに「バージョン `{args}` でリリース ワークフローをトリガーしてよいか」を確認する。
4. `gh workflow run release.yml --field version={args}` を実行する。
5. `gh run list --workflow=release.yml --limit 1` でワークフローの開始を確認し、URL を表示する。
