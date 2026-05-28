import { relative } from 'node:path';
import type { Configuration } from 'lint-staged';

export default {
  '*.cs': (files: readonly string[]) => {
    const relFiles = files.map(f => relative(import.meta.dirname, f)).join(' ');
    return [
      `dotnet format style BlueBlaze.slnx --verify-no-changes --include ${relFiles}`,
      `dotnet format whitespace BlueBlaze.slnx --verify-no-changes --include ${relFiles}`,
    ];
  },
} satisfies Configuration;
