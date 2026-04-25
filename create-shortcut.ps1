<#
.SYNOPSIS
    Creates a desktop shortcut that launches Stronghold Enterprise Estimating.
    Run once: .\create-shortcut.ps1

    The shortcut will appear on your desktop as "Stronghold Estimating".
    Double-clicking it starts Docker + API + Vue and opens the browser.
#>

$AppRoot   = $PSScriptRoot
$Script    = Join-Path $AppRoot "start-stronghold.ps1"
$Desktop   = [Environment]::GetFolderPath('Desktop')
$Shortcut  = Join-Path $Desktop "Stronghold Estimating.lnk"
$IconPath  = Join-Path $Env:SystemRoot "System32\SHELL32.dll"
$IconIndex = 13   # briefcase icon

$WshShell = New-Object -ComObject WScript.Shell
$Link     = $WshShell.CreateShortcut($Shortcut)

$Link.TargetPath       = "powershell.exe"
$Link.Arguments        = "-NoExit -ExecutionPolicy Bypass -File `"$Script`""
$Link.WorkingDirectory = $AppRoot
$Link.Description      = "Stronghold Enterprise Estimating - Start All Services"
$Link.IconLocation     = "$IconPath,$IconIndex"
$Link.WindowStyle      = 1   # normal window

$Link.Save()

Write-Host ""
Write-Host "  Desktop shortcut created: $Shortcut" -ForegroundColor Green
Write-Host "  Double-click it to start Stronghold Estimating." -ForegroundColor Green
Write-Host ""
