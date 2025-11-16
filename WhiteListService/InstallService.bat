@echo off
REM Install WhiteList Service using InstallUtil
REM This script must be run with Administrator privileges

setlocal enabledelayedexpansion

echo ================================================
echo WhiteList Service Installation Script
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

REM Get the service executable and configuration paths
set SERVICE_EXE=%~dp0WhiteListService.exe
set SERVICE_CONFIG=%~dp0serviceconfig.json
set SERVICE_NAME=WhiteListAccessService

for /f "usebackq tokens=*" %%i in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%SERVICE_CONFIG%') { $value = (Get-Content -Path '%SERVICE_CONFIG%' -Raw | ConvertFrom-Json).serviceName; if (-not [string]::IsNullOrWhiteSpace($value)) { $value } }"`) do set SERVICE_NAME=%%i

if not exist "%SERVICE_EXE%" (
    echo ERROR: Service executable not found at: %SERVICE_EXE%
    echo Please ensure the service is built before running this script.
    echo.
    pause
    exit /b 1
)

echo Service executable: %SERVICE_EXE%
echo Target service name: %SERVICE_NAME%
echo.

REM Stop the service if it's already running
echo Checking if service is already installed...
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo Service is already installed. Stopping...
    sc stop "%SERVICE_NAME%"
    timeout /t 5 /nobreak >nul
)

REM Install the service
echo Installing service...
%INSTALLUTIL_PATH% /ShowCallStack "%SERVICE_EXE%"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Service installation failed.
    echo Check the log file for details.
    pause
    exit /b 1
)

echo.
echo Service installed successfully!
echo.

REM Start the service
echo Starting service...
sc start "%SERVICE_NAME%"

if %errorlevel% neq 0 (
    echo.
    echo WARNING: Service was installed but failed to start.
    echo You can start it manually from services.msc
) else (
    echo Service started successfully!
)

echo.
echo ================================================
echo Installation Complete
echo ================================================
echo.
echo You can manage the service using:
echo   - services.msc (Services management console)
echo   - sc query %SERVICE_NAME%
echo   - sc stop %SERVICE_NAME%
echo   - sc start %SERVICE_NAME%
echo.

pause
