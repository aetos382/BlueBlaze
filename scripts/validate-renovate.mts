import { spawnSync } from "node:child_process";
import process from "node:process";

// renovate.json を renovate-config-validator で検証する。
//
// devcontainer には renovate がグローバル install されているので、その
// renovate-config-validator を PATH から直接使う。
// ホスト(Windows 等、renovate 未インストール)では npx でフォールバックし、
// コンテナ・ホストのどちらでもコミット時に検証が動くようにする。
//
// 引数は渡さない。renovate-config-validator は引数なしだとデフォルト位置の
// renovate.json を repo config として検証する。

function run(command: string, args: readonly string[], shell: boolean): number | null {
  // stdin は ignore(即 EOF)にする。過去に renovate-config-validator が
  // stdin 入力待ちで固まった経緯があり、それを確実に回避するため。
  // stdout/stderr は inherit して検証結果をそのまま表示する。
  const result = spawnSync(command, args, { stdio: ["ignore", "inherit", "inherit"], shell });
  // コマンド自体が見つからない場合は null を返してフォールバックさせる。
  // validation 失敗(非ゼロ status)はここに来ないのでフォールバックしない。
  if (result.error && (result.error as NodeJS.ErrnoException).code === "ENOENT") {
    return null;
  }
  return result.status;
}

// まず PATH 上の renovate-config-validator を試す(devcontainer 経路)。
// Windows では拡張子解決の都合で shell 経由が要るが、ホストには validator を
// 入れない前提なので shell: false のままで良い(見つからなければ npx に回る)。
let status = run("renovate-config-validator", [], false);

if (status === null) {
  // Windows で npx.cmd を解決するため shell 経由で起動する。
  status = run(
    "npx",
    ["--yes", "--package", "renovate", "--", "renovate-config-validator"],
    true,
  );
}

process.exit(status ?? 1);
