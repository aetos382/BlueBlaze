$changed = git status -u --ignore-submodules --porcelain -- '*.cs'
if (-not $changed) { exit 0 }
dotnet build BlueBlaze.slnx --no-restore --no-logo
