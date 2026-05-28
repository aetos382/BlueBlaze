#requires -Modules Pester

BeforeAll {
  $script = $PSCommandPath -replace '\.tests\.ps1$', '.ps1'
  $workspaceDirectory = ('TestDrive:/' | Resolve-Path).ProviderPath
  Push-Location 'TestDrive:/'

@'
root = true

[*.cs]
indent_style = space
indent_size = 4
csharp_style_namespace_declarations = file_scoped:error
'@ > 'TestDrive:/.editorconfig'

@'
<Project>
  <PropertyGroup>
    <LangVersion>14</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
'@ > 'TestDrive:/Directory.Build.props'
}

AfterAll {
  Pop-Location
}

Describe 'dotnet-format hook' {

  Context '違反なし' {
    BeforeAll {
      New-Item 'TestDrive:/clean' -ItemType Directory -Force
      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/clean/clean.csproj'

@'
namespace Clean;

class Foo { }
'@ > 'TestDrive:/clean/Foo.cs'

      $files = (Resolve-Path 'TestDrive:/clean/Foo.cs').ProviderPath
      $solution = (Resolve-Path 'TestDrive:/clean/clean.csproj').ProviderPath
      & $script -StagedFiles $files -ProjectOrSolution $solution
      $exitCode = $LASTEXITCODE
    }

    It '終了コードがゼロ' {
      $exitCode | Should -Be 0
    }
  }

  Context 'style 違反あり（block-scoped namespace）' {
    BeforeAll {
      New-Item 'TestDrive:/style-err' -ItemType Directory -Force
      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/style-err/style-err.csproj'

@'
namespace StyleErr
{
    class Foo { }
}
'@ > 'TestDrive:/style-err/Foo.cs'

      $files = (Resolve-Path 'TestDrive:/style-err/Foo.cs').ProviderPath
      $solution = (Resolve-Path 'TestDrive:/style-err/style-err.csproj').ProviderPath
      & $script -StagedFiles $files -ProjectOrSolution $solution
      $exitCode = $LASTEXITCODE
    }

    It '終了コードが非ゼロ' {
      $exitCode | Should -Not -Be 0
    }
  }

  Context 'whitespace 違反あり（タブインデント）' {
    BeforeAll {
      New-Item 'TestDrive:/ws-err' -ItemType Directory -Force
      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/ws-err/ws-err.csproj'

      "namespace WsErr;`n`nclass Foo`n{`n`tvoid Bar() { }`n}" |
        Set-Content 'TestDrive:/ws-err/Foo.cs'

      $files = (Resolve-Path 'TestDrive:/ws-err/Foo.cs').ProviderPath
      $solution = (Resolve-Path 'TestDrive:/ws-err/ws-err.csproj').ProviderPath
      & $script -StagedFiles $files -ProjectOrSolution $solution
      $exitCode = $LASTEXITCODE
    }

    It '終了コードが非ゼロ' {
      $exitCode | Should -Not -Be 0
    }
  }

  Context '.cs 以外のファイルが混在している' {
    BeforeAll {
      New-Item 'TestDrive:/mixed' -ItemType Directory -Force
      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/mixed/mixed.csproj'

@'
namespace Mixed;

class Foo { }
'@ > 'TestDrive:/mixed/Foo.cs'

      'content' > 'TestDrive:/mixed/readme.md'

      $files = @(
        (Resolve-Path 'TestDrive:/mixed/Foo.cs').ProviderPath
        (Resolve-Path 'TestDrive:/mixed/readme.md').ProviderPath
      )
      $solution = (Resolve-Path 'TestDrive:/mixed/mixed.csproj').ProviderPath
      & $script -StagedFiles $files -ProjectOrSolution $solution
      $exitCode = $LASTEXITCODE
    }

    It '.cs 以外は無視されて終了コードがゼロ' {
      $exitCode | Should -Be 0
    }
  }

  Context '複数プロジェクトがいずれも style・whitespace 両方に違反あり' {
    BeforeAll {
      New-Item 'TestDrive:/both-err-1' -ItemType Directory -Force
      New-Item 'TestDrive:/both-err-2' -ItemType Directory -Force

      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/both-err-1/both-err-1.csproj'
      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/both-err-2/both-err-2.csproj'

      # block-scoped namespace (style 違反) + タブインデント (whitespace 違反)
      "namespace BothErr1`n{`n`tclass Foo { }`n}" |
        Set-Content 'TestDrive:/both-err-1/Foo.cs'

      "namespace BothErr2`n{`n`tclass Bar { }`n}" |
        Set-Content 'TestDrive:/both-err-2/Bar.cs'

      $solutionPath = Join-Path $workspaceDirectory 'both-err.slnx'
@'
<Solution>
  <Projects>
    <Project Path="both-err-1/both-err-1.csproj" />
    <Project Path="both-err-2/both-err-2.csproj" />
  </Projects>
</Solution>
'@ | Set-Content $solutionPath

      $files = @(
        (Resolve-Path 'TestDrive:/both-err-1/Foo.cs').ProviderPath
        (Resolve-Path 'TestDrive:/both-err-2/Bar.cs').ProviderPath
      )
      & $script -StagedFiles $files -ProjectOrSolution $solutionPath
      $exitCode = $LASTEXITCODE
    }

    It '終了コードが非ゼロ' {
      $exitCode | Should -Not -Be 0
    }
  }

  Context '複数プロジェクトにまたがり、片方に style 違反あり' {
    BeforeAll {
      New-Item 'TestDrive:/proj-ok' -ItemType Directory -Force
      New-Item 'TestDrive:/proj-err' -ItemType Directory -Force

      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/proj-ok/proj-ok.csproj'
      '<Project Sdk="Microsoft.NET.Sdk"/>' > 'TestDrive:/proj-err/proj-err.csproj'

@'
namespace ProjOk;

class Foo { }
'@ > 'TestDrive:/proj-ok/Foo.cs'

@'
namespace ProjErr
{
    class Bar { }
}
'@ > 'TestDrive:/proj-err/Bar.cs'

      $solutionPath = Join-Path $workspaceDirectory 'proj-mix.slnx'
@'
<Solution>
  <Projects>
    <Project Path="proj-ok/proj-ok.csproj" />
    <Project Path="proj-err/proj-err.csproj" />
  </Projects>
</Solution>
'@ | Set-Content $solutionPath

      $files = @(
        (Resolve-Path 'TestDrive:/proj-ok/Foo.cs').ProviderPath
        (Resolve-Path 'TestDrive:/proj-err/Bar.cs').ProviderPath
      )
      & $script -StagedFiles $files -ProjectOrSolution $solutionPath
      $exitCode = $LASTEXITCODE
    }

    It '終了コードが非ゼロ' {
      $exitCode | Should -Not -Be 0
    }
  }
}
