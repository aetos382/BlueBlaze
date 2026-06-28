$json = [Console]::In.ReadToEnd() | ConvertFrom-Json
$file = $json.file_path

if (-not $file -or $file -notmatch '\.cs$') {
    exit 0
}

if ($file -match '[/\\](obj|bin|artifacts)[/\\]') {
    exit 0
}

$rel = [IO.Path]::GetRelativePath($PWD, $file)
dotnet format style BlueBlaze.slnx --include $rel --no-restore
dotnet format whitespace BlueBlaze.slnx --include $rel --no-restore
