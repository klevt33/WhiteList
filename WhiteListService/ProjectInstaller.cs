using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.ServiceProcess;

namespace WhiteListService
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private readonly ServiceProcessInstaller _serviceProcessInstaller;
        private readonly ServiceInstaller _serviceInstaller;
        private readonly ServiceConfiguration _configuration;

        public ProjectInstaller()
        {
            _configuration = ServiceConfiguration.Load(out _);

            _serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem,
                Username = null,
                Password = null
            };

            _serviceInstaller = new ServiceInstaller
            {
                ServiceName = _configuration.ServiceName,
                DisplayName = _configuration.DisplayName,
                Description = _configuration.Description,
                StartType = ServiceStartMode.Automatic,
                DelayedAutoStart = _configuration.DelayedAutoStart
            };

            if (_configuration.Dependencies != null && _configuration.Dependencies.Length > 0)
            {
                _serviceInstaller.ServicesDependedOn = _configuration.Dependencies;
            }

            Installers.Add(_serviceProcessInstaller);
            Installers.Add(_serviceInstaller);
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);
            LogInstallerEvent("Starting installation.");
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);

            try
            {
                ConfigureServiceRecoveryOptions();
                EnsureAutomaticStartup();
                LogInstallerEvent("Installation completed successfully.");
            }
            catch (Exception ex)
            {
                LogInstallerEvent($"Warning: Failed to apply post-install configuration: {ex.Message}");
            }
        }

        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            base.OnBeforeUninstall(savedState);
            LogInstallerEvent("Starting uninstallation.");

            try
            {
                using (var controller = new ServiceController(_configuration.ServiceName))
                {
                    controller.Refresh();
                    if (controller.Status != ServiceControllerStatus.Stopped && controller.Status != ServiceControllerStatus.StopPending)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                LogInstallerEvent($"Warning: Failed to stop service before uninstall: {ex.Message}");
            }
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            base.OnAfterUninstall(savedState);

            try
            {
                if (!string.IsNullOrWhiteSpace(_configuration.EventLogSource) && EventLog.SourceExists(_configuration.EventLogSource))
                {
                    EventLog.DeleteEventSource(_configuration.EventLogSource);
                }

                LogInstallerEvent("Uninstallation completed successfully.");
            }
            catch (Exception ex)
            {
                LogInstallerEvent($"Warning: Failed to clean up event log source: {ex.Message}");
            }
        }

        private void ConfigureServiceRecoveryOptions()
        {
            var serviceName = _configuration.ServiceName;
            var restartDelay = (int)_configuration.FailureRestartDelay.TotalMilliseconds;
            var resetPeriod = (int)_configuration.FailureResetPeriod.TotalSeconds;

            RunScCommand($"failure \"{serviceName}\" reset= {resetPeriod} actions= restart/{restartDelay}/restart/{restartDelay}/restart/{restartDelay}");
            RunScCommand($"failureflag \"{serviceName}\" 1");
        }

        private void EnsureAutomaticStartup()
        {
            var serviceName = _configuration.ServiceName;
            RunScCommand($"config \"{serviceName}\" start= AUTO");
        }

        private static void RunScCommand(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException($"Failed to start sc.exe with arguments: {arguments}");
                }

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"sc.exe exited with code {process.ExitCode} for arguments: {arguments}");
                }
            }
        }

        private static void LogInstallerEvent(string message)
        {
            try
            {
                using (var log = new EventLog("Application"))
                {
                    log.Source = "Application";
                    log.WriteEntry($"WhiteListService Installer: {message}", EventLogEntryType.Information);
                }
            }
            catch
            {
            }
        }
    }
}
