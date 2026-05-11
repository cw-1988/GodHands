
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "WinForms\GodHands\GodHands\GodHands.csproj"
$vswherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Could not find project file: $projectPath"
}

if (-not (Test-Path -LiteralPath $vswherePath)) {
    throw "Could not find vswhere.exe at: $vswherePath"
}

$msbuildPath = & $vswherePath `
    -latest `
    -products * `
    -requires Microsoft.Component.MSBuild `
    -find MSBuild\**\Bin\MSBuild.exe |
    Select-Object -First 1

if (-not $msbuildPath) {
    throw "Could not locate MSBuild.exe via vswhere."
}

Write-Host "Using MSBuild:" $msbuildPath
Write-Host "Building:" $projectPath
Write-Host "Configuration:" $Configuration

$runningGodHands = Get-Process -Name "GodHands" -ErrorAction SilentlyContinue
if ($runningGodHands) {
    Write-Host "Closing running GodHands.exe before build..."
    $runningGodHands | Stop-Process -Force
    foreach ($process in $runningGodHands) {
        Wait-Process -Id $process.Id -ErrorAction SilentlyContinue
    }
}

& $msbuildPath `
    $projectPath `
    /restore `
    /p:Configuration=$Configuration `
    /p:RuntimeIdentifier=win `
    /p:RuntimeIdentifiers=win

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$outputExe = Join-Path $repoRoot ("WinForms\GodHands\GodHands\bin\" + $Configuration + "\GodHands.exe")
if (Test-Path -LiteralPath $outputExe) {
    Write-Host "Build output:" $outputExe
    Write-Host "Launching GodHands.exe..."
    Start-Process -FilePath $outputExe
}
else {
    throw "Build succeeded but output executable was not found: $outputExe"
}
