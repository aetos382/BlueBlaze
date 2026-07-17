import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import process from "node:process";

// atproto の lexicon を「@atproto/api の semver タグ」時点のツリーから取得する。
//
// この repo は atproto を git submodule では管理しない。理由は次の通り:
// - Renovate の git-submodules manager は .gitmodules の branch 値をそのまま
//   semver 判定するため、`@atproto/api@x.y.z` のようなモノレポ形式タグを追従
//   できない(裸バージョンを書き戻して .gitmodules を壊す)。
// - 代わりに .renovate/atproto-lexicon.version の裸バージョンを Renovate の
//   customManager(git-tags + extractVersion) で追従し、このスクリプトが
//   `@atproto/api@<version>` を再合成して該当タグの lexicons/ だけを取得する。
//
// タグ名 `@atproto/api@x.y.z` の `@` は git の HEAD/reflog 構文と衝突するため、
// fetch/checkout では必ず `refs/tags/` を明示する。

const REPO_URL = "https://github.com/bluesky-social/atproto.git";
const TAG_PREFIX = "@atproto/api@";
const SPARSE_PATH = "lexicons";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(scriptDir, "..");
const versionFile = path.join(repoRoot, ".renovate", "atproto-lexicon.version");
const checkoutDir = path.join(repoRoot, "external", "atproto");
// 取得済みバージョンを記録するスタンプ。MSBuild が version ファイル(Inputs)と
// このスタンプ(Outputs)のタイムスタンプを比較して増分実行を判断する。
const stampFile = path.join(checkoutDir, ".fetched-version");
// マルチターゲットビルド等で同一の checkoutDir に並列 fetch すると
// `shallow file has changed` で壊れるため、ディレクトリ作成のアトミック性で排他する。
const lockDir = path.join(repoRoot, "external", ".atproto-fetch.lock");
const LOCK_TIMEOUT_MS = 120_000;
const LOCK_POLL_MS = 100;

function fail(message: string): never {
  console.error(`ERROR: ${message}`);
  process.exit(1);
}

// .renovate/atproto-lexicon.version から ATPROTO_LEXICON_VERSION を読む。
function readVersion(): string {
  if (!existsSync(versionFile)) {
    fail(`バージョンファイルが見つかりません: ${versionFile}`);
  }
  const content = readFileSync(versionFile, "utf8");
  const match = content.match(/^ATPROTO_LEXICON_VERSION=(\S+)/m);
  if (!match) {
    fail(`ATPROTO_LEXICON_VERSION が ${versionFile} に見つかりません。`);
  }
  return match[1];
}

// git コマンドを checkoutDir(または cwd 指定)で実行する。失敗したら終了。
function git(args: readonly string[], options: { cwd?: string } = {}): string {
  const result = spawnSync("git", args, {
    cwd: options.cwd ?? repoRoot,
    stdio: ["ignore", "pipe", "inherit"],
    encoding: "utf8",
  });
  if (result.error) {
    fail(`git の実行に失敗しました: ${result.error.message}`);
  }
  if (result.status !== 0) {
    fail(`git ${args.join(" ")} が失敗しました (exit ${result.status})。`);
  }
  return result.stdout.trim();
}

// checkoutDir に git リポジトリが無ければ空の状態で初期化する。
function ensureRepo(): void {
  const gitDir = path.join(checkoutDir, ".git");
  if (existsSync(gitDir)) {
    return;
  }
  git(["init", "-q", checkoutDir]);
  // remote は再取得時の一貫性のため固定名 origin で登録する。
  git(["remote", "add", "origin", REPO_URL], { cwd: checkoutDir });
}

const lexiconsDir = path.join(checkoutDir, SPARSE_PATH);

// スタンプが tag 一致 + lexicons/ が存在するなら取得済み。
function isUpToDate(tag: string): boolean {
  return existsSync(stampFile) && existsSync(lexiconsDir) &&
    readFileSync(stampFile, "utf8").trim() === tag;
}

// ディレクトリ作成のアトミック性で排他ロックを取る。TIMEOUT まで待つ。
function acquireLock(): void {
  mkdirSync(path.dirname(lockDir), { recursive: true });
  const deadline = Date.now() + LOCK_TIMEOUT_MS;
  for (;;) {
    try {
      mkdirSync(lockDir);
      return;
    } catch (err) {
      if ((err as NodeJS.ErrnoException).code !== "EEXIST") {
        throw err;
      }
      if (Date.now() > deadline) {
        fail(`ロック ${lockDir} の取得がタイムアウトしました。残留していれば手動で削除してください。`);
      }
      // 他プロセスが取得中。Atomics.wait で同期的に短時間待つ。
      Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, LOCK_POLL_MS);
    }
  }
}

function releaseLock(): void {
  rmSync(lockDir, { recursive: true, force: true });
}

function fetchLexicon(tag: string, tagRef: string): void {
  console.log(`atproto lexicon を ${tag} から取得します。`);

  ensureRepo();

  // 巨大な atproto リポジトリ全体を展開しないよう、lexicons/ だけを疎に取得する。
  git(["sparse-checkout", "init", "--cone"], { cwd: checkoutDir });
  git(["sparse-checkout", "set", SPARSE_PATH], { cwd: checkoutDir });

  // `@` 衝突を避けるため refs/tags/ を明示し、浅く取得する。
  git(["fetch", "--depth", "1", "origin", tagRef], { cwd: checkoutDir });
  git(["checkout", "-q", "FETCH_HEAD"], { cwd: checkoutDir });

  if (!existsSync(lexiconsDir)) {
    fail(`取得後に ${lexiconsDir} が存在しません。タグまたは sparse-checkout 設定を確認してください。`);
  }

  // 取得完了を記録する。MSBuild の Outputs はこのファイルのタイムスタンプを見る。
  writeFileSync(stampFile, `${tag}\n`, "utf8");

  console.log(`完了: ${path.relative(repoRoot, lexiconsDir)} (${tag})`);
}

function main(): void {
  const version = readVersion();
  const tag = `${TAG_PREFIX}${version}`;
  const tagRef = `refs/tags/${tag}`;

  // ロック取得前の早期スキップ。毎ビルド呼び出しで余計なロック競合を避けるため。
  if (isUpToDate(tag)) {
    console.log(`atproto lexicon は ${tag} で最新です。スキップします。`);
    return;
  }

  // マルチターゲット/複数プロジェクトの並列ビルドが同一 checkoutDir に同時 fetch
  // すると壊れるため、取得はロックで直列化する。
  acquireLock();
  try {
    // ロック待ちの間に別プロセスが取得を終えている場合があるので再確認する。
    if (isUpToDate(tag)) {
      console.log(`atproto lexicon は ${tag} で最新です。スキップします。`);
      return;
    }
    fetchLexicon(tag, tagRef);
  } finally {
    releaseLock();
  }
}

main();
