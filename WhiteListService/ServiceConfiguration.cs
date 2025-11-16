using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WhiteListService
{
    [DataContract]
    public class ServiceConfiguration
    {
        private const string ConfigurationFileName = "serviceconfig.json";
        private const string DefaultServiceName = "WhiteListAccessService";
        private const string DefaultDisplayName = "WhiteList Web Access Restriction Service";
        private const string DefaultDescription = "Enforces whitelist policies for web access restrictions.";
        private const string DefaultEventLogName = "Application";
        private const string DefaultEventLogSource = DefaultServiceName;
        private const int DefaultShutdownTimeoutSeconds = 30;
        private const int MinimumShutdownTimeoutSeconds = 5;
        private const int DefaultBackgroundIntervalSeconds = 60;
        private const int MinimumBackgroundIntervalSeconds = 5;
        private const int DefaultFailureRestartDelaySeconds = 60;
        private const int MinimumFailureRestartDelaySeconds = 15;
        private const int DefaultFailureResetPeriodHours = 24;

        [DataMember(Name = "serviceName")]
        public string ServiceName { get; set; } = DefaultServiceName;

        [DataMember(Name = "displayName")]
        public string DisplayName { get; set; } = DefaultDisplayName;

        [DataMember(Name = "description")]
        public string Description { get; set; } = DefaultDescription;

        [DataMember(Name = "eventLogName")]
        public string EventLogName { get; set; } = DefaultEventLogName;

        [DataMember(Name = "eventLogSource")]
        public string EventLogSource { get; set; } = DefaultEventLogSource;

        [DataMember(Name = "shutdownTimeoutSeconds")]
        public int ShutdownTimeoutSeconds { get; set; } = DefaultShutdownTimeoutSeconds;

        [DataMember(Name = "backgroundWorkIntervalSeconds")]
        public int BackgroundWorkIntervalSeconds { get; set; } = DefaultBackgroundIntervalSeconds;

        [DataMember(Name = "failureRestartDelaySeconds")]
        public int FailureRestartDelaySeconds { get; set; } = DefaultFailureRestartDelaySeconds;

        [DataMember(Name = "failureResetPeriodHours")]
        public int FailureResetPeriodHours { get; set; } = DefaultFailureResetPeriodHours;

        [DataMember(Name = "delayedAutoStart")]
        public bool DelayedAutoStart { get; set; }
            = false;

        [DataMember(Name = "dependencies")]
        public string[] Dependencies { get; set; } = Array.Empty<string>();

        public static string ConfigurationPath
        {
            get
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDirectory, ConfigurationFileName);
            }
        }

        public static ServiceConfiguration Load(out string? diagnosticMessage)
        {
            diagnosticMessage = null;

            try
            {
                var filePath = ConfigurationPath;
                if (!File.Exists(filePath))
                {
                    diagnosticMessage = $"Configuration file '{ConfigurationFileName}' was not found. Using default settings.";
                    return CreateDefault();
                }

                using (var stream = File.OpenRead(filePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(ServiceConfiguration));
                    var config = serializer.ReadObject(stream) as ServiceConfiguration;
                    if (config == null)
                    {
                        diagnosticMessage = "Configured settings could not be deserialized. Using default settings.";
                        return CreateDefault();
                    }

                    config.ApplyDefaults();
                    return config;
                }
            }
            catch (Exception ex)
            {
                diagnosticMessage = $"Failed to load service configuration: {ex.Message}. Using default settings.";
                return CreateDefault();
            }
        }

        public TimeSpan ShutdownTimeout => TimeSpan.FromSeconds(ShutdownTimeoutSeconds);

        public TimeSpan BackgroundWorkInterval => TimeSpan.FromSeconds(BackgroundWorkIntervalSeconds);

        public TimeSpan FailureRestartDelay => TimeSpan.FromSeconds(FailureRestartDelaySeconds);

        public TimeSpan FailureResetPeriod => TimeSpan.FromHours(FailureResetPeriodHours);

        private static ServiceConfiguration CreateDefault()
        {
            return new ServiceConfiguration
            {
                ServiceName = DefaultServiceName,
                DisplayName = DefaultDisplayName,
                Description = DefaultDescription,
                EventLogName = DefaultEventLogName,
                EventLogSource = DefaultEventLogSource,
                ShutdownTimeoutSeconds = DefaultShutdownTimeoutSeconds,
                BackgroundWorkIntervalSeconds = DefaultBackgroundIntervalSeconds,
                FailureRestartDelaySeconds = DefaultFailureRestartDelaySeconds,
                FailureResetPeriodHours = DefaultFailureResetPeriodHours,
                Dependencies = new[] { "Tcpip" }
            };
        }

        private void ApplyDefaults()
        {
            ServiceName = NormalizeText(ServiceName, DefaultServiceName);
            DisplayName = NormalizeText(DisplayName, DefaultDisplayName);
            Description = NormalizeText(Description, DefaultDescription);
            EventLogName = NormalizeText(EventLogName, DefaultEventLogName);
            EventLogSource = NormalizeText(EventLogSource, DefaultEventLogSource);

            ShutdownTimeoutSeconds = NormalizePositiveInt(ShutdownTimeoutSeconds, MinimumShutdownTimeoutSeconds, DefaultShutdownTimeoutSeconds);
            BackgroundWorkIntervalSeconds = NormalizePositiveInt(BackgroundWorkIntervalSeconds, MinimumBackgroundIntervalSeconds, DefaultBackgroundIntervalSeconds);
            FailureRestartDelaySeconds = NormalizePositiveInt(FailureRestartDelaySeconds, MinimumFailureRestartDelaySeconds, DefaultFailureRestartDelaySeconds);
            FailureResetPeriodHours = FailureResetPeriodHours < 0 ? DefaultFailureResetPeriodHours : FailureResetPeriodHours;

            Dependencies = Dependencies == null
                ? Array.Empty<string>()
                : Dependencies
                    .Where(dependency => !string.IsNullOrWhiteSpace(dependency))
                    .Select(dependency => dependency.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
        }

        private static string NormalizeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int NormalizePositiveInt(int value, int minimum, int fallback)
        {
            if (value < minimum)
            {
                return fallback;
            }

            return value;
        }
    }
}
