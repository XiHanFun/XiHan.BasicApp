@echo off
title SwitchFrameworkRef

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

if not exist "SwitchFrameworkRef.ps1" (
    echo [ERROR] SwitchFrameworkRef.ps1 not found: %SCRIPT_DIR%
    goto :END
)

echo ========================================
echo  SwitchFrameworkRef - XiHan.BasicApp
echo ========================================

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%SwitchFrameworkRef.ps1" -Mode menu

:END
echo.
pause
