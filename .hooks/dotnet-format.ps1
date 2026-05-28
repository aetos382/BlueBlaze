param(
  [Parameter(Mandatory, ValueFromRemainingArguments)]
  [string[]] $StagedFiles,

  [Parameter(Mandatory)]
  [ValidateScript({Test-Path $_ -PathType Container})]
  [string] $WorkspaceDirectory = $env:WORKSPACE,

  [switch] $EmitFormatReport)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-FullPath {
  [CmdletBinding()]
  [OutputType([string])]

  param(
    [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
    [string] $Path)

  return (Resolve-Path $Path).Path
}

function Test-UnderWorkspace {
  param(
    [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
    [string] $Path)

  return !(Resolve-Path $Path -Relative -RelativeBasePath $workspace).StartsWith('..', [StringComparison]::Ordinal)
}

function Group-CSharpFilesByProject {
  [CmdletBinding()]

  param(
    [Parameter(Mandatory, ValueFromRemainingArguments)]
    [string[]] $Path)

  $Path -like '*.cs' |
    Resolve-Path -Relative -RelativeBasePath $workspace |
    ForEach-Object {
      $filePath = $_
      $directory = Split-Path $filePath | Get-FullPath
      $project = $null

      while (!$project -and (Test-UnderWorkspace $directory)) {
        $projects = $directory | Join-Path -ChildPath '*.csproj' | Get-Item

        switch (@($projects).Count) {
          0 { $directory = Split-Path $directory | Get-FullPath; continue }
          1 { $project = $projects[0]; break }
          default { Write-Error -Message "Multiple projects found for $filePath"; break }
        }
      }

      if (!$project) {
        Write-Error -Message "No projects found for $filePath"
        return
      }

      return [PSCustomObject] @{
        Path = $filePath
        Project = $project
      }
    } |
    Group-Object -Property Project |
    Write-Output
}

$workspace = $WorkspaceDirectory | Get-FullPath

Push-Location $workspace
$hasError = $false

try {
  $groups = Group-CSharpFilesByProject $StagedFiles

  foreach ($group in $groups) {
    $projectPath = $group.Name
    $files = $group.Group | Select-Object -ExpandProperty Path

    foreach ($type in @('whitespace', 'style')) {
      $reportArgs = @()

      if ($EmitFormatReport) {
        $reportDirectory = $projectPath | Split-Path -Parent | Join-Path -ChildPath "reports/${type}"
        New-Item $reportDirectory -ItemType Directory

        $reportArgs = ('--report', $reportDirectory)
      }

      $outputRecords = (dotnet format $type $projectPath --include $files --verify-no-changes --verbosity diag @reportArgs) 2>&1

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
}
catch {
  Write-Error -ErrorRecord $_ -ErrorAction Continue
  $hasError = $true
}
finally {
  Pop-Location
}

$exitCode = if ($hasError) { 1 } else { 0 }
exit $exitCode
