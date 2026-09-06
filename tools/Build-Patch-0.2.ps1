$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\patched-5.1\FM26PlayerExport.csproj"
$outDir = Join-Path $repoRoot "dist\patch-0.2"

Write-Host ""
Write-Host "FM26 Player Export - Patch 0.2 Builder"
Write-Host "======================================"
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: The .NET SDK was not found." -ForegroundColor Red
    Write-Host "Install a current .NET SDK, then run this script again."
    exit 1
}

$candidates = @()

if ($env:FM26_PATH) {
    $candidates += $env:FM26_PATH
}

$candidates += @(
    "$env:ProgramFiles(x86)\Steam\steamapps\common\Football Manager 26",
    "$env:ProgramFiles\Steam\steamapps\common\Football Manager 26",
    "D:\SteamLibrary\steamapps\common\Football Manager 26",
    "D:\Steam\steamapps\common\Football Manager 26",
    "E:\SteamLibrary\steamapps\common\Football Manager 26",
    "E:\Steam\steamapps\common\Football Manager 26"
)

$fmPath = $null

foreach ($candidate in $candidates) {
    if ([string]::IsNullOrWhiteSpace($candidate)) {
        continue
    }

    $coreDll = Join-Path $candidate "BepInEx\core\BepInEx.Core.dll"
    if (Test-Path $coreDll) {
        $fmPath = $candidate
        break
    }
}

if (-not $fmPath) {
    Write-Host "FM26 was not found in the common Steam locations."
    $fmPath = Read-Host "Paste the full Football Manager 26 installation folder"

    $coreDll = Join-Path $fmPath "BepInEx\core\BepInEx.Core.dll"
    if (-not (Test-Path $coreDll)) {
        Write-Host "ERROR: BepInEx.Core.dll was not found under that folder." -ForegroundColor Red
        Write-Host "Expected: $coreDll"
        exit 1
    }
}

Write-Host "FM26 path: $fmPath"
Write-Host "Project:   $project"
Write-Host ""

if (Test-Path $outDir) {
    Remove-Item $outDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Host "Building Patch 0.2..."
& dotnet build $project -c Release "-p:FM26Path=$fmPath" -o $outDir

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "BUILD FAILED." -ForegroundColor Red
    exit $LASTEXITCODE
}

$dll = Join-Path $outDir "FM26PlayerExport.dll"

if (-not (Test-Path $dll)) {
    Write-Host ""
    Write-Host "BUILD FINISHED, but FM26PlayerExport.dll was not found where expected." -ForegroundColor Yellow
    Write-Host "Check: $outDir"
    exit 1
}

Write-Host ""
Write-Host "BUILD SUCCEEDED." -ForegroundColor Green
Write-Host "Patched DLL:"
Write-Host $dll
Write-Host ""
Write-Host "This script does NOT install the DLL automatically."
Write-Host "Keep the original DLL backed up before replacing the installed plugin."
Write-Host ""
