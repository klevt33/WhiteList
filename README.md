# WhiteList Web Access Restriction Service

A Windows Service application designed to enforce whitelist policies for web access restrictions on Windows systems.

## Overview

The WhiteList Service is a robust, production-ready Windows Service built with .NET Framework 4.8. It provides the foundational infrastructure for managing and enforcing web access restrictions through a whitelist-based approach.

## Features

- **Windows Service Architecture**: Built using System.ServiceProcess.ServiceBase for native Windows integration
- **Full Lifecycle Management**: Implements OnStart, OnStop, OnPause, OnContinue, and OnShutdown handlers
- **Event Log Integration**: All service events logged to Windows Event Viewer
- **IPC State Query Interface**: Named pipe interface for admin tools to query service state (Running, Paused, Stopped)
- **Configurable Settings**: JSON-based configuration file for service parameters
- **Automatic Recovery**: Configured to automatically restart on failure
- **Graceful Shutdown**: Waits for background tasks to complete with configurable timeout
- **System User Execution**: Runs as LocalSystem account with appropriate privileges
- **Service Dependencies**: Can be configured to depend on other services (e.g., TCP/IP)

## Project Structure

```
WhiteListService/
├── WhiteListService.csproj       # Project file (.NET Framework 4.8)
├── Program.cs                     # Service entry point
├── WhiteListService.cs            # Main service implementation (ServiceBase)
├── ProjectInstaller.cs            # Service installer (for InstallUtil)
├── ServiceConfiguration.cs        # Configuration loader and validator
├── ServiceStateIpcServer.cs       # IPC server for state queries
├── ServiceStateIpcClient.cs       # IPC client helper for admin tools
├── App.config                     # Application configuration
├── serviceconfig.json             # Service settings (JSON)
├── InstallService.bat             # Installation script
└── UninstallService.bat           # Uninstallation script
```

## Requirements

- Windows 7 or later (Windows 10/11 or Windows Server 2016+ recommended)
- .NET Framework 4.8 or later
- Administrator privileges for installation
- Visual Studio 2019 or later (for building from source)

## Building the Service

### Using Visual Studio

1. Open `WhiteListService.sln` in Visual Studio
2. Select **Release** configuration
3. Build the solution (Build → Build Solution or Ctrl+Shift+B)
4. Output will be in `WhiteListService\bin\Release\`

### Using MSBuild (Command Line)

```cmd
cd /d "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin"
msbuild.exe "C:\path\to\WhiteListService.sln" /p:Configuration=Release /p:Platform="Any CPU"
```

Or use Developer Command Prompt:

```cmd
msbuild WhiteListService.sln /p:Configuration=Release
```

## Installation

### Method 1: Using Installation Scripts (Recommended)

1. Build the service in **Release** configuration
2. Navigate to `WhiteListService\bin\Release\`
3. Right-click **InstallService.bat** and select **Run as administrator**
4. Follow the on-screen prompts

The script will:
- Verify administrator privileges
- Locate the appropriate .NET Framework InstallUtil
- Stop any existing service instance
- Install the service using InstallUtil
- Configure automatic startup and failure recovery
- Start the service

### Method 2: Using InstallUtil Directly

```cmd
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319
InstallUtil.exe "C:\path\to\WhiteListService.exe"
```

### Method 3: Using sc.exe

```cmd
sc create WhiteListAccessService binPath= "C:\path\to\WhiteListService.exe" start= auto
sc description WhiteListAccessService "Enforces whitelist policies for web access restrictions."
sc start WhiteListAccessService
```

## Uninstallation

### Using Uninstall Script (Recommended)

1. Navigate to the service installation directory
2. Right-click **UninstallService.bat** and select **Run as administrator**
3. Follow the on-screen prompts

### Using InstallUtil

```cmd
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319
InstallUtil.exe /u "C:\path\to\WhiteListService.exe"
```

### Using sc.exe

```cmd
sc stop WhiteListAccessService
sc delete WhiteListAccessService
```

## Configuration

### serviceconfig.json

The service is configured via `serviceconfig.json` in the same directory as the executable:

```json
{
  "serviceName": "WhiteListAccessService",
  "displayName": "WhiteList Web Access Restriction Service",
  "description": "Enforces whitelist policies for web access restrictions on the local system.",
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

#### Configuration Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `serviceName` | Windows service name (internal) | WhiteListAccessService |
| `displayName` | Display name in Services.msc | WhiteList Web Access Restriction Service |
| `description` | Service description | Enforces whitelist policies... |
| `eventLogName` | Event log name | Application |
| `eventLogSource` | Event log source name | WhiteListAccessService |
| `shutdownTimeoutSeconds` | Max wait time for graceful shutdown | 30 |
| `backgroundWorkIntervalSeconds` | Interval between background tasks | 60 |
| `failureRestartDelaySeconds` | Delay before restart after failure | 60 |
| `failureResetPeriodHours` | Period to reset failure count | 24 |
| `delayedAutoStart` | Delayed automatic start | false |
| `dependencies` | Service dependencies | ["Tcpip"] |

**Note**: Changes to `serviceName`, `displayName`, or `description` require service reinstallation.

## Service Management

### Using Services Console (services.msc)

1. Press `Win+R`, type `services.msc`, press Enter
2. Find "WhiteList Web Access Restriction Service"
3. Right-click for options: Start, Stop, Pause, Resume, Restart

### Using Command Line

```cmd
REM Query service status
sc query WhiteListAccessService

REM Start service
sc start WhiteListAccessService

REM Stop service
sc stop WhiteListAccessService

REM Pause service
sc pause WhiteListAccessService

REM Resume service
sc continue WhiteListAccessService

REM Get detailed config
sc qc WhiteListAccessService
```

### Using PowerShell

```powershell
# Get service status
Get-Service -Name WhiteListAccessService

# Start service
Start-Service -Name WhiteListAccessService

# Stop service
Stop-Service -Name WhiteListAccessService

# Restart service
Restart-Service -Name WhiteListAccessService

# Get detailed info
Get-Service -Name WhiteListAccessService | Select-Object *
```

## Viewing Logs

The service logs all events to the Windows Event Viewer.

### Using Event Viewer GUI

1. Press `Win+R`, type `eventvwr.msc`, press Enter
2. Navigate to **Windows Logs → Application**
3. Filter by source: **WhiteListAccessService**

### Using PowerShell

```powershell
# View recent service logs
Get-EventLog -LogName Application -Source WhiteListAccessService -Newest 50 | Format-Table TimeGenerated, EntryType, Message -AutoSize

# View errors only
Get-EventLog -LogName Application -Source WhiteListAccessService -EntryType Error -Newest 20
```

### Using wevtutil (Command Line)

```cmd
wevtutil qe Application "/q:*[System[Provider[@Name='WhiteListAccessService']]]" /c:50 /f:text
```

## Querying Service State (IPC Interface)

The service provides an IPC (Inter-Process Communication) interface via Named Pipes that allows admin tools to query the service state programmatically. This interface is accessible only to administrators and the SYSTEM account.

### Using the IPC Client (C# Code)

```csharp
using WhiteListService;

// Create IPC client
var client = new ServiceStateIpcClient();

// Query service status
string status = client.GetServiceStatus();
Console.WriteLine(status);
// Output example: "OK:Running|CanPause=True|CanStop=True|CanShutdown=True"

// Ping test (check if service is responding)
bool isResponding = client.Ping();
Console.WriteLine($"Service responding: {isResponding}");

// Async versions
string statusAsync = await client.GetServiceStatusAsync();
bool isRespondingAsync = await client.PingAsync();
```

### IPC Protocol

The IPC interface uses Named Pipe: `\\.\pipe\WhiteListServiceStateQuery`

**Supported Commands:**
- `STATUS` - Returns service status and capabilities
- `PING` - Returns `PONG` if service is responding

**Response Format:**
- Success: `OK:<ServiceStatus>|CanPause=<bool>|CanStop=<bool>|CanShutdown=<bool>`
- Error: `ERROR:<message>`

**Example Responses:**
- `OK:Running|CanPause=True|CanStop=True|CanShutdown=True`
- `OK:Paused|CanPause=True|CanStop=True|CanShutdown=True`
- `ERROR:Service not found`

**Service Status Values:**
- `Stopped` - Service is stopped
- `StartPending` - Service is starting
- `StopPending` - Service is stopping
- `Running` - Service is running
- `ContinuePending` - Service is resuming from pause
- `PausePending` - Service is pausing
- `Paused` - Service is paused (web access restriction disabled)

**Security:**
- Pipe is accessible only to Administrators and SYSTEM account
- Connection timeout: 5 seconds (configurable via client constructor)

### Integration Example

Admin tools can integrate the IPC client to monitor or control the service:

```csharp
public class ServiceMonitor
{
    private readonly ServiceStateIpcClient _client = new ServiceStateIpcClient();
    
    public bool IsServiceEnabled()
    {
        try
        {
            var status = _client.GetServiceStatus();
            if (status.StartsWith("OK:"))
            {
                var parts = status.Substring(3).Split('|');
                var state = parts[0];
                return state == "Running";
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    public bool IsServicePaused()
    {
        try
        {
            var status = _client.GetServiceStatus();
            return status.Contains("Paused");
        }
        catch
        {
            return false;
        }
    }
}
```

## Service Behavior

### Startup
- Service starts automatically on system boot (unless configured for delayed start)
- Loads configuration from `serviceconfig.json`
- Creates/verifies Event Log source
- Initializes background worker thread
- Starts IPC server for state queries
- Logs startup event

### Running
- Executes background work at configured intervals
- Respects pause/continue commands (immediate effect, no restart required)
- IPC server responds to state queries from admin tools
- Handles exceptions gracefully
- Logs important events and errors

### Shutdown
- Stops IPC server
- Cancels background operations
- Waits for tasks to complete (up to configured timeout)
- Cleans up resources
- Logs shutdown event

### Pause/Resume
- When paused, background work is suspended (web access restriction disabled)
- When resumed, background work continues (web access restriction re-enabled)
- Service remains running during pause
- State changes take effect immediately (no restart required)
- IPC interface remains available during pause to report status

### Failure Recovery
- Automatically restarts after failure
- Configurable restart delay (default: 60 seconds)
- Failure counter resets after 24 hours (configurable)
- Up to 3 automatic restart attempts

## Architecture

### Service Lifecycle

```
System Boot → OnStart() → Background Worker Loop
                ↓
         [Service Running]
                ↓
         OnPause() ⇄ OnContinue()
                ↓
         OnStop() / OnShutdown()
                ↓
         Cleanup → Service Stopped
```

### Threading Model

- **Main Thread**: Service control handler (SCM communication)
- **Worker Thread**: Background task execution (async/await pattern)
- **Thread Safety**: Uses ManualResetEventSlim for pause/resume coordination

### Exception Handling

- All lifecycle methods wrapped in try-catch blocks
- Exceptions logged to Event Viewer
- Critical failures during OnStart prevent service start
- Non-critical exceptions logged without stopping service

## Troubleshooting

### Service Won't Install

**Problem**: InstallUtil fails with access denied

**Solution**: Ensure you're running as Administrator

**Problem**: "Event log source already exists"

**Solution**: Delete existing source or use a different name in config

### Service Won't Start

**Problem**: Service starts then stops immediately

**Solution**: Check Event Viewer for error details

**Problem**: Configuration file not found

**Solution**: Ensure `serviceconfig.json` is in the same directory as executable

### Service Crashes

**Problem**: Service crashes during operation

**Solution**: 
1. Check Event Viewer for exception details
2. Review `shutdownTimeoutSeconds` if crashes occur during stop
3. Verify configuration file is valid JSON

### Logs Not Appearing

**Problem**: No logs in Event Viewer

**Solution**:
1. Verify Event Log source was created (requires admin rights)
2. Check Application log with source "Application" (fallback)
3. Restart Event Viewer application

## Development

### Extending the Service

The service is designed to be extended. Key extension points:

1. **Background Work**: Modify `ExecuteServiceWorkAsync()` in `WhiteListService.cs`
2. **Configuration**: Add properties to `ServiceConfiguration.cs`
3. **Logging**: Use the protected `Log*` methods

Example:

```csharp
private Task ExecuteServiceWorkAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    // Add your custom logic here
    LogInformation("Performing custom work...");
    
    return Task.CompletedTask;
}
```

### Debugging

To debug the service:

1. Build in Debug configuration
2. Install the service
3. Attach Visual Studio debugger to the service process:
   - Debug → Attach to Process
   - Find WhiteListService.exe
   - Click Attach

Or debug during startup:

```csharp
// Add to OnStart() method
#if DEBUG
    Debugger.Launch();
#endif
```

## Security Considerations

- Service runs as **LocalSystem** account (highest privileges)
- Consider using a dedicated service account for production
- Event Log writes require appropriate permissions
- Configuration file should be protected (NTFS permissions)
- IPC interface via Named Pipes (local only, restricted to Administrators/SYSTEM)
- No network ports are opened by default

## Performance

- Minimal CPU usage during idle
- Background work interval configurable (default: 60 seconds)
- Graceful shutdown timeout prevents indefinite hangs
- No memory leaks (proper IDisposable implementation)

## License

[Add your license information here]

## Support

[Add support/contact information here]

## Version History

### Version 1.0.0
- Initial release
- Core Windows Service infrastructure
- Event log integration
- JSON configuration support
- Installer scripts
- Automatic failure recovery
- Graceful shutdown with timeout
- IPC interface for service state queries
- Pause/Resume for immediate enable/disable (no restart required)

## Contributing

[Add contribution guidelines here]
