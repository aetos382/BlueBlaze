import { relative } from 'node:path';
import type { Configuration } from 'lint-staged';

export default {
  '*': (files: readonly string[]) => {
    const fileList = files.join(' ');
    return [
      'bash .git-hooks/check-protected-branch.sh',
      `bash .git-hooks/check-encoding.sh ${fileList}`,
    ];
  },
  // lint-staged は staged ファイルのフルパスを渡してくるが、dotnet format は相対パスしか受け付けないので変換する
  '*.cs': (files: readonly string[]) => {
    const relFiles = files.map(f => relative(import.meta.dirname, f)).join(' ');
    return [
      `dotnet format style BlueBlaze.slnx --verify-no-changes --include ${relFiles}`,
      `dotnet format whitespace BlueBlaze.slnx --verify-no-changes --include ${relFiles}`,
    ];
  },
  'renovate.json': () => 'npm run validate:renovate',
} satisfies Configuration;
