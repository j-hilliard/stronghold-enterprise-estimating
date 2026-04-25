#Requires -Version 5.1
$ErrorActionPreference = 'Continue'
$AppRoot = $PSScriptRoot

Clear-Host
Write-Host ""
Write-Host "  STRONGHOLD ENTERPRISE ESTIMATING  -- Starting Up" -ForegroundColor Cyan
Write-Host ""

# ── SQL Express ───────────────────────────────────────────────────────────────
Write-Host "  Checking SQL Server Express..." -ForegroundColor Yellow
$svc = Get-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
if ($null -eq $svc) {
    Write-Host "  ERROR: SQL Server Express not found." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
if ($svc.Status -ne 'Running') {
    Write-Host "  Starting SQL Server Express..." -ForegroundColor Yellow
    Start-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 4
}
Write-Host "  SQL Server Express: OK" -ForegroundColor Green

# ── Start API window ──────────────────────────────────────────────────────────
Write-Host "  Starting API..." -ForegroundColor Yellow
Start-Process powershell `
    -WorkingDirectory $AppRoot `
    -ArgumentList "-NoExit", "-Command", "dotnet run --project Api --launch-profile https"

# ── Start Vue window ──────────────────────────────────────────────────────────
Write-Host "  Starting Vue dev server..." -ForegroundColor Yellow
Start-Process powershell `
    -WorkingDirectory "$AppRoot\webapp" `
    -ArgumentList "-NoExit", "-Command", "npm run dev"

# ── Wait for API ──────────────────────────────────────────────────────────────
Write-Host "  Waiting for API on https://localhost:7211 ..." -ForegroundColor Yellow
Add-Type -AssemblyName System.Net.Http
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
$http = [System.Net.Http.HttpClient]::new($handler)
$http.Timeout = [TimeSpan]::FromSeconds(3)
$apiReady = $false
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 2
    try {
        $body = [System.Net.Http.StringContent]::new('{"username":"x","password":"x"}', [System.Text.Encoding]::UTF8, 'application/json')
        $resp = $http.PostAsync('https://localhost:7211/api/auth/login', $body).GetAwaiter().GetResult()
        $code = [int]$resp.StatusCode
        if ($code -ge 200 -and $code -lt 600) { $apiReady = $true; break }
    } catch { }
}
$http.Dispose()

if ($apiReady) {
    Write-Host "  API: Ready" -ForegroundColor Green
} else {
    Write-Host "  API may still be starting -- opening browser anyway" -ForegroundColor Yellow
}

# ── Wait for Vue ──────────────────────────────────────────────────────────────
Write-Host "  Waiting for Vue on https://localhost:7210 ..." -ForegroundColor Yellow
$handler2 = [System.Net.Http.HttpClientHandler]::new()
$handler2.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
$http2 = [System.Net.Http.HttpClient]::new($handler2)
$http2.Timeout = [TimeSpan]::FromSeconds(3)
$viteReady = $false
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 2
    try {
        $null = $http2.GetAsync('https://localhost:7210').GetAwaiter().GetResult()
        $viteReady = $true; break
    } catch { }
}
$http2.Dispose()

if ($viteReady) {
    Write-Host "  Vue: Ready" -ForegroundColor Green
} else {
    Write-Host "  Vue may still be starting -- opening browser anyway" -ForegroundColor Yellow
}

# ── Open browser ──────────────────────────────────────────────────────────────
Start-Process "https://localhost:7210"

Write-Host ""
Write-Host "  App:  https://localhost:7210" -ForegroundColor Green
Write-Host "  API:  https://localhost:7211" -ForegroundColor Green
Write-Host ""
Write-Host "  Both servers are running in their own windows." -ForegroundColor DarkGray
Write-Host "  Close those windows to shut them down." -ForegroundColor DarkGray
Write-Host ""
Read-Host "Press Enter to close this launcher"
