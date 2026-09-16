@echo off
title OmniHub Diagnostic Launcher
color 0F
echo ===================================================
echo       Launching OmniHub - DIAGNOSTIC MODE
echo ===================================================
echo.
cd /d "%~dp0"

if exist "omnihub-startup-error.log" del /q "omnihub-startup-error.log"

dotnet run --project src\OmniHub.UI\OmniHub.UI.csproj -c Release
set "EXITCODE=%ERRORLEVEL%"

echo.
echo ===================================================
if "%EXITCODE%"=="0" (
    echo [OK] OmniHub exited normally.
) else (
    echo [ERROR] OmniHub exited with code %EXITCODE%.
)
echo ===================================================

echo.
if exist "omnihub-startup-error.log" (
    echo [DIAGNOSTIC] Startup error log was created:
    echo %CD%\omnihub-startup-error.log
    echo.
    echo ---------------- ERROR LOG ----------------
    type "omnihub-startup-error.log"
    echo --------------------------------------------
) else (
    echo [DIAGNOSTIC] No application exception log was created.
    echo The console output above is important.
)
echo.
pause
