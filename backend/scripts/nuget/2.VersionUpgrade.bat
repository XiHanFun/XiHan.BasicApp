@echo off
title VersionUpgrade

set "SCRIPT_DIR=%~dp0"

if not "%1"=="am_admin" (
    powershell -Command "Start-Process -Verb RunAs -FilePath '%0' -ArgumentList 'am_admin'"
    exit /b
)

cd /d "%SCRIPT_DIR%"

if not exist "VersionUpgrade.ps1" (
    echo [ERROR] VersionUpgrade.ps1 not found: %SCRIPT_DIR%
    goto :END
)

echo ========================================
echo  VersionUpgrade - XiHan.BasicApp
echo ========================================

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Set-Location '%CD%'; & '%SCRIPT_DIR%VersionUpgrade.ps1'"

:END
echo.
pause
