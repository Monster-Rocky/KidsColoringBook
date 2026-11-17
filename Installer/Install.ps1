# Kids Coloring Book - Windows Desktop Installer
param(
    [string]$TargetDir = ""
)
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "    Kids Coloring Book - Windows Installer v1.0.0         " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Cyan

$sourceDir = Join-Path $PSScriptRoot "..\Publish"
if (-not (Test-Path $sourceDir)) {
    $sourceDir = Join-Path $PSScriptRoot "Publish"
}

if (-not (Test-Path $sourceDir)) {
    Write-Error "Could not find Publish directory. Please build/publish the application first."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $installDir = Join-Path $env:LOCALAPPDATA "Programs\KidsColoringBook"
} else {
    $installDir = $TargetDir
}
Write-Host "Installing to: $installDir" -ForegroundColor Green

if (Test-Path $installDir) {
    Write-Host "Removing previous installation..." -ForegroundColor Gray
    Remove-Item -Path $installDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -Path "$sourceDir\*" -Destination $installDir -Recurse -Force

# Create Desktop Shortcut
try {
    $wsh = New-Object -ComObject WScript.Shell
    $desktop = [Environment]::GetFolderPath("Desktop")
    $desktopShortcutPath = Join-Path $desktop "Kids Coloring Book.lnk"
    $shortcut = $wsh.CreateShortcut($desktopShortcutPath)
    $shortcut.TargetPath = Join-Path $installDir "KidsColoringBook.exe"
    $shortcut.WorkingDirectory = $installDir
    $shortcut.Description = "Kids Coloring Book - Offline Desktop Game"
    $shortcut.Save()
    Write-Host "Created Desktop Shortcut: $desktopShortcutPath" -ForegroundColor Green
} catch {
    Write-Host "Skipped Desktop shortcut (restricted environment): $_" -ForegroundColor DarkGray
}

# Create Start Menu Shortcut
try {
    $startMenu = [Environment]::GetFolderPath("StartMenu")
    $programsFolder = Join-Path $startMenu "Programs\Kids Coloring Book"
    New-Item -ItemType Directory -Force -Path $programsFolder | Out-Null
    $startShortcutPath = Join-Path $programsFolder "Kids Coloring Book.lnk"
    $startShortcut = $wsh.CreateShortcut($startShortcutPath)
    $startShortcut.TargetPath = Join-Path $installDir "KidsColoringBook.exe"
    $startShortcut.WorkingDirectory = $installDir
    $startShortcut.Description = "Kids Coloring Book - Offline Desktop Game"
    $startShortcut.Save()
    Write-Host "Created Start Menu Shortcut: $startShortcutPath" -ForegroundColor Green
} catch {
    Write-Host "Skipped Start Menu shortcut (restricted environment): $_" -ForegroundColor DarkGray
}

# Create Uninstall script inside Install Directory
$uninstallScript = @"
Write-Host 'Uninstalling Kids Coloring Book...' -ForegroundColor Yellow
`$installDir = Split-Path -Parent `$MyInvocation.MyCommand.Path
`$desktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Kids Coloring Book.lnk'
if (Test-Path `$desktopLnk) { Remove-Item `$desktopLnk -Force }

`$startMenuDir = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Kids Coloring Book'
if (Test-Path `$startMenuDir) { Remove-Item `$startMenuDir -Recurse -Force }

# Remove Registry
Remove-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\KidsColoringBook' -Recurse -ErrorAction SilentlyContinue

Write-Host 'Kids Coloring Book was successfully uninstalled.' -ForegroundColor Green
"@
Set-Content -Path (Join-Path $installDir "Uninstall.ps1") -Value $uninstallScript

# Create Uninstall.bat
$uninstallBat = @"
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
cd /d "%TEMP%"
rmdir /s /q "$installDir"
echo Uninstallation complete.
pause
"@
Set-Content -Path (Join-Path $installDir "Uninstall.bat") -Value $uninstallBat

# Register in Windows Add/Remove Programs (Registry)
try {
    $regKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\KidsColoringBook"
    New-Item -Path $regKey -Force | Out-Null
    Set-ItemProperty -Path $regKey -Name "DisplayName" -Value "Kids Coloring Book"
    Set-ItemProperty -Path $regKey -Name "DisplayVersion" -Value "1.0.0"
    Set-ItemProperty -Path $regKey -Name "Publisher" -Value "Google DeepMind"
    Set-ItemProperty -Path $regKey -Name "InstallLocation" -Value $installDir
    Set-ItemProperty -Path $regKey -Name "DisplayIcon" -Value (Join-Path $installDir "KidsColoringBook.exe")
    Set-ItemProperty -Path $regKey -Name "UninstallString" -Value "`"$(Join-Path $installDir "Uninstall.bat")`""
    Set-ItemProperty -Path $regKey -Name "NoModify" -Value 1
    Set-ItemProperty -Path $regKey -Name "NoRepair" -Value 1
} catch {
    Write-Host "Skipped Windows registry entry (restricted environment): $_" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Installation Succeeded!                                " -ForegroundColor Green
Write-Host "  Launch application from Desktop or Start Menu:          " -ForegroundColor Yellow
Write-Host "  $(Join-Path $installDir 'KidsColoringBook.exe')         " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
