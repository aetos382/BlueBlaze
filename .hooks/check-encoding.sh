#!/bin/bash
set -euo pipefail

fail=0
bom_files=()

for f in "$@"; do
    # Skip binary files
    if ! grep -Iq . "$f" 2>/dev/null; then
        continue
    fi

    # Check for UTF-8 BOM (EF BB BF)
    bom=$(od -An -tx1 -N3 "$f" | tr -d ' \n')
    if [[ "$bom" == efbbbf* ]]; then
        echo "error: UTF-8 BOM detected: $f" >&2
        bom_files+=("$f")
        fail=1
    fi

done

if [ ${#bom_files[@]} -gt 0 ]; then
    echo "" >&2
    echo "BOM を除去するには以下を実行してください:" >&2
    echo "  bash .hooks/remove-bom.sh ${bom_files[*]}" >&2
fi

exit $fail
