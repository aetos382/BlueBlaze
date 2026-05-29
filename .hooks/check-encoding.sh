#!/bin/bash
set -euo pipefail

fail=0

for f in "$@"; do
    # バイナリファイルはスキップ
    if ! grep -Iq . "$f" 2>/dev/null; then
        continue
    fi

    # UTF-8 BOM (EF BB BF) チェック
    bom=$(od -An -tx1 -N3 "$f" | tr -d ' \n')
    if [[ "$bom" == efbbbf* ]]; then
        echo "error: UTF-8 BOM が検出されました: $f" >&2
        fail=1
    fi

    # CRLF チェック
    if grep -q $'\r' "$f" 2>/dev/null; then
        echo "error: CRLF 改行が検出されました: $f" >&2
        fail=1
    fi
done

exit $fail
