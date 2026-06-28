# Stop フック: Claude が .cs ファイルを編集した応答の終了時に dotnet build を実行してビルドエラーを検出する。
# PostToolUse フック (if-cs-changed.ps1) が作成したセンチネルファイルで .cs 編集の有無を判定する。

$ErrorActionPreference = 'Stop'

$flag = "$PSScriptRoot/.cs-changed"
if (-not (Test-Path $flag)) { exit 0 }
Remove-Item $flag

dotnet build BlueBlaze.slnx --no-restore --no-logo

# exit 2 = ブロッキングエラー。Stop フックで exit 2 を返すと Claude の停止が阻止され、
# stderr がフィードバックされて修正ループに入る。exit 1 は非ブロッキングで通知のみ。
# https://code.claude.com/docs/ja/hooks
if ($LASTEXITCODE -ne 0) { exit 2 }
