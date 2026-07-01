#!/usr/bin/env pwsh
param(
  [switch] $SkipHookConfig,
  [switch] $SkipSubmoduleUpdate,
  [switch] $SkipDotnetToolRestore,
  [switch] $SkipNpmInstall,
  [switch] $SkipMcpConfig)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (!$SkipHookConfig) {
  Write-Host "Configuring git hooks..."
  git config --local include.path ../.git-hooks/hooks.gitconfig
}

if (!$SkipSubmoduleUpdate) {
  Write-Host "Updating git submodules..."
  git submodule update --init --recursive
}

if (!$SkipDotnetToolRestore) {
  Write-Host "Restoring .NET tools..."
  dotnet tool restore
}

if (!$SkipNpmInstall) {
  Write-Host "Installing npm packages..."
  npm ci
}

if (!$SkipMcpConfig) {
  Write-Host "Configuring GitHub Copilot MCP..."

  $pat = $env:GITHUB_PERSONAL_ACCESS_TOKEN
  if (!$pat) {
    $pat = $env:GH_PAT_FOR_MCP_SERVER
  }

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
}
