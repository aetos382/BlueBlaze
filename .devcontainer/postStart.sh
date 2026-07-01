#!/usr/bin/env bash
set -euo pipefail

export GITHUB_PERSONAL_ACCESS_TOKEN="$(op read 'op://Private/BlueBlaze GitHub PAT/credential' --no-newline)"

pwsh -File Register-GitHubCopilotMcp.ps1
