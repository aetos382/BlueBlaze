---
description: >-
  "Test" ワークフローが失敗した際に、MSBuild binlog を binlog-mcp サーバー経由で解析し、
  根本原因を PR にコメントする。ビルドが成功している間はトリガーされない。

on:
  workflow_run:
    workflows: ["Test"]
    types: [completed]
    # branches を指定すると「PR 元ブランチ(feature ブランチ)」での失敗に反応しなくなり、
    # PR へのコメントという目的を果たせなくなるため、意図的に指定しない
    # (gh-aw compile 時に出るセキュリティ/パフォーマンス警告は許容する)。

# Test ワークフローが失敗(または cancelled)で完了した場合のみエージェントを起動する。
if: github.event.workflow_run.conclusion == 'failure' || github.event.workflow_run.conclusion == 'cancelled'

engine: copilot

timeout-minutes: 15

permissions:
  actions: read
  contents: read
  pull-requests: read

# Test ワークフロー(test.yml)がアップロードした test-binlog artifact を
# /tmp/test.binlog にダウンロードし、binlog-mcp コンテナへ読み取り専用でマウントする。
mcp-servers:
  binlog-mcp:
    container: "mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64"
    mounts:
      - "/tmp/test.binlog:/data/test.binlog:ro"
    allowed: ["*"]

steps:
  - name: Download binlog artifact
    uses: actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c # v8.0.1
    continue-on-error: true
    with:
      name: test-binlog
      run-id: ${{ github.event.workflow_run.id }}
      github-token: ${{ secrets.GITHUB_TOKEN }}
      path: /tmp/download

  - name: Stage binlog for MCP mount
    run: |
      set -euo pipefail
      if [ -f /tmp/download/test.binlog ]; then
        cp /tmp/download/test.binlog /tmp/test.binlog
        echo "GH_AW_BINLOG_FOUND=true" >> "$GITHUB_ENV"
      else
        echo "GH_AW_BINLOG_FOUND=false" >> "$GITHUB_ENV"
      fi

  - name: Resolve associated PR number
    # workflow_run イベントのペイロードに含まれる pull_requests は、
    # 許可された式リストに無いため本文中で直接参照できない。
    # ここで解決して env 経由(許可済み)で渡す。
    env:
      GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      REPO: ${{ github.repository }}
      HEAD_SHA: ${{ github.event.workflow_run.head_sha }}
    run: |
      set -euo pipefail
      PR_NUMBER=$(jq -r '.workflow_run.pull_requests[0].number // empty' "$GITHUB_EVENT_PATH")
      if [ -z "$PR_NUMBER" ]; then
        PR_NUMBER=$(gh api "repos/$REPO/commits/$HEAD_SHA/pulls" \
          -H "Accept: application/vnd.github.groot-preview+json" \
          --jq '.[0].number // empty' 2>/dev/null || true)
      fi
      echo "GH_AW_PR_NUMBER=${PR_NUMBER}" >> "$GITHUB_ENV"

tools:
  github:
    toolsets: [pull_requests]

safe-outputs:
  add-comment:
    max: 1
    hide-older-comments: true
  noop:
    max: 1
---

# ビルド失敗診断

"Test" ワークフローが失敗しました。

- Run: [${{ github.event.workflow_run.id }}](${{ github.event.workflow_run.html_url }})
- Head SHA: `${{ github.event.workflow_run.head_sha }}`
- Trigger event: ${{ github.event.workflow_run.event }}

## 手順

1. `env.GH_AW_BINLOG_FOUND` が `false` の場合、binlog を取得できなかったことを示す短い `noop` を呼んで終了する。
2. binlog が取得できた場合、`binlog-mcp` の `binlog_*` ツールを使って `/data/test.binlog` を解析する。
   すべての `binlog_*` ツール呼び出しで `binlog_file` 引数に `/data/test.binlog` を渡すこと。
   - まず `binlog_overview` で全体状況を把握する。
   - エラーがあれば `binlog_errors` で詳細を取得する。
   - 必要に応じて `binlog_explain_property` や `binlog_imports` などで原因を深掘りする。
3. 根本原因(コンパイルエラー / NuGet 復元失敗 / MSBuild ターゲット失敗など)を特定する。
   - binlog は正常なのにジョブが失敗している場合(=テストのアサーション失敗など、ビルド自体は成功している場合)は、
     その旨を明記し、binlog による原因特定はできない範囲であることを断る。

{{#if env.GH_AW_PR_NUMBER}}
4. 対象 PR #${{ env.GH_AW_PR_NUMBER }} に、根本原因と修正案を日本語でまとめて `add_comment` でコメントする。
{{else}}
4. このワークフロー実行に紐づく PR が見つからない(push トリガー等)。分析結果を `noop` のメッセージとして出力するだけに留める。
{{/if}}

コメントは以下の構成にすること:

```markdown
### 🔧 ビルド失敗診断

**対象コミット**: `${{ github.event.workflow_run.head_sha }}`

#### 根本原因
<!-- 特定した原因 -->

#### 推奨される対応
<!-- 具体的な修正案。ファイルパスと行番号を可能な限り含める -->

<details>
<summary>解析手順</summary>
<!-- 使用したツールと確認した内容の要約 -->
</details>
```

**重要**: 必ず `add_comment` または `noop` のいずれかを呼んで終了すること。
