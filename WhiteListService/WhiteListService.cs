using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace WhiteListService
{
    public class WhiteListService : ServiceBase
    {
        private readonly object _stateLock = new object();
        private readonly EventLog _eventLog;
        private ServiceConfiguration _configuration;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _workerTask;
        private ManualResetEventSlim? _pauseEvent;
        private string? _pendingDiagnosticMessage;

        public WhiteListService()
        {
            CanStop = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            AutoLog = false;

            _configuration = ServiceConfiguration.Load(out _pendingDiagnosticMessage);
            ServiceName = _configuration.ServiceName;

            _eventLog = new EventLog("Application")
            {
                Source = "Application"
            };
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                ReloadConfiguration();
                InitializeEventLog();

                LogInformation($"Service starting (PID {Process.GetCurrentProcess().Id}).");

                _pauseEvent = new ManualResetEventSlim(true);
                _cancellationTokenSource = new CancellationTokenSource();
                _workerTask = Task.Run(() => RunAsync(_cancellationTokenSource.Token));

                LogInformation("Service started successfully.");
            }
            catch (Exception ex)
            {
                LogError("Service failed to start.", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                LogInformation("Stop requested.");

                RequestCancellation();
                WaitForWorkerCompletion("stop");
            }
            catch (Exception ex)
            {
                LogError("Unexpected error during stop.", ex);
            }
            finally
            {
                Cleanup();
                LogInformation("Service stopped.");
            }
        }

        protected override void OnPause()
        {
            try
            {
                LogInformation("Pause requested.");
                _pauseEvent?.Reset();
                LogInformation("Service paused.");
            }
            catch (Exception ex)
            {
                LogError("Unexpected error while pausing.", ex);
            }
        }

        protected override void OnContinue()
        {
            try
            {
                LogInformation("Continue requested.");
                _pauseEvent?.Set();
                LogInformation("Service resumed.");
            }
            catch (Exception ex)
            {
                LogError("Unexpected error while resuming.", ex);
            }
        }

        protected override void OnShutdown()
        {
            try
            {
                LogInformation("System shutdown detected.");
                OnStop();
            }
            catch (Exception ex)
            {
                LogError("Unexpected error during system shutdown.", ex);
            }
        }

        private void ReloadConfiguration()
        {
            var configuration = ServiceConfiguration.Load(out var diagnosticMessage);
            if (!string.Equals(configuration.ServiceName, ServiceName, StringComparison.OrdinalIgnoreCase))
            {
                diagnosticMessage = $"Configured service name '{configuration.ServiceName}' does not match the installed service name '{ServiceName}'. Using '{ServiceName}'.";
                configuration.ServiceName = ServiceName;
            }

            _configuration = configuration;
            if (!string.IsNullOrWhiteSpace(diagnosticMessage))
            {
                _pendingDiagnosticMessage = diagnosticMessage;
            }
        }

        private void InitializeEventLog()
        {
            var desiredSource = string.IsNullOrWhiteSpace(_configuration.EventLogSource)
                ? _configuration.ServiceName
                : _configuration.EventLogSource;
            var desiredLog = string.IsNullOrWhiteSpace(_configuration.EventLogName)
                ? "Application"
                : _configuration.EventLogName;

            try
            {
                if (!EventLog.SourceExists(desiredSource))
                {
                    var sourceData = new EventSourceCreationData(desiredSource, desiredLog);
                    EventLog.CreateEventSource(sourceData);
                }

                _eventLog.Source = desiredSource;
                _eventLog.Log = desiredLog;
            }
            catch (Exception ex)
            {
                _eventLog.Source = "Application";
                _eventLog.Log = "Application";

                LogWarning($"Failed to initialise custom event log '{desiredLog}' with source '{desiredSource}'. Using Application log. {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(_pendingDiagnosticMessage))
            {
                LogWarning(_pendingDiagnosticMessage);
                _pendingDiagnosticMessage = null;
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            LogInformation("Background worker started.");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    WaitForResume(cancellationToken);
                    await ExecuteServiceWorkAsync(cancellationToken).ConfigureAwait(false);
                    await DelayAsync(_configuration.BackgroundWorkInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogError("Unhandled exception in background worker.", ex);
            }
            finally
            {
                LogInformation("Background worker stopped.");
            }
        }

        private void WaitForResume(CancellationToken cancellationToken)
        {
            var pauseHandle = _pauseEvent;
            if (pauseHandle == null)
            {
                return;
            }

            pauseHandle.Wait(cancellationToken);
        }

        private Task ExecuteServiceWorkAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private static Task DelayAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            if (interval <= TimeSpan.Zero)
            {
                return Task.CompletedTask;
            }

            return Task.Delay(interval, cancellationToken);
        }

        private void RequestCancellation()
        {
            _pauseEvent?.Set();
            _cancellationTokenSource?.Cancel();
        }

        private void WaitForWorkerCompletion(string operation)
        {
            var worker = _workerTask;
            if (worker == null)
            {
                return;
            }

            var timeout = _configuration.ShutdownTimeout;
            if (!worker.Wait(timeout))
            {
                LogWarning($"Background worker did not complete within {timeout.TotalSeconds} seconds during {operation}.");
            }
        }

        private void Cleanup()
        {
            lock (_stateLock)
            {
                _workerTask = null;

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                if (_pauseEvent != null)
                {
                    _pauseEvent.Dispose();
                    _pauseEvent = null;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cleanup();
                _eventLog.Dispose();
            }

            base.Dispose(disposing);
        }

        private void LogInformation(string message)
        {
            Log(EventLogEntryType.Information, message);
        }

        private void LogWarning(string message)
        {
            Log(EventLogEntryType.Warning, message);
        }

        private void LogError(string message, Exception exception)
        {
            Log(EventLogEntryType.Error, $"{message}{Environment.NewLine}{exception}");
        }

        private void Log(EventLogEntryType entryType, string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var formattedMessage = $"[{timestamp}] {message}";

                if (formattedMessage.Length > 31800)
                {
                    formattedMessage = formattedMessage.Substring(0, 31800) + "...";
                }

                _eventLog.WriteEntry(formattedMessage, entryType);
            }
            catch
            {
            }
        }
    }
}
