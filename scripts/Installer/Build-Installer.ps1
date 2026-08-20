#!/usr/bin/env pwsh
<#
.SYNOPSIS
Build OpenMultiSeat installer package

.DESCRIPTION
Compiles all projects in Release mode and creates NSIS installer

.PARAMETER Configuration
Build configuration (Debug or Release)

.PARAMETER Platform
Target platform (x64)

.EXAMPLE
.\Build-Installer.ps1 -Configuration Release -Platform x64
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('x64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Get script location
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "Building OpenMultiSeat Installer" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Platform: $Platform" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build all projects
Write-Host "Step 1: Building projects..." -ForegroundColor Yellow

$SolutionFile = Join-Path $RepoRoot "OpenMultiSeat.sln"
if (-not (Test-Path $SolutionFile)) {
    Write-Error "Solution file not found: $SolutionFile"
    exit 1
}

try {
    dotnet build $SolutionFile -c $Configuration -p:Platform=$Platform --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "✓ Build succeeded" -ForegroundColor Green
}
catch {
    Write-Error "Build failed: $_"
    exit 1
}

# Step 2: Check NSIS installation
Write-Host ""
Write-Host "Step 2: Checking for NSIS..." -ForegroundColor Yellow

$NSISPath = $null
$NSISLocations = @(
    'C:\Program Files (x86)\NSIS\makensis.exe',
    'C:\Program Files\NSIS\makensis.exe'
)

foreach ($location in $NSISLocations) {
    if (Test-Path $location) {
        $NSISPath = $location
        break
    }
}

if (-not $NSISPath) {
    Write-Warning "NSIS not found. Installer package cannot be created."
    Write-Host "Please install NSIS from: https://nsis.sourceforge.io/"
    exit 1
}

Write-Host "✓ Found NSIS at: $NSISPath" -ForegroundColor Green

# Step 3: Create installer
Write-Host ""
Write-Host "Step 3: Creating installer..." -ForegroundColor Yellow

$NSIScript = Join-Path $ScriptDir "OpenMultiSeat.nsi"
$OutputDir = Join-Path $RepoRoot "build\installer"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

try {
    & $NSISPath /V4 "/O$OutputDir\build.log" $NSIScript
    if ($LASTEXITCODE -ne 0) {
        Write-Error "NSIS build failed with exit code $LASTEXITCODE"
        exit 1
    }
    Write-Host "✓ Installer created successfully" -ForegroundColor Green
}
catch {
    Write-Error "NSIS build failed: $_"
    exit 1
}

# Step 4: Verify installer
Write-Host ""
Write-Host "Step 4: Verifying installer..." -ForegroundColor Yellow

$InstallerFile = Join-Path $OutputDir "OpenMultiSeat-1.0.0-x64-setup.exe"
if (Test-Path $InstallerFile) {
    $FileSize = (Get-Item $InstallerFile).Length / 1MB
    Write-Host "✓ Installer verified" -ForegroundColor Green
    Write-Host "  File: $InstallerFile" -ForegroundColor Green
    Write-Host "  Size: $([Math]::Round($FileSize, 2)) MB" -ForegroundColor Green
}
else {
    Write-Error "Installer file not found: $InstallerFile"
    exit 1
}

Write-Host ""
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Test the installer: $InstallerFile"
Write-Host "2. Verify all components are properly installed"
Write-Host "3. Test uninstall process"
Write-Host ""
