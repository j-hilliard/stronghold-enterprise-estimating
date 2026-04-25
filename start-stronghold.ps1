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

# ── Start Cloudflare tunnel ───────────────────────────────────────────────────
Write-Host "  Starting Cloudflare tunnel..." -ForegroundColor Yellow
Stop-Process -Name "cloudflared" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$cfLog = "$env:TEMP\cf-tunnel-$((Get-Date).Ticks).log"
Start-Process powershell `
    -WindowStyle Minimized `
    -ArgumentList "-NoExit", "-Command", "& 'C:\Program Files (x86)\cloudflared\cloudflared.exe' tunnel --url https://localhost:7211 2>&1 | Tee-Object -FilePath '$cfLog'"

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

# ── Grab tunnel URL from log ──────────────────────────────────────────────────
Write-Host "  Waiting for tunnel URL..." -ForegroundColor Yellow
$tunnelUrl = $null
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 2
    if (Test-Path $cfLog) {
        $tunnelUrl = Select-String -Path $cfLog -Pattern "trycloudflare\.com" |
            Select-Object -First 1 |
            ForEach-Object { ($_.Line -replace '.*https://', 'https://') -replace '\s.*', '' }
        if ($tunnelUrl) { break }
    }
}

# ── Push tunnel URL to Cloudflare Worker secret ───────────────────────────────
$cfAccountId  = "95714f404b7d0fdfc428c954c99b46a0"
$cfApiToken   = $env:CF_API_TOKEN   # set this in Windows Environment Variables
$cfWorkerName = "stronghold-agent"

if ($tunnelUrl -and $cfApiToken) {
    Write-Host "  Updating Worker TUNNEL_URL → $tunnelUrl ..." -ForegroundColor Yellow
    try {
        $body = (@{ name = "TUNNEL_URL"; text = $tunnelUrl; type = "secret_text" } | ConvertTo-Json)
        $resp = Invoke-RestMethod `
            -Uri "https://api.cloudflare.com/client/v4/accounts/$cfAccountId/workers/scripts/$cfWorkerName/secrets" `
            -Method PUT `
            -Headers @{ Authorization = "Bearer $cfApiToken"; "Content-Type" = "application/json" } `
            -Body $body
        if ($resp.success) {
            Write-Host "  Worker secret updated — permanent URL active." -ForegroundColor Green
        } else {
            Write-Host "  Worker update returned: $($resp | ConvertTo-Json -Compress)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  Worker secret update failed: $_" -ForegroundColor Yellow
    }
} elseif ($tunnelUrl -and -not $cfApiToken) {
    Write-Host "  Skipping Worker update — CF_API_TOKEN env var not set." -ForegroundColor DarkGray
}

# ── Open browser ──────────────────────────────────────────────────────────────
Start-Process "https://localhost:7210"

Write-Host ""
Write-Host "  App:  https://localhost:7210" -ForegroundColor Green
Write-Host "  API:  https://localhost:7211" -ForegroundColor Green
Write-Host ""
Write-Host "  ╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║  AI FOUNDRY → EstimatingAgent URL (PERMANENT — never changes)   ║" -ForegroundColor Cyan
Write-Host "  ╠═══════════════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
Write-Host "  ║  https://stronghold-agent.j-travishilliard.workers.dev          ║" -ForegroundColor Yellow
Write-Host "  ╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
if (-not $tunnelUrl) {
    Write-Host "  WARNING: Tunnel URL not detected — check minimized Cloudflare window." -ForegroundColor Red
}
Write-Host ""
Write-Host "  Both servers are running in their own windows." -ForegroundColor DarkGray
Write-Host "  Close those windows to shut them down." -ForegroundColor DarkGray
Write-Host ""
Read-Host "Press Enter to close this launcher"
