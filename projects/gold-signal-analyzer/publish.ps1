<#
    FR-34 packaging — one-command reproducible self-contained build (Cycle 6).

    Produces ONE self-extracting GoldSignalAnalyzer.Wpf.exe (win-x64, self-contained)
    that a non-developer can run with NO .NET SDK / runtime installed. All settings
    come from the checked-in publish profile so there are no CLI flags to remember
    (NFR-PKG-6).

    Usage (from the project root):
        pwsh ./publish.ps1

    Output:
        src/GoldSignalAnalyzer.Wpf/bin/publish/win-x64/GoldSignalAnalyzer.Wpf.exe

    Distribution: zip that folder (currently one .exe) and hand it to the user. No
    installer, no admin rights, no runtime prerequisite. The app writes its per-user
    state to %LOCALAPPDATA%\GoldSignalAnalyzer at runtime; nothing is bundled into the
    package (D6-5, D6-6).
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root 'src/GoldSignalAnalyzer.Wpf/GoldSignalAnalyzer.Wpf.csproj'
$profile = 'win-x64-selfcontained'

Write-Host "Publishing self-contained single-file build (profile: $profile)..." -ForegroundColor Cyan
dotnet publish $proj -p:PublishProfile=$profile --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$out = Join-Path $root 'src/GoldSignalAnalyzer.Wpf/bin/publish/win-x64'
$exe = Join-Path $out 'GoldSignalAnalyzer.Wpf.exe'
if (-not (Test-Path $exe)) { throw "Expected published exe not found at $exe" }

$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "Published OK -> $exe ($sizeMb MB)" -ForegroundColor Green
