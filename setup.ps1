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

  $marketplaces = @(
    @{
      Id      = 'claude-plugins-official'
      Repo    = 'anthropics/claude-plugins-official'
      Plugins = @(
        'claude-code-setup',
        'commit-commands',
        'feature-dev',
        'frontend-design',
        'pr-review-toolkit',
        'skill-creator'
      )
    },
    @{
      Id      = 'microsoft-docs-marketplace'
      Repo    = 'MicrosoftDocs/mcp'
      Plugins = @(
        'microsoft-docs'
      )
    },
    @{
      Id      = 'dotnet-agent-skills'
      Repo    = 'dotnet/skills'
      Plugins = @(
        'dotnet',
        'dotnet-advanced',
        'dotnet-ai',
        'dotnet-aspnetcore',
        'dotnet-blazor',
        'dotnet-data',
        'dotnet-diag',
        'dotnet-maui',
        'dotnet-msbuild',
        'dotnet-nuget',
        'dotnet-template-engine',
        'dotnet-test'
      )
    }
  )

  foreach ($marketplace in $marketplaces) {
    claude plugin marketplace add --scope user $marketplace.Repo

    foreach ($plugin in $marketplace.Plugins) {
      claude plugin install --scope user "$($plugin)@$($marketplace.Id)"
    }
  }
}
