using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WhiteListService
{
    public class ServiceStateIpcServer : IDisposable
    {
        private const string PipeName = "WhiteListServiceStateQuery";
        private readonly string _serviceName;
        private readonly object _disposeLock = new object();
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _disposed;

        public ServiceStateIpcServer(string serviceName)
        {
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        }

        public void Start()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ServiceStateIpcServer));
                }

                if (_cancellationTokenSource != null)
                {
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token));
            }
        }

        public void Stop()
        {
            lock (_disposeLock)
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }

                _cancellationTokenSource.Cancel();

                try
                {
                    _listenerTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _listenerTask = null;
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var pipeServer = CreateNamedPipeServer())
                    {
                        await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        await HandleClientAsync(pipeServer, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                }
            }
        }

        private NamedPipeServerStream CreateNamedPipeServer()
        {
            var pipeSecurity = new PipeSecurity();
            var adminsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);

            pipeSecurity.AddAccessRule(new PipeAccessRule(adminsSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(systemSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));

            return new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous,
                4096,
                4096,
                pipeSecurity);
        }

        private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                using (var reader = new StreamReader(pipeServer, Encoding.UTF8, false, 1024, true))
                using (var writer = new StreamWriter(pipeServer, Encoding.UTF8, 1024, true) { AutoFlush = true })
                {
                    var request = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    string response;
                    if (string.Equals(request, "STATUS", StringComparison.OrdinalIgnoreCase))
                    {
                        response = GetServiceStatus();
                    }
                    else if (string.Equals(request, "PING", StringComparison.OrdinalIgnoreCase))
                    {
                        response = "PONG";
                    }
                    else
                    {
                        response = "ERROR:UNKNOWN_COMMAND";
                    }

                    await writer.WriteLineAsync(response).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
            }
            catch (Exception)
            {
            }
        }

        private string GetServiceStatus()
        {
            try
            {
                using (var controller = new ServiceController(_serviceName))
                {
                    controller.Refresh();
                    var status = controller.Status;
                    var canPauseAndContinue = controller.CanPauseAndContinue;
                    var canStop = controller.CanStop;
                    var canShutdown = controller.CanShutdown;

                    return $"OK:{status}|CanPause={canPauseAndContinue}|CanStop={canStop}|CanShutdown={canShutdown}";
                }
            }
            catch (Exception ex)
            {
                return $"ERROR:{ex.Message}";
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Stop();
            }
        }
    }
}
