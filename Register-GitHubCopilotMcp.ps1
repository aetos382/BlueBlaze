#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pat = $env:GITHUB_PERSONAL_ACCESS_TOKEN

if (!$pat) {
  Write-Error "GITHUB_PERSONAL_ACCESS_TOKEN environment variable is not set."
  exit 1
}

$githubMcpConfig = @{
  type = 'http'
  url = 'https://api.githubcopilot.com/mcp/'
  headers = @{
    Authorization = "Bearer $pat"
  }
}

claude mcp remove github
claude mcp add-json --scope user github ($githubMcpConfig | ConvertTo-Json -Compress)
