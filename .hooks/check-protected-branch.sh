#!/bin/bash
set -euo pipefail

branch=$(git rev-parse --abbrev-ref HEAD)

if [ "$branch" = "main" ]; then
    echo "error: Direct commits to 'main' are not allowed." >&2
    echo "'main' is a protected branch on GitHub." >&2
    echo "Please create a new branch and open a pull request instead of committing directly to 'main'." >&2
    exit 1
fi
