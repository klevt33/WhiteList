@echo off
REM Uninstall WhiteList Service using InstallUtil
REM This script must be run with Administrator privileges

setlocal enabledelayedexpansion

echo ================================================
echo WhiteList Service Uninstallation Script
echo ================================================
echo.

REM Check for admin privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script requires Administrator privileges.
    echo Please right-click and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Get service configuration
set SERVICE_CONFIG=%~dp0serviceconfig.json
set SERVICE_NAME=WhiteListAccessService

for /f "usebackq tokens=*" %%i in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%SERVICE_CONFIG%') { $value = (Get-Content -Path '%SERVICE_CONFIG%' -Raw | ConvertFrom-Json).serviceName; if (-not [string]::IsNullOrWhiteSpace($value)) { $value } }"`) do set SERVICE_NAME=%%i

echo Target service name: %SERVICE_NAME%
echo.

REM Check if service exists
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% neq 0 (
    echo Service is not installed.
    echo.
    pause
    exit /b 0
)

REM Stop the service
echo Stopping service...
sc stop "%SERVICE_NAME%"
echo Waiting for service to stop...
timeout /t 5 /nobreak >nul

REM Locate the .NET Framework InstallUtil
set INSTALLUTIL_PATH=""
set FRAMEWORK_VERSIONS=v4.0.30319 v4.5 v4.6 v4.7 v4.8

for %%v in (%FRAMEWORK_VERSIONS%) do (
    if exist "%SystemRoot%\Microsoft.NET\Framework64\%%v\InstallUtil.exe" (
        set INSTALLUTIL_PATH=%SystemRoot%\Microsoft.NET\Framework64\%%v\InstallUtil.exe
        goto :found
    )
    if exist "%SystemRoot%\Microsoft.NET\Framework\%%v\InstallUtil.exe" (
        set INSTALLUTIL_PATH=%SystemRoot%\Microsoft.NET\Framework\%%v\InstallUtil.exe
        goto :found
    )
)

:found
if %INSTALLUTIL_PATH%=="" (
    echo ERROR: Could not locate InstallUtil.exe in .NET Framework directory.
    echo Please ensure .NET Framework 4.x is installed.
    echo.
    pause
    exit /b 1
)

echo Located InstallUtil: %INSTALLUTIL_PATH%
echo.

REM Get the service executable path
set SERVICE_EXE=%~dp0WhiteListService.exe

if not exist "%SERVICE_EXE%" (
    echo ERROR: Service executable not found at: %SERVICE_EXE%
    echo Cannot uninstall without the original executable.
    echo You may need to manually remove the service using:
    echo   sc delete %SERVICE_NAME%
    echo.
    pause
    exit /b 1
)

echo Service executable: %SERVICE_EXE%
echo.

REM Uninstall the service
echo Uninstalling service...
%INSTALLUTIL_PATH% /u /ShowCallStack "%SERVICE_EXE%"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Service uninstallation failed.
    echo Check the log file for details.
    echo.
    echo You can try to manually remove the service using:
    echo   sc delete %SERVICE_NAME%
    echo.
    pause
    exit /b 1
)

echo.
echo ================================================
echo Uninstallation Complete
echo ================================================
echo Service has been removed successfully.
echo.

pause
