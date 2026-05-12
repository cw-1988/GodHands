
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "WinForms\GodHands\GodHands\GodHands.csproj"
$vstoolsRoot = Join-Path (Split-Path -Parent $repoRoot) "vstools"
$vstoolsBridgePath = Join-Path $repoRoot "WinForms\GodHands\GodHands\Source\Mission\View\Helpers\VstoolsBridge.cs"
$vswherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

function Get-NormalizedRelativePath([string]$basePath, [string]$fullPath) {
    $resolvedBasePath = (Resolve-Path -LiteralPath $basePath).Path.TrimEnd('\')
    $resolvedFullPath = (Resolve-Path -LiteralPath $fullPath).Path

    if (-not $resolvedFullPath.StartsWith($resolvedBasePath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path '$resolvedFullPath' is not under '$resolvedBasePath'."
    }

    $relativePath = $resolvedFullPath.Substring($resolvedBasePath.Length).TrimStart('\')
    return $relativePath.Replace('\', '/')
}

function Get-VstoolsExpectedEmbeddedFiles([string]$rootPath) {
    $expected = New-Object System.Collections.Generic.List[string]
    $expected.Add("index.html")

    foreach ($relativeDir in @("css", "dist")) {
        $fullDir = Join-Path $rootPath $relativeDir
        if (-not (Test-Path -LiteralPath $fullDir)) {
            throw "Missing vstools directory: $fullDir"
        }

        Get-ChildItem -LiteralPath $fullDir -Recurse -File |
            Sort-Object FullName |
            ForEach-Object {
                $expected.Add((Get-NormalizedRelativePath $rootPath $_.FullName))
            }
    }

    return $expected.ToArray()
}

function Get-VstoolsDeclaredEmbeddedFiles([string]$bridgePath) {
    if (-not (Test-Path -LiteralPath $bridgePath)) {
        throw "Could not find VstoolsBridge.cs: $bridgePath"
    }

    $content = Get-Content -LiteralPath $bridgePath -Raw
    $arrayMatch = [regex]::Match(
        $content,
        'embeddedStaticFiles\s*=\s*new\s+string\[\]\s*\{(?<body>.*?)\};',
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    if (-not $arrayMatch.Success) {
        throw "Could not parse embeddedStaticFiles from $bridgePath"
    }

    $declared = New-Object System.Collections.Generic.List[string]
    [regex]::Matches($arrayMatch.Groups["body"].Value, '"([^"]+)"') |
        ForEach-Object {
            $declared.Add($_.Groups[1].Value.Replace('\', '/'))
        }

    return $declared.ToArray()
}

function Test-VstoolsEmbeddingSync([string]$rootPath, [string]$bridgePath) {
    $expected = @(Get-VstoolsExpectedEmbeddedFiles $rootPath)
    $declared = @(Get-VstoolsDeclaredEmbeddedFiles $bridgePath)

    $missing = @($expected | Where-Object { $_ -notin $declared })
    $extra = @($declared | Where-Object { $_ -notin $expected })

    if (($missing.Count -gt 0) -or ($extra.Count -gt 0)) {
        $message = New-Object System.Text.StringBuilder
        [void]$message.AppendLine("GodHands embedded vstools file list is out of sync with _refs/vstools.")
        [void]$message.AppendLine("Update embeddedStaticFiles in VstoolsBridge.cs before building.")

        if ($missing.Count -gt 0) {
            [void]$message.AppendLine("")
            [void]$message.AppendLine("Missing entries:")
            foreach ($entry in $missing) {
                [void]$message.AppendLine("  - $entry")
            }
        }

        if ($extra.Count -gt 0) {
            [void]$message.AppendLine("")
            [void]$message.AppendLine("Extra stale entries:")
            foreach ($entry in $extra) {
                [void]$message.AppendLine("  - $entry")
            }
        }

        throw $message.ToString().TrimEnd()
    }
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Could not find project file: $projectPath"
}

if (-not (Test-Path -LiteralPath $vstoolsRoot)) {
    throw "Could not find vstools root: $vstoolsRoot"
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
Write-Host "Building vstools:" $vstoolsRoot

Push-Location $vstoolsRoot
try {
    & bun run build
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}

Test-VstoolsEmbeddingSync -rootPath $vstoolsRoot -bridgePath $vstoolsBridgePath

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
