# atproto lexicon 生成: `.g.cs` 並列書き込み競合の調査メモ

作業ブランチ: `feat/atproto-lexicon-tag-tracking`
ベースコミット: `803d73f`(submodule 廃止 + external/atproto タグ追従の第1弾)

## 症状(未解決)

- 完全クリーン → 初回ビルドが **非決定的に失敗**(前回セッションで 6 回中 2〜4 回)。
- エラー: 生成器 Exec で
  `System.IO.IOException: The process cannot access the file '...<Name>.Input.g.cs' because it is being used by another process`
  (binlog の `_BBCliGenerate/Exec` 下)。
- 単一 TF / マルチ TF どちらでも発生。lexicon 取得直後の初回が危険、再ビルドは成功しやすい。

## 前回までに切り分け済みの事実

- `_BBCliGenerate` は target_reasons 上「1 execution」(二重起動ではないと MSBuild は言う)。
- 生成器(`GenerateHandler.cs`)の書き込みは `foreach` 逐次で並列なし。input-files.txt は 95 行・重複なし。
- `-m:1`(MSBuild 並列無効)で **むしろ悪化**(6/6 近く失敗)。並列は原因ではない。
- `-p:UseSharedCompilation=false`(VBCSCompiler オフ)でも消えない。
- run 間に `sleep 2` を挟んでも消えない。

## 今回セッションの検証(NEW)

**目的**: 競合が (a) 今回の変更由来か (b) submodule 時代(`803d73f` 以前)からの潜在バグか。

**方法**: `803d73f` を detached worktree(`../BlueBlaze-verify803`)にチェックアウトし、
working tree から lexicons をコピー、bbgen(Cli)exe をビルドして用意。
「AtProtocol/Bluesky の obj/bin を削除 → 初回ビルド」を 6 回繰り返す
(スクリプト: worktree 内 `verify-race.sh`、binlog は `verify-logs/`)。

**結果**: **6/6 成功。競合は一度も再現せず。**

### 起動経路の決定的な差(803d73f vs working tree)

| | 803d73f | working tree(未コミット) |
|---|---|---|
| lexicon 取得 | ビルド **外**(`.devcontainer/updateContent.sh` 等) | ビルド **内**に統合(`_FetchAtprotoLexiconCore`) |
| LexiconDocument | csproj トップレベルに glob 直書き | `AtprotoLexiconPatterns`(文字列)→ `PrepareAtprotoLexicon` がターゲット内 ItemGroup で glob 展開 |
| `_BBCliGenerate` 依存 | `_BBCliSetup;_BBCliWriteArgs` のみ | `$(_BBCliGenerateDependsOn)`(= `PrepareAtprotoLexicon`)を先頭に注入 |

→ **803d73f で再現しない以上、競合の引き金は今回追加した `PrepareAtprotoLexicon` 方式が濃厚**
(`_BBCliGenerate` の Inputs `@(LexiconDocument)` を依存ターゲットが実行時に書き換えるタイミング絡み)。

### この検証の限界(次セッションで潰すべき条件差)

今回の 6/6 成功は「今回変更が原因」を**強く示唆するが確定ではない**。以下が同条件でない:

1. **ビルド対象**: 検証は `Client.Bluesky.csproj`(と依存で AtProtocol)。前回競合は AtProtocol/Bluesky を
   ソリューション一括ビルドで観測。**ソリューション全体(`BlueBlaze.slnx`)一括**で 803d73f を回すべき。
2. **fetch のタイミング**: 803d73f は fetch がビルド外なので「lexicon 取得直後」の状態を再現できていない。
   working tree 側は取得とビルドが同一 MSBuild 実行。→ 引き金が「取得直後」なら 803d73f では原理的に出ない。
3. 試行 6 回は前回の再現率(1/3〜2/3)なら十分検出できる回数だが、低確率化している可能性は残る。

## 次セッションの最優先アクション

1. **working tree(未コミット全適用)側**で同じ「完全クリーン→初回ビルド ×6」を回し、まず competition を再現させる
   (再現しなければ前回と条件が変わっている＝別要因)。
2. 再現したら working tree で `PrepareAtprotoLexicon` を疑う実験:
   - `_BBCliGenerate` の `Inputs` から `@(LexiconDocument)` を外す or 静的化して競合が消えるか。
   - `PrepareAtprotoLexicon` の ItemGroup 追加を `BeforeTargets` ではなく別経路にした場合の挙動。
3. handle.exe(`/c/Users/S25367/AppData/Local/Microsoft/WindowsApps/handle`)で失敗時に `.g.cs` を
   握る PID/プロセス名を特定(bbgen exe の前 run が握っている仮説の検証)。

## 仮説(未検証、優先度順)

1. `PrepareAtprotoLexicon` が `_BBCliGenerate` の依存経由で `@(LexiconDocument)` を実行時追加 → Inputs 再評価と
   本体 Exec の間で bbgen が 2 回起動する経路がある(MSBuild の2グローバルコンテキスト評価等)。
2. NativeAOT でない bbgen exe の前 run プロセスがファイルハンドルを保持したまま次 run が書く。
3. outer/inner build で同じ obj に書く(ただし出力パスは `debug_net10.0` / `debug_netstandard2.0` と
   TFM 分離されているので可能性は低い)。

## 検証環境の後片付け

- worktree: `git worktree remove ../BlueBlaze-verify803`(未コミットの検証物があるので `--force` が要る場合あり)。
- worktree 内の `verify-race.sh` / `verify-logs/` / コピーした `external/` はコミット対象外(別ディレクトリ)。
