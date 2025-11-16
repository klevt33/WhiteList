using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace WhiteListService
{
    [Serializable]
    public class EncryptedConfig
    {
        public string WhitelistStoreEncrypted { get; set; } = string.Empty;

        public string AdminCredentialsEncrypted { get; set; } = string.Empty;

        public string ConfigurationHash { get; set; } = string.Empty;

        public int Version { get; set; } = 1;
    }

    public class ConfigManager : IDisposable
    {
        private const string EncryptedConfigFileName = "encrypted.config";
        private const int DefaultEncryptionWorkFactor = 12;
        private const bool UseUserScope = false;

        private readonly string _configDirectory;
        private readonly ReaderWriterLockSlim _configLock;
        private EncryptedConfig _currentConfig;
        private WhitelistStore _whitelistStore;
        private AdminCredentials _adminCredentials;
        private bool _disposed;

        public ConfigManager() : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public ConfigManager(string configDirectory)
        {
            if (string.IsNullOrEmpty(configDirectory))
            {
                throw new ArgumentNullException(nameof(configDirectory));
            }

            _configDirectory = configDirectory;
            _configLock = new ReaderWriterLockSlim();
            _currentConfig = new EncryptedConfig();
            _whitelistStore = new WhitelistStore();
            _adminCredentials = new AdminCredentials();

            Load();
        }

        public string EncryptedConfigPath
        {
            get { return Path.Combine(_configDirectory, EncryptedConfigFileName); }
        }

        public bool Load(out string? diagnosticMessage)
        {
            diagnosticMessage = null;

            try
            {
                _configLock.EnterWriteLock();

                var filePath = EncryptedConfigPath;
                if (!File.Exists(filePath))
                {
                    diagnosticMessage = "Encrypted configuration file not found. Using default empty configuration.";
                    _whitelistStore = new WhitelistStore();
                    _adminCredentials = new AdminCredentials();
                    _currentConfig = new EncryptedConfig();
                    return true;
                }

                using (var stream = File.OpenRead(filePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(EncryptedConfig));
                    var config = serializer.ReadObject(stream) as EncryptedConfig;

                    if (config == null)
                    {
                        diagnosticMessage = "Failed to deserialize encrypted configuration. Using default empty configuration.";
                        _whitelistStore = new WhitelistStore();
                        _adminCredentials = new AdminCredentials();
                        _currentConfig = new EncryptedConfig();
                        return false;
                    }

                    if (!VerifyConfigIntegrity(config, out var integrityError))
                    {
                        diagnosticMessage = $"Configuration integrity check failed: {integrityError}. Using default empty configuration.";
                        _whitelistStore = new WhitelistStore();
                        _adminCredentials = new AdminCredentials();
                        _currentConfig = new EncryptedConfig();
                        return false;
                    }

                    _currentConfig = config;

                    try
                    {
                        var whitelistJson = DpapiEncryptor.Decrypt(config.WhitelistStoreEncrypted, UseUserScope);
                        var whitelistSerializer = new DataContractJsonSerializer(typeof(WhitelistStore));
                        using (var whitelistStream = new MemoryStream(Encoding.UTF8.GetBytes(whitelistJson)))
                        {
                            _whitelistStore = whitelistSerializer.ReadObject(whitelistStream) as WhitelistStore
                                ?? new WhitelistStore();
                        }
                    }
                    catch (Exception ex)
                    {
                        diagnosticMessage = $"Failed to decrypt whitelist store: {ex.Message}. Using empty whitelist (all blocked).";
                        _whitelistStore = new WhitelistStore();
                        return false;
                    }

                    try
                    {
                        var credentialsJson = DpapiEncryptor.Decrypt(config.AdminCredentialsEncrypted, UseUserScope);
                        var credentialsSerializer = new DataContractJsonSerializer(typeof(AdminCredentials));
                        using (var credentialsStream = new MemoryStream(Encoding.UTF8.GetBytes(credentialsJson)))
                        {
                            _adminCredentials = credentialsSerializer.ReadObject(credentialsStream) as AdminCredentials
                                ?? new AdminCredentials();
                        }
                    }
                    catch (Exception ex)
                    {
                        diagnosticMessage = $"Failed to decrypt admin credentials: {ex.Message}. Admin password cleared.";
                        _adminCredentials = new AdminCredentials();
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                diagnosticMessage = $"Unexpected error loading configuration: {ex.Message}. Using default empty configuration.";
                _whitelistStore = new WhitelistStore();
                _adminCredentials = new AdminCredentials();
                _currentConfig = new EncryptedConfig();
                return false;
            }
            finally
            {
                if (_configLock.IsWriteLockHeld)
                {
                    _configLock.ExitWriteLock();
                }
            }
        }

        public void Load()
        {
            Load(out _);
        }

        public bool Save(out string? diagnosticMessage)
        {
            diagnosticMessage = null;

            try
            {
                _configLock.EnterWriteLock();

                var whitelistJson = SerializeToJson(_whitelistStore);
                var credentialsJson = SerializeToJson(_adminCredentials);

                var encryptedWhitelist = DpapiEncryptor.Encrypt(whitelistJson, UseUserScope);
                var encryptedCredentials = DpapiEncryptor.Encrypt(credentialsJson, UseUserScope);

                var configData = whitelistJson + credentialsJson;
                var hash = IntegrityVerifier.ComputeHash(configData);

                var newConfig = new EncryptedConfig
                {
                    WhitelistStoreEncrypted = encryptedWhitelist,
                    AdminCredentialsEncrypted = encryptedCredentials,
                    ConfigurationHash = hash,
                    Version = 1
                };

                var filePath = EncryptedConfigPath;
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = File.Create(filePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(EncryptedConfig));
                    serializer.WriteObject(stream, newConfig);
                }

                _currentConfig = newConfig;
                return true;
            }
            catch (Exception ex)
            {
                diagnosticMessage = $"Failed to save configuration: {ex.Message}";
                return false;
            }
            finally
            {
                if (_configLock.IsWriteLockHeld)
                {
                    _configLock.ExitWriteLock();
                }
            }
        }

        public WhitelistStore GetWhitelist()
        {
            try
            {
                _configLock.EnterReadLock();
                return new WhitelistStore(_whitelistStore.Domains);
            }
            finally
            {
                if (_configLock.IsReadLockHeld)
                {
                    _configLock.ExitReadLock();
                }
            }
        }

        public void UpdateWhitelist(WhitelistStore whitelist)
        {
            if (whitelist == null)
            {
                throw new ArgumentNullException(nameof(whitelist));
            }

            try
            {
                _configLock.EnterWriteLock();
                _whitelistStore = new WhitelistStore(whitelist.Domains);
            }
            finally
            {
                if (_configLock.IsWriteLockHeld)
                {
                    _configLock.ExitWriteLock();
                }
            }
        }

        public void AddWhitelistDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            try
            {
                _configLock.EnterWriteLock();
                _whitelistStore.AddDomain(domain);
            }
            finally
            {
                if (_configLock.IsWriteLockHeld)
                {
                    _configLock.ExitWriteLock();
                }
            }
        }

        public void RemoveWhitelistDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            try
            {
                _configLock.EnterWriteLock();
                _whitelistStore.RemoveDomain(domain);
            }
            finally
            {
                if (_configLock.IsWriteLockHeld)
                {
                    _configLock.ExitWriteLock();
                }
            }
        }

        public AdminCredentials GetAdminCredentials()
        {
            try
            {
                _configLock.EnterReadLock();
                return new AdminCredentials(_adminCredentials.PasswordHash);
            }
            finally
            {
                if (_configLock.IsReadLockHeld)
                {
                    _configLock.ExitReadLock();
                }
            }
        }

        public void SetAdminPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            try
            {
                _configLock.EnterWriteLock();
                _adminCredentials = AdminCredentials.CreateFromPassword(password);
            }
            finally
            {
                if (_configLock.IsWriteLockHeld)
                {
                    _configLock.ExitWriteLock();
                }
            }
        }

        public bool VerifyAdminPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            try
            {
                _configLock.EnterReadLock();
                return _adminCredentials.VerifyPassword(password);
            }
            finally
            {
                if (_configLock.IsReadLockHeld)
                {
                    _configLock.ExitReadLock();
                }
            }
        }

        public bool IsAdminPasswordSet()
        {
            try
            {
                _configLock.EnterReadLock();
                return _adminCredentials.IsPasswordSet();
            }
            finally
            {
                if (_configLock.IsReadLockHeld)
                {
                    _configLock.ExitReadLock();
                }
            }
        }

        private static bool VerifyConfigIntegrity(EncryptedConfig config, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (config == null)
            {
                errorMessage = "Configuration is null";
                return false;
            }

            if (string.IsNullOrEmpty(config.WhitelistStoreEncrypted))
            {
                errorMessage = "Whitelist store is missing";
                return false;
            }

            if (string.IsNullOrEmpty(config.AdminCredentialsEncrypted))
            {
                errorMessage = "Admin credentials are missing";
                return false;
            }

            if (string.IsNullOrEmpty(config.ConfigurationHash))
            {
                errorMessage = "Configuration hash is missing";
                return false;
            }

            try
            {
                var whitelistJson = DpapiEncryptor.Decrypt(config.WhitelistStoreEncrypted, UseUserScope);
                var credentialsJson = DpapiEncryptor.Decrypt(config.AdminCredentialsEncrypted, UseUserScope);

                var configData = whitelistJson + credentialsJson;
                var computedHash = IntegrityVerifier.ComputeHash(configData);

                if (!string.Equals(computedHash, config.ConfigurationHash, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Configuration hash mismatch - possible corruption";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to verify integrity: {ex.Message}";
                return false;
            }
        }

        private static string SerializeToJson(object obj)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(stream, obj);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _configLock?.Dispose();
            _disposed = true;
        }
    }
}
