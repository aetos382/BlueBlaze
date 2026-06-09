#!/bin/bash
set -euo pipefail

if [ $# -eq 0 ]; then
    echo "Usage: $0 <file>..." >&2
    exit 1
fi

for f in "$@"; do
    if [ ! -f "$f" ]; then
        echo "error: not a file: $f" >&2
        continue
    fi

    # UTF-8 BOM (EF BB BF) の有無を確認
    bom=$(od -An -tx1 -N3 "$f" | tr -d ' \n')
    if [[ "$bom" == efbbbf* ]]; then
        sed -i '1s/^\xEF\xBB\xBF//' "$f"
        echo "BOM removed: $f"
    else
        echo "No BOM: $f"
    fi
done
