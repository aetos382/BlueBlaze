#!/usr/bin/env bash
set -euo pipefail

export GITHUB_PERSONAL_ACCESS_TOKEN="$(op read 'op://Automation/GitHub Personal Access Token for MCP Server/credential' --no-newline)"

pwsh -File Register-GitHubCopilotMcp.ps1
