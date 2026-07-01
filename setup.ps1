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
  & "$PSScriptRoot/Register-GitHubCopilotMcp.ps1"
}
