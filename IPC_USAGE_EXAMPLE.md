# IPC Interface Usage Examples

This document provides examples of how to use the WhiteList Service IPC interface for querying service state from admin tools.

## Overview

The WhiteList Service exposes an IPC (Inter-Process Communication) interface via Named Pipes that allows admin tools to query the service state programmatically. The interface is secure and accessible only to administrators and the SYSTEM account.

**Named Pipe:** `\\.\pipe\WhiteListServiceStateQuery`

## Quick Start with C# Client

### Basic Usage

```csharp
using WhiteListService;

class Program
{
    static void Main()
    {
        var client = new ServiceStateIpcClient();
        
        // Check if service is responding
        if (client.Ping())
        {
            Console.WriteLine("Service is responding");
            
            // Get service status
            string status = client.GetServiceStatus();
            Console.WriteLine($"Status: {status}");
        }
        else
        {
            Console.WriteLine("Service is not responding");
        }
    }
}
```

### Async Usage

```csharp
using WhiteListService;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new ServiceStateIpcClient();
        
        // Check if service is responding (async)
        bool isResponding = await client.PingAsync();
        Console.WriteLine($"Service responding: {isResponding}");
        
        if (isResponding)
        {
            // Get service status (async)
            string status = await client.GetServiceStatusAsync();
            Console.WriteLine($"Status: {status}");
            
            // Parse response
            if (status.StartsWith("OK:"))
            {
                var parts = status.Substring(3).Split('|');
                Console.WriteLine($"Service State: {parts[0]}");
                
                foreach (var part in parts.Skip(1))
                {
                    Console.WriteLine($"  {part}");
                }
            }
        }
    }
}
```

## Response Format

### STATUS Command Response

**Success Format:** `OK:<ServiceStatus>|CanPause=<bool>|CanStop=<bool>|CanShutdown=<bool>`

**Example:**
```
OK:Running|CanPause=True|CanStop=True|CanShutdown=True
```

**Service Status Values:**
- `Stopped` - Service is stopped
- `StartPending` - Service is starting
- `StopPending` - Service is stopping
- `Running` - Service is running (web access restriction enabled)
- `ContinuePending` - Service is resuming from pause
- `PausePending` - Service is pausing
- `Paused` - Service is paused (web access restriction disabled)

### PING Command Response

**Success:** `PONG`

### Error Response

**Format:** `ERROR:<message>`

**Example:**
```
ERROR:Service not found
```

## Advanced Integration Examples

### Service Monitor Class

```csharp
using WhiteListService;
using System;

public class WhiteListServiceMonitor
{
    private readonly ServiceStateIpcClient _client;
    
    public WhiteListServiceMonitor()
    {
        _client = new ServiceStateIpcClient(TimeSpan.FromSeconds(5));
    }
    
    public ServiceState GetCurrentState()
    {
        try
        {
            var status = _client.GetServiceStatus();
            
            if (!status.StartsWith("OK:"))
            {
                return ServiceState.Unknown;
            }
            
            var parts = status.Substring(3).Split('|');
            var state = parts[0];
            
            switch (state)
            {
                case "Running":
                    return ServiceState.Running;
                case "Paused":
                    return ServiceState.Paused;
                case "Stopped":
                    return ServiceState.Stopped;
                case "StartPending":
                case "ContinuePending":
                    return ServiceState.Starting;
                case "StopPending":
                case "PausePending":
                    return ServiceState.Stopping;
                default:
                    return ServiceState.Unknown;
            }
        }
        catch
        {
            return ServiceState.Unknown;
        }
    }
    
    public bool IsEnabled()
    {
        return GetCurrentState() == ServiceState.Running;
    }
    
    public bool IsDisabled()
    {
        var state = GetCurrentState();
        return state == ServiceState.Paused || state == ServiceState.Stopped;
    }
}

public enum ServiceState
{
    Unknown,
    Running,
    Paused,
    Stopped,
    Starting,
    Stopping
}
```

### Polling for State Changes

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

public class ServiceStatePoller
{
    private readonly ServiceStateIpcClient _client = new ServiceStateIpcClient();
    
    public async Task MonitorServiceAsync(CancellationToken cancellationToken)
    {
        string lastStatus = string.Empty;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var status = await _client.GetServiceStatusAsync();
                
                if (status != lastStatus)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Status changed: {status}");
                    lastStatus = status;
                    
                    // Trigger your event handlers here
                    OnServiceStateChanged(status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying service: {ex.Message}");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
    
    private void OnServiceStateChanged(string newStatus)
    {
        // Your custom logic here
        if (newStatus.Contains("Running"))
        {
            Console.WriteLine("Web access restriction is now ENABLED");
        }
        else if (newStatus.Contains("Paused"))
        {
            Console.WriteLine("Web access restriction is now DISABLED");
        }
    }
}
```

## PowerShell Integration

While the C# client is recommended, you can also interact with Named Pipes from PowerShell:

```powershell
# Note: This is a basic example. For production, use the C# client.

function Get-WhiteListServiceStatus {
    try {
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "WhiteListServiceStateQuery", [System.IO.Pipes.PipeDirection]::InOut)
        $pipe.Connect(5000)
        
        $writer = New-Object System.IO.StreamWriter($pipe)
        $writer.AutoFlush = $true
        $reader = New-Object System.IO.StreamReader($pipe)
        
        $writer.WriteLine("STATUS")
        $response = $reader.ReadLine()
        
        $reader.Close()
        $writer.Close()
        $pipe.Close()
        
        return $response
    }
    catch {
        Write-Error "Failed to query service: $_"
        return $null
    }
}

# Usage
$status = Get-WhiteListServiceStatus
Write-Host "Service Status: $status"
```

## Error Handling

Always implement proper error handling when using the IPC client:

```csharp
using WhiteListService;
using System;

public bool CheckServiceIsRunning()
{
    try
    {
        var client = new ServiceStateIpcClient(TimeSpan.FromSeconds(3));
        var status = client.GetServiceStatus();
        
        if (status.StartsWith("ERROR:"))
        {
            Console.WriteLine($"Service returned error: {status}");
            return false;
        }
        
        return status.Contains("Running");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Timeout connecting to service");
        return false;
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Access denied - administrator privileges required");
        return false;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Communication error: {ex.Message}");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
        return false;
    }
}
```

## Security Considerations

1. **Administrator Privileges Required**: The IPC interface is restricted to Administrators and SYSTEM account only.

2. **Local Machine Only**: The Named Pipe is only accessible from the local machine.

3. **Timeout Protection**: The client has a default 5-second timeout to prevent hanging.

4. **No Sensitive Data**: The interface only exposes service state information, no configuration or sensitive data.

## Troubleshooting

### Connection Timeout

**Problem:** Client times out connecting to the pipe.

**Solutions:**
- Verify the service is running: `sc query WhiteListAccessService`
- Check Event Viewer for service errors
- Ensure you're running as Administrator

### Access Denied

**Problem:** UnauthorizedAccessException when connecting.

**Solutions:**
- Run your application as Administrator
- Check that the service is running as SYSTEM
- Verify Windows security policies haven't restricted Named Pipes

### Service Not Found

**Problem:** STATUS command returns "ERROR:Service not found"

**Solutions:**
- Verify service is installed: `sc query WhiteListAccessService`
- Check the service name matches the configuration
- Reinstall the service if necessary

## Best Practices

1. **Use Async Methods**: For UI applications, use the async methods to prevent blocking.

2. **Handle Timeouts**: Always configure appropriate timeouts for your use case.

3. **Implement Retry Logic**: For monitoring applications, implement exponential backoff retry.

4. **Cache Results**: Don't poll too frequently; cache results for 1-2 seconds.

5. **Error Logging**: Log all communication errors for troubleshooting.

6. **Dispose Properly**: The client is lightweight, but dispose of instances in long-running apps.

## Example: Admin CLI Tool

Here's a complete example of a simple CLI tool to query service state:

```csharp
using System;
using WhiteListService;

class ServiceStatusCLI
{
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "status")
        {
            ShowStatus();
        }
        else if (args.Length > 0 && args[0] == "monitor")
        {
            MonitorService();
        }
        else
        {
            Console.WriteLine("WhiteList Service Status Tool");
            Console.WriteLine("Usage:");
            Console.WriteLine("  servicestatus status   - Show current status");
            Console.WriteLine("  servicestatus monitor  - Monitor status (Ctrl+C to exit)");
        }
    }
    
    static void ShowStatus()
    {
        var client = new ServiceStateIpcClient();
        
        try
        {
            var status = client.GetServiceStatus();
            
            if (status.StartsWith("OK:"))
            {
                var parts = status.Substring(3).Split('|');
                Console.WriteLine($"Service State: {parts[0]}");
                Console.WriteLine("Capabilities:");
                
                foreach (var capability in parts.Skip(1))
                {
                    Console.WriteLine($"  {capability}");
                }
                
                if (parts[0] == "Running")
                {
                    Console.WriteLine("\nWeb access restriction is ENABLED");
                }
                else if (parts[0] == "Paused")
                {
                    Console.WriteLine("\nWeb access restriction is DISABLED (paused)");
                }
            }
            else
            {
                Console.WriteLine($"Error: {status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to query service: {ex.Message}");
        }
    }
    
    static void MonitorService()
    {
        var client = new ServiceStateIpcClient();
        string lastStatus = string.Empty;
        
        Console.WriteLine("Monitoring service status (press Ctrl+C to exit)...\n");
        
        while (true)
        {
            try
            {
                var status = client.GetServiceStatus();
                
                if (status != lastStatus)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {status}");
                    lastStatus = status;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
            }
            
            System.Threading.Thread.Sleep(2000);
        }
    }
}
```

## Conclusion

The IPC interface provides a simple, secure way for admin tools to query the WhiteList Service state. Use the provided `ServiceStateIpcClient` class for easy integration into your applications.
