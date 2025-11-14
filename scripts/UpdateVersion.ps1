# PowerShell script to update version in both Schedule1ModdingTool.csproj and AutoUpdater.xml
# Usage: .\scripts\UpdateVersion.ps1 -Version "1.0.0-beta.3"
# Or: .\scripts\UpdateVersion.ps1 (will prompt for version)

param(
    [Parameter(Mandatory=$false)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

# Get the script directory and project root
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

# Paths to files
$csprojPath = Join-Path $projectRoot "Schedule1ModdingTool.csproj"
$xmlPath = Join-Path $projectRoot "AutoUpdater.xml"

# If version not provided, prompt for it
if ([string]::IsNullOrWhiteSpace($Version)) {
    # Try to get current version from csproj
    $currentVersion = Select-String -Path $csprojPath -Pattern '<Version>(.*)</Version>' | ForEach-Object { $_.Matches.Groups[1].Value }
    
    if ($currentVersion) {
        Write-Host "Current version: $currentVersion"
        $Version = Read-Host "Enter new version (or press Enter to keep current)"
        
        if ([string]::IsNullOrWhiteSpace($Version)) {
            Write-Host "No version provided. Exiting."
            exit 0
        }
    } else {
        $Version = Read-Host "Enter version"
    }
}

# Ask if this is a beta version
$isBeta = Read-Host "Is this a beta version? (y/n)"
$isBeta = $isBeta.Trim().ToLower()

# Determine URL version (append -beta if beta, otherwise use version as-is)
$urlVersion = $Version
if ($isBeta -eq "y" -or $isBeta -eq "yes") {
    # Only append -beta if it's not already in the version
    if ($Version -notmatch "-beta") {
        $urlVersion = "$Version-beta"
    } else {
        $urlVersion = $Version
    }
}

Write-Host "Updating version to: $Version" -ForegroundColor Green
if ($isBeta -eq "y" -or $isBeta -eq "yes") {
    Write-Host "URL version will be: $urlVersion" -ForegroundColor Yellow
}

# Update Schedule1ModdingTool.csproj
if (Test-Path $csprojPath) {
    $csprojContent = Get-Content $csprojPath -Raw
    $csprojContent = $csprojContent -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
    $csprojContent = $csprojContent -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>"
    $csprojContent = $csprojContent -replace '<FileVersion>.*?</FileVersion>', "<FileVersion>$Version</FileVersion>"
    $csprojContent | Set-Content $csprojPath -NoNewline
    Write-Host "[OK] Updated Schedule1ModdingTool.csproj (Version, AssemblyVersion, FileVersion)" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Schedule1ModdingTool.csproj not found at: $csprojPath" -ForegroundColor Red
    exit 1
}

# Update AutoUpdater.xml
if (Test-Path $xmlPath) {
    $xmlContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<item>
    <version>$Version</version>
    <url>https://github.com/S1M0D/ModcreatorSchedule1/releases/download/v$urlVersion/Schedule1ModdingTool-$urlVersion.zip</url>
    <changelog>https://github.com/S1M0D/ModcreatorSchedule1/releases/latest</changelog>
    <mandatory>true</mandatory>
</item>
"@
    $xmlContent | Out-File -FilePath $xmlPath -Encoding UTF8 -NoNewline
    Write-Host "[OK] Updated AutoUpdater.xml" -ForegroundColor Green
} else {
    Write-Host "[ERROR] AutoUpdater.xml not found at: $xmlPath" -ForegroundColor Red
    exit 1
}

Write-Host "`nVersion update complete!" -ForegroundColor Green

