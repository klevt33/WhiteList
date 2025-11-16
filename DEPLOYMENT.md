# WhiteList Service - Deployment Guide

This guide provides step-by-step instructions for deploying the WhiteList Web Access Restriction Service to production environments.

## Pre-Deployment Checklist

- [ ] .NET Framework 4.8 installed on target system
- [ ] Administrator access to target system
- [ ] Service built in Release configuration
- [ ] Configuration file reviewed and customized
- [ ] Backup of any existing service installation
- [ ] Firewall rules reviewed (if applicable)
- [ ] Service account prepared (if not using LocalSystem)

## Build for Production

### Option 1: Visual Studio

1. Open `WhiteListService.sln` in Visual Studio
2. Select **Release** configuration from the toolbar
3. Build → Build Solution (Ctrl+Shift+B)
4. Files will be in `WhiteListService\bin\Release\`

### Option 2: Command Line (MSBuild)

```cmd
cd "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin"
msbuild.exe "C:\path\to\WhiteListService.sln" /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild /m
```

### Build Output

After building, you should have these files in `bin\Release\`:

```
WhiteListService.exe
WhiteListService.exe.config
serviceconfig.json
InstallService.bat
UninstallService.bat
```

## Deployment Methods

### Method 1: Manual Deployment (Recommended for Production)

#### Step 1: Prepare Target Directory

```cmd
mkdir "C:\Program Files\WhiteListService"
```

#### Step 2: Copy Files

Copy all files from `bin\Release\` to the target directory:

```cmd
xcopy "WhiteListService\bin\Release\*.*" "C:\Program Files\WhiteListService\" /Y /I
```

#### Step 3: Customize Configuration

Edit `C:\Program Files\WhiteListService\serviceconfig.json`:

```json
{
  "serviceName": "WhiteListAccessService",
  "displayName": "WhiteList Web Access Restriction Service",
  "description": "Enforces whitelist policies for web access restrictions.",
  "eventLogName": "Application",
  "eventLogSource": "WhiteListAccessService",
  "shutdownTimeoutSeconds": 30,
  "backgroundWorkIntervalSeconds": 60,
  "failureRestartDelaySeconds": 60,
  "failureResetPeriodHours": 24,
  "delayedAutoStart": false,
  "dependencies": ["Tcpip"]
}
```

#### Step 4: Install Service

Navigate to the installation directory and run:

```cmd
cd "C:\Program Files\WhiteListService"
InstallService.bat
```

Or manually using InstallUtil:

```cmd
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319
InstallUtil.exe "C:\Program Files\WhiteListService\WhiteListService.exe"
```

#### Step 5: Configure Service Properties (Optional)

```cmd
REM Set service to auto-start
sc config WhiteListAccessService start= auto

REM Configure failure recovery (restart service on failure)
sc failure WhiteListAccessService reset= 86400 actions= restart/60000/restart/60000/restart/60000

REM Set failure flag
sc failureflag WhiteListAccessService 1

REM Add service description
sc description WhiteListAccessService "Enforces whitelist policies for web access restrictions."
```

#### Step 6: Verify Installation

```cmd
sc query WhiteListAccessService
```

#### Step 7: Start Service

```cmd
sc start WhiteListAccessService
```

Or use the Services console (services.msc).

### Method 2: Script-Based Deployment

Create a deployment script `Deploy.bat`:

```batch
@echo off
setlocal

REM Configuration
set SOURCE_DIR=WhiteListService\bin\Release
set TARGET_DIR=C:\Program Files\WhiteListService
set SERVICE_NAME=WhiteListAccessService

echo ================================================
echo WhiteList Service Deployment Script
echo ================================================
echo.

REM Check admin privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Administrator privileges required
    pause
    exit /b 1
)

REM Stop existing service
sc query %SERVICE_NAME% >nul 2>&1
if %errorlevel% equ 0 (
    echo Stopping existing service...
    sc stop %SERVICE_NAME%
    timeout /t 5 /nobreak >nul
)

REM Create target directory
if not exist "%TARGET_DIR%" (
    echo Creating installation directory...
    mkdir "%TARGET_DIR%"
)

REM Backup existing config (if exists)
if exist "%TARGET_DIR%\serviceconfig.json" (
    echo Backing up configuration...
    copy "%TARGET_DIR%\serviceconfig.json" "%TARGET_DIR%\serviceconfig.json.backup"
)

REM Copy files
echo Copying files...
xcopy "%SOURCE_DIR%\*.*" "%TARGET_DIR%\" /Y /I

REM Restore config from backup if it was newer
if exist "%TARGET_DIR%\serviceconfig.json.backup" (
    echo Note: Configuration backup available at serviceconfig.json.backup
)

REM Install or update service
echo Installing service...
cd /d "%TARGET_DIR%"
call InstallService.bat

echo.
echo ================================================
echo Deployment Complete
echo ================================================
pause
```

### Method 3: Silent Installation (Unattended)

For automated/silent deployment:

```cmd
REM Copy files
xcopy "WhiteListService\bin\Release\*.*" "C:\Program Files\WhiteListService\" /Y /I /Q

REM Install silently
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /LogToConsole=false "C:\Program Files\WhiteListService\WhiteListService.exe"

REM Configure and start
sc config WhiteListAccessService start= auto
sc failure WhiteListAccessService reset= 86400 actions= restart/60000/restart/60000/restart/60000
sc start WhiteListAccessService
```

## Configuration Management

### Environment-Specific Configurations

Create separate configuration files for each environment:

- `serviceconfig.dev.json`
- `serviceconfig.staging.json`
- `serviceconfig.prod.json`

Deploy the appropriate configuration:

```cmd
copy serviceconfig.prod.json "C:\Program Files\WhiteListService\serviceconfig.json"
```

### Configuration Best Practices

1. **Production Settings**:
   ```json
   {
     "shutdownTimeoutSeconds": 60,
     "backgroundWorkIntervalSeconds": 30,
     "failureRestartDelaySeconds": 120,
     "delayedAutoStart": true
   }
   ```

2. **Staging Settings**:
   ```json
   {
     "shutdownTimeoutSeconds": 30,
     "backgroundWorkIntervalSeconds": 60,
     "failureRestartDelaySeconds": 60,
     "delayedAutoStart": false
   }
   ```

3. **Development Settings**:
   ```json
   {
     "shutdownTimeoutSeconds": 10,
     "backgroundWorkIntervalSeconds": 120,
     "failureRestartDelaySeconds": 30,
     "delayedAutoStart": false
   }
   ```

## Service Account Configuration

### Using LocalSystem (Default)

The service runs as LocalSystem by default. No additional configuration needed.

**Pros**: Full system access, no password management
**Cons**: Highest privilege level, security risk

### Using Dedicated Service Account (Recommended for Production)

#### Step 1: Create Service Account

```powershell
# Create local user account
$Password = ConvertTo-SecureString "YourSecurePassword!" -AsPlainText -Force
New-LocalUser -Name "WhiteListServiceAccount" -Password $Password -FullName "WhiteList Service Account" -Description "Service account for WhiteList Service"

# Set password to never expire
Set-LocalUser -Name "WhiteListServiceAccount" -PasswordNeverExpires $true
```

#### Step 2: Grant Required Permissions

```cmd
REM Grant Log on as a service right
ntrights +r SeServiceLogonRight -u WhiteListServiceAccount

REM Grant permissions to installation directory
icacls "C:\Program Files\WhiteListService" /grant "WhiteListServiceAccount:(OI)(CI)RX" /T
```

#### Step 3: Install with Custom Account

Modify `ProjectInstaller.cs` before building:

```csharp
_serviceProcessInstaller = new ServiceProcessInstaller
{
    Account = ServiceAccount.User,
    Username = @".\WhiteListServiceAccount",
    Password = "YourSecurePassword!"
};
```

Or configure after installation:

```cmd
sc config WhiteListAccessService obj= ".\WhiteListServiceAccount" password= "YourSecurePassword!"
```

## Monitoring and Verification

### Post-Deployment Checks

1. **Verify Service Status**:
   ```cmd
   sc query WhiteListAccessService
   ```
   
   Expected output:
   ```
   STATE              : 4  RUNNING
   ```

2. **Check Event Logs**:
   ```powershell
   Get-EventLog -LogName Application -Source WhiteListAccessService -Newest 10
   ```
   
   Look for "Service started successfully" message.

3. **Verify Auto-Start Configuration**:
   ```cmd
   sc qc WhiteListAccessService
   ```
   
   Check `START_TYPE` is `AUTO_START`.

4. **Test Pause/Resume**:
   ```cmd
   sc pause WhiteListAccessService
   timeout /t 2 /nobreak
   sc continue WhiteListAccessService
   ```

5. **Test Stop/Start**:
   ```cmd
   sc stop WhiteListAccessService
   timeout /t 5 /nobreak
   sc start WhiteListAccessService
   ```

### Health Monitoring Script

Create `CheckServiceHealth.ps1`:

```powershell
$ServiceName = "WhiteListAccessService"
$Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($null -eq $Service) {
    Write-Host "ERROR: Service not installed" -ForegroundColor Red
    exit 1
}

Write-Host "Service Status: $($Service.Status)" -ForegroundColor $(if ($Service.Status -eq 'Running') { 'Green' } else { 'Red' })
Write-Host "Service Start Mode: $($Service.StartType)" -ForegroundColor Cyan

$RecentErrors = Get-EventLog -LogName Application -Source $ServiceName -EntryType Error -Newest 5 -ErrorAction SilentlyContinue
if ($RecentErrors) {
    Write-Host "`nRecent Errors:" -ForegroundColor Yellow
    $RecentErrors | ForEach-Object {
        Write-Host "  $($_.TimeGenerated): $($_.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "`nNo recent errors" -ForegroundColor Green
}
```

Run with:
```cmd
powershell -ExecutionPolicy Bypass -File CheckServiceHealth.ps1
```

## Updating/Upgrading Service

### Update Procedure

1. **Stop the service**:
   ```cmd
   sc stop WhiteListAccessService
   ```

2. **Backup current installation**:
   ```cmd
   xcopy "C:\Program Files\WhiteListService" "C:\Backup\WhiteListService-%date:~-4,4%%date:~-10,2%%date:~-7,2%\" /E /I /Y
   ```

3. **Backup configuration**:
   ```cmd
   copy "C:\Program Files\WhiteListService\serviceconfig.json" "C:\Backup\serviceconfig.json.backup"
   ```

4. **Copy new files** (preserving configuration):
   ```cmd
   xcopy "WhiteListService\bin\Release\WhiteListService.exe" "C:\Program Files\WhiteListService\" /Y
   xcopy "WhiteListService\bin\Release\WhiteListService.exe.config" "C:\Program Files\WhiteListService\" /Y
   REM Do NOT overwrite serviceconfig.json
   ```

5. **Restart service**:
   ```cmd
   sc start WhiteListAccessService
   ```

6. **Verify**:
   ```cmd
   sc query WhiteListAccessService
   Get-EventLog -LogName Application -Source WhiteListAccessService -Newest 5
   ```

### Rolling Back

If issues occur after update:

```cmd
REM Stop service
sc stop WhiteListAccessService

REM Restore from backup
xcopy "C:\Backup\WhiteListService-20240101\*.*" "C:\Program Files\WhiteListService\" /E /I /Y

REM Start service
sc start WhiteListAccessService
```

## Uninstallation

### Clean Uninstall Procedure

1. **Stop the service**:
   ```cmd
   sc stop WhiteListAccessService
   ```

2. **Uninstall using script**:
   ```cmd
   cd "C:\Program Files\WhiteListService"
   UninstallService.bat
   ```

3. **Remove installation directory**:
   ```cmd
   rmdir /S /Q "C:\Program Files\WhiteListService"
   ```

4. **Clean up Event Log source** (if not done automatically):
   ```powershell
   Remove-EventLog -Source WhiteListAccessService
   ```

## Troubleshooting Deployment Issues

### Issue: InstallUtil Not Found

**Solution**: Install .NET Framework 4.8 Developer Pack or locate existing InstallUtil:

```cmd
dir /s /b C:\Windows\Microsoft.NET\InstallUtil.exe
```

### Issue: Access Denied During Installation

**Solution**: 
- Run command prompt as Administrator
- Check NTFS permissions on installation directory
- Disable antivirus temporarily

### Issue: Service Installs But Won't Start

**Solution**:
1. Check Event Viewer for error details
2. Verify all dependencies are present
3. Check configuration file is valid JSON
4. Ensure .NET Framework 4.8 is installed

### Issue: Configuration Not Loading

**Solution**:
- Verify `serviceconfig.json` is in same directory as executable
- Validate JSON syntax using online validator
- Check file permissions

## Security Hardening

### File System Permissions

```cmd
REM Remove inherited permissions
icacls "C:\Program Files\WhiteListService" /inheritance:r

REM Grant admin full control
icacls "C:\Program Files\WhiteListService" /grant "Administrators:(OI)(CI)F"

REM Grant SYSTEM full control
icacls "C:\Program Files\WhiteListService" /grant "SYSTEM:(OI)(CI)F"

REM Grant service account read/execute
icacls "C:\Program Files\WhiteListService" /grant "WhiteListServiceAccount:(OI)(CI)RX"

REM Protect configuration file
icacls "C:\Program Files\WhiteListService\serviceconfig.json" /grant "Administrators:F" /grant "SYSTEM:F" /grant "WhiteListServiceAccount:R"
```

### Audit Logging

Enable audit logging for the service directory:

```cmd
auditpol /set /subcategory:"File System" /success:enable /failure:enable
```

## Multi-Server Deployment

For deploying to multiple servers, use:

### Option 1: PowerShell Remoting

```powershell
$Servers = @("Server01", "Server02", "Server03")
$SourcePath = "\\FileShare\WhiteListService"

foreach ($Server in $Servers) {
    Invoke-Command -ComputerName $Server -ScriptBlock {
        param($Source)
        
        # Copy files
        Copy-Item -Path "$Source\*" -Destination "C:\Program Files\WhiteListService\" -Recurse -Force
        
        # Install service
        & "C:\Program Files\WhiteListService\InstallService.bat"
        
        # Start service
        Start-Service -Name WhiteListAccessService
    } -ArgumentList $SourcePath
}
```

### Option 2: Group Policy Software Deployment

1. Create MSI installer package (using WiX Toolset or similar)
2. Deploy via Group Policy:
   - Computer Configuration → Software Settings → Software Installation
   - New Package → Select MSI

## Compliance and Documentation

### Change Log

Maintain a change log for each deployment:

```
Date: 2024-01-15
Version: 1.0.0
Deployed By: John Doe
Environment: Production
Changes:
- Initial production deployment
- Configured for 30-second intervals
- Running as LocalSystem account
Notes: No issues during deployment
```

### Deployment Checklist

- [ ] Pre-deployment backup completed
- [ ] Service built in Release configuration
- [ ] Configuration reviewed and approved
- [ ] Installation directory created
- [ ] Files copied to target location
- [ ] Service installed successfully
- [ ] Service started successfully
- [ ] Event logs verified
- [ ] Health check passed
- [ ] Documentation updated
- [ ] Change log updated
- [ ] Stakeholders notified

## Support

For deployment issues or questions, contact:
- [Technical Support Email]
- [Support Phone Number]
- [Internal Wiki/Documentation]
