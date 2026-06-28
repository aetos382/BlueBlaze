#!/usr/bin/env pwsh
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

git config --local include.path ../.git-hooks/hooks.gitconfig
git submodule update --init --recursive

dotnet tool restore

npm ci
