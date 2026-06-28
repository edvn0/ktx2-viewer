#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes KTX2 Viewer and builds an NSIS installer that associates
    .ktx / .ktx2 files with the application.

.PARAMETER Version
    Installer/product version (e.g. 1.0.0). Default: 1.0.0

.PARAMETER SkipPublish
    Reuse the existing .\publish output instead of re-publishing.

.EXAMPLE
    .\build-installer.ps1 -Version 1.2.0
#>
param(
    [string]$Version = "1.0.0",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$root        = $PSScriptRoot
$projectPath = Join-Path $root "KtxViewer.UI\KtxViewer.UI\KtxViewer.UI.csproj"
$publishDir  = Join-Path $root "publish"
$distDir     = Join-Path $root "dist"
$nsiScript   = Join-Path $root "installer\KtxViewer.nsi"
$ktxDllSource = "C:\Program Files\KTX-Software\bin\ktx.dll"

# 1. Publish the application -------------------------------------------------
if (-not $SkipPublish) {
    Write-Host "Publishing application..." -ForegroundColor Cyan

    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }

    dotnet publish $projectPath `
        --configuration Release `
        --runtime win-x64 `
        --self-contained false `
        --output $publishDir `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }

    if (Test-Path $ktxDllSource) {
        Copy-Item $ktxDllSource $publishDir -Force
        Write-Host "ktx.dll copied." -ForegroundColor Green
    } else {
        Write-Host "WARNING: ktx.dll not found at $ktxDllSource - BasisU textures won't decode." -ForegroundColor Yellow
    }
} else {
    Write-Host "Skipping publish (using existing $publishDir)." -ForegroundColor Yellow
}

if (-not (Test-Path (Join-Path $publishDir "KtxViewer.UI.exe"))) {
    throw "Published executable not found in $publishDir. Run without -SkipPublish first."
}

# 2. Locate makensis ---------------------------------------------------------
function Find-MakeNsis {
    $cmd = Get-Command makensis.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $candidates = @(
        "$env:ProgramFiles\NSIS\makensis.exe",
        "${env:ProgramFiles(x86)}\NSIS\makensis.exe"
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) { return $c }
    }
    return $null
}

$makensis = Find-MakeNsis
if (-not $makensis) {
    throw "makensis.exe not found. Install NSIS (https://nsis.sourceforge.io) or add it to PATH."
}
Write-Host "Using NSIS: $makensis" -ForegroundColor DarkGray

# 3. Build the installer -----------------------------------------------------
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

$outFile = Join-Path $distDir "KtxViewerSetup-$Version.exe"
Write-Host "Building installer for version $Version..." -ForegroundColor Cyan

& $makensis `
    "/DVERSION=$Version" `
    "/DPUBLISH_DIR=$publishDir" `
    "/DOUTFILE=$outFile" `
    $nsiScript

if ($LASTEXITCODE -ne 0) {
    throw "makensis failed."
}

Write-Host "`nInstaller created:" -ForegroundColor Green
Write-Host "  $outFile" -ForegroundColor Cyan
if (Test-Path $outFile) {
    $size = [math]::Round((Get-Item $outFile).Length / 1MB, 2)
    Write-Host "  Size: $size MB" -ForegroundColor Cyan
}
