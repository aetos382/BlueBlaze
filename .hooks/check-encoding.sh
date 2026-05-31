#!/bin/bash
set -euo pipefail

fail=0

for f in "$@"; do
    # Skip binary files
    if ! grep -Iq . "$f" 2>/dev/null; then
        continue
    fi

    # Check for UTF-8 BOM (EF BB BF)
    bom=$(od -An -tx1 -N3 "$f" | tr -d ' \n')
    if [[ "$bom" == efbbbf* ]]; then
        echo "error: UTF-8 BOM detected: $f" >&2
        fail=1
    fi

    # Check for CRLF (-U: prevent Windows grep from stripping CR in text mode)
    if grep -qU $'\r' "$f" 2>/dev/null; then
        echo "error: CRLF line endings detected: $f" >&2
        fail=1
    fi
done

exit $fail
