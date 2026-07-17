import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
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

function main(): void {
  const version = readVersion();
  const tag = `${TAG_PREFIX}${version}`;
  const tagRef = `refs/tags/${tag}`;

  console.log(`atproto lexicon を ${tag} から取得します。`);

  ensureRepo();

  // 巨大な atproto リポジトリ全体を展開しないよう、lexicons/ だけを疎に取得する。
  git(["sparse-checkout", "init", "--cone"], { cwd: checkoutDir });
  git(["sparse-checkout", "set", SPARSE_PATH], { cwd: checkoutDir });

  // `@` 衝突を避けるため refs/tags/ を明示し、浅く取得する。
  git(["fetch", "--depth", "1", "origin", tagRef], { cwd: checkoutDir });
  git(["checkout", "-q", "FETCH_HEAD"], { cwd: checkoutDir });

  const lexiconsDir = path.join(checkoutDir, SPARSE_PATH);
  if (!existsSync(lexiconsDir)) {
    fail(`取得後に ${lexiconsDir} が存在しません。タグまたは sparse-checkout 設定を確認してください。`);
  }

  console.log(`完了: ${path.relative(repoRoot, lexiconsDir)} (${tag})`);
}

main();
