param(
  [Parameter(Mandatory, ValueFromRemainingArguments)]
  [string[]] $StagedFiles,

  [Parameter(Mandatory)]
  [ValidateScript({Test-Path $_ -PathType Leaf})]
  [string] $ProjectOrSolution)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$csFiles = $StagedFiles -like '*.cs'
if (!$csFiles) { exit 0 }

$solutionPath = (Resolve-Path $ProjectOrSolution).Path

$hasError = $false

try {
  foreach ($type in @('whitespace', 'style')) {
    # --include を使うと --verify-no-changes で違反が検出されない既知の不具合のため、プロジェクト全体を検査する
    # dotnet/format#1479
    $outputRecords = (dotnet format $type $solutionPath --verify-no-changes --verbosity diag) 2>&1
    $formatExitCode = $LASTEXITCODE

    foreach ($outputRecord in $outputRecords) {
      if ($outputRecord -is [System.Management.Automation.ErrorRecord]) {
        Write-Warning -Message $outputRecord.ToString()
      }
      elseif ($outputRecord -is [string]) {
        Write-Verbose $outputRecord
      }
    }

    if ($formatExitCode -ne 0) {
      $hasError = $true
    }
  }
}
catch {
  Write-Error -ErrorRecord $_ -ErrorAction Continue
  $hasError = $true
}

exit ($hasError ? 1 : 0)
