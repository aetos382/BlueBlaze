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
$solutionDir = Split-Path $solutionPath -Parent
$relativeFiles = $csFiles | Resolve-Path -Relative -RelativeBasePath $solutionDir

$hasError = $false

try {
  foreach ($type in @('whitespace', 'style')) {
    $outputRecords = (dotnet format $type $solutionPath --include $relativeFiles --verify-no-changes --verbosity diag) 2>&1

    foreach ($outputRecord in $outputRecords) {
      if ($outputRecord -is [System.Management.Automation.ErrorRecord]) {
        Write-Warning -Message $outputRecord.ToString()
        $hasError = $true
      }
      elseif ($outputRecord -is [string]) {
        Write-Verbose $outputRecord
      }
    }
  }
}
catch {
  Write-Error -ErrorRecord $_ -ErrorAction Continue
  $hasError = $true
}

exit ($hasError ? 1 : 0)
