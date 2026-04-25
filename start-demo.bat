@echo off
echo Starting Stronghold API + Cloudflare tunnel...

:: Start API
start "Stronghold API" /D "%~dp0Api" cmd /c "set ASPNETCORE_ENVIRONMENT=Local && dotnet run"

:: Wait for API to be ready
timeout /t 10 /nobreak >nul

:: Start tunnel and capture URL
start "Cloudflare Tunnel" cmd /c ""C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --url https://localhost:7211 2>&1 | tee %TEMP%\cf-tunnel.log"

echo.
echo API starting on https://localhost:7211
echo Tunnel starting... check the "Cloudflare Tunnel" window for the trycloudflare.com URL
echo Copy that URL and update the Foundry tool server URL.
echo.
pause
