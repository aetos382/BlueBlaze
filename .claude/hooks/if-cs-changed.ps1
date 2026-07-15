# PostToolUse フック: Claude が .cs/.csproj/.props/.targets ファイルを編集するたびに以下を実行する。
#   1. Stop フック (build-if-cs-changed.ps1) 用のセンチネルファイルを作成する
#   2. .cs ファイルの場合、dotnet format を実行してコードスタイルを自動修正する

$ErrorActionPreference = 'Stop'

# stdin から受け取る JSON スキーマ: { tool_name, tool_input: { file_path, ... }, tool_output, ... }
# https://code.claude.com/docs/ja/hooks
$json = [Console]::In.ReadToEnd() | ConvertFrom-Json
$file = $json.tool_input.file_path

# .csproj/.props/.targets の変更だけで顕在化するビルドエラー(対象 TargetFramework の
# 組み合わせ依存など)もあるため、.cs 以外のビルド関連ファイルもセンチネル対象に含める。
if (-not $file -or $file -notmatch '\.(cs|csproj|props|targets)$') {
    exit 0
}

if ($file -match '[/\\](obj|bin|artifacts)[/\\]') {
    exit 0
}

function Set-BuildNeededFlag {
    $null = New-Item -ItemType File -Force "$PSScriptRoot/.build-needed"
}

function Invoke-Format([string] $file) {
    $rel = [IO.Path]::GetRelativePath($PWD, $file)

    dotnet format style BlueBlaze.slnx --include $rel --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet format whitespace BlueBlaze.slnx --include $rel --no-restore
    exit $LASTEXITCODE
}

Set-BuildNeededFlag

if ($file -match '\.cs$') {
    Invoke-Format $file
}
