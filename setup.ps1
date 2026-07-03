#!/usr/bin/env pwsh
param(
  [switch] $SkipHookConfig,
  [switch] $SkipSubmoduleUpdate,
  [switch] $SkipDotnetToolRestore,
  [switch] $SkipNpmInstall,
  [switch] $SkipMcpConfig,
  [switch] $SkipPluginInstall)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (!$SkipHookConfig) {
  Write-Host "Configuring git hooks..."
  git config --local include.path ../.gitconfig
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
  & "$PSScriptRoot/Register-GitHubCopilotMcp.ps1"
}

if (!$SkipPluginInstall) {
  Write-Host "Installing Claude Code plugins..."
  $settings = Get-Content "$PSScriptRoot/.claude/settings.json" -Raw | ConvertFrom-Json

  foreach ($marketplace in $settings.extraKnownMarketplaces.PSObject.Properties) {
    claude plugin marketplace add --scope project $marketplace.Value.source.repo
  }

  foreach ($plugin in $settings.enabledPlugins.PSObject.Properties) {
    if ($plugin.Value) {
      claude plugin install --scope project $plugin.Name
    }
  }
}
