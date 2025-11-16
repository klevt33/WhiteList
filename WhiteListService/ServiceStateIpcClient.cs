using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WhiteListService
{
    public class ServiceStateIpcClient
    {
        private const string PipeName = "WhiteListServiceStateQuery";
        private readonly TimeSpan _timeout;

        public ServiceStateIpcClient() : this(TimeSpan.FromSeconds(5))
        {
        }

        public ServiceStateIpcClient(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        public async Task<string> GetServiceStatusAsync()
        {
            return await SendCommandAsync("STATUS").ConfigureAwait(false);
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                var response = await SendCommandAsync("PING").ConfigureAwait(false);
                return string.Equals(response, "PONG", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> SendCommandAsync(string command)
        {
            using (var cts = new CancellationTokenSource(_timeout))
            using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                await pipeClient.ConnectAsync((int)_timeout.TotalMilliseconds, cts.Token).ConfigureAwait(false);

                using (var writer = new StreamWriter(pipeClient, Encoding.UTF8, 1024, true) { AutoFlush = true })
                using (var reader = new StreamReader(pipeClient, Encoding.UTF8, false, 1024, true))
                {
                    await writer.WriteLineAsync(command).ConfigureAwait(false);
                    var response = await reader.ReadLineAsync().ConfigureAwait(false);
                    return response ?? string.Empty;
                }
            }
        }

        public string GetServiceStatus()
        {
            return SendCommand("STATUS");
        }

        public bool Ping()
        {
            try
            {
                var response = SendCommand("PING");
                return string.Equals(response, "PONG", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string SendCommand(string command)
        {
            using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
            {
                pipeClient.Connect((int)_timeout.TotalMilliseconds);

                using (var writer = new StreamWriter(pipeClient, Encoding.UTF8, 1024, true) { AutoFlush = true })
                using (var reader = new StreamReader(pipeClient, Encoding.UTF8, false, 1024, true))
                {
                    writer.WriteLine(command);
                    var response = reader.ReadLine();
                    return response ?? string.Empty;
                }
            }
        }
    }
}
