# PostToolUse フック: Claude が .cs ファイルを編集するたびに以下を実行する。
#   1. Stop フック (build-if-cs-changed.ps1) 用のセンチネルファイルを作成する
#   2. dotnet format を実行してコードスタイルを自動修正する

$ErrorActionPreference = 'Stop'

# stdin から受け取る JSON スキーマ: { tool_name, tool_input: { file_path, ... }, tool_output, ... }
# https://code.claude.com/docs/ja/hooks
$json = [Console]::In.ReadToEnd() | ConvertFrom-Json
$file = $json.tool_input.file_path

if (-not $file -or $file -notmatch '\.cs$') {
    exit 0
}

if ($file -match '[/\\](obj|bin|artifacts)[/\\]') {
    exit 0
}

function Set-CsChangedFlag {
    $null = New-Item -ItemType File -Force "$PSScriptRoot/.cs-changed"
}

function Invoke-Format([string] $file) {
    $rel = [IO.Path]::GetRelativePath($PWD, $file)

    dotnet format style BlueBlaze.slnx --include $rel --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet format whitespace BlueBlaze.slnx --include $rel --no-restore
    exit $LASTEXITCODE
}

Set-CsChangedFlag
Invoke-Format $file
