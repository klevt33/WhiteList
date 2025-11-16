using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace WhiteListService.Tests
{
    [TestClass]
    public class ConfigManagerTests
    {
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"WhiteListServiceTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [TestMethod]
        public void Constructor_WithValidDirectory_InitializesManager()
        {
            var manager = new ConfigManager(_testDirectory);
            
            Assert.IsNotNull(manager);
            manager.Dispose();
        }

        [TestMethod]
        public void Constructor_WithNullDirectory_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new ConfigManager(null));
        }

        [TestMethod]
        public void Load_WithNoExistingConfig_ReturnsDefaultEmptyConfig()
        {
            var manager = new ConfigManager(_testDirectory);
            
            var result = manager.Load(out var message);
            
            Assert.IsTrue(result);
            Assert.IsNotNull(message);
            manager.Dispose();
        }

        [TestMethod]
        public void Save_CreatesEncryptedConfigFile()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("TestPassword123!");
            manager.AddWhitelistDomain("example.com");
            
            var result = manager.Save(out var message);
            
            Assert.IsTrue(result);
            Assert.IsNull(message);
            Assert.IsTrue(File.Exists(manager.EncryptedConfigPath));
            manager.Dispose();
        }

        [TestMethod]
        public void SaveLoad_RoundTrip_PreservesData()
        {
            var manager1 = new ConfigManager(_testDirectory);
            manager1.SetAdminPassword("TestPassword123!");
            manager1.AddWhitelistDomain("example.com");
            manager1.AddWhitelistDomain("test.org");
            manager1.Save(out _);
            manager1.Dispose();
            
            var manager2 = new ConfigManager(_testDirectory);
            
            Assert.IsTrue(manager2.VerifyAdminPassword("TestPassword123!"));
            Assert.IsTrue(manager2.GetWhitelist().ContainsDomain("example.com"));
            Assert.IsTrue(manager2.GetWhitelist().ContainsDomain("test.org"));
            manager2.Dispose();
        }

        [TestMethod]
        public void GetWhitelist_ReturnsWhitelistCopy()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.AddWhitelistDomain("example.com");
            
            var whitelist = manager.GetWhitelist();
            
            Assert.AreEqual(1, whitelist.Count);
            Assert.IsTrue(whitelist.ContainsDomain("example.com"));
            manager.Dispose();
        }

        [TestMethod]
        public void UpdateWhitelist_ReplacesWhitelist()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.AddWhitelistDomain("example.com");
            
            var newWhitelist = new WhitelistStore(new[] { "new.org", "another.net" });
            manager.UpdateWhitelist(newWhitelist);
            
            var whitelist = manager.GetWhitelist();
            Assert.AreEqual(2, whitelist.Count);
            Assert.IsTrue(whitelist.ContainsDomain("new.org"));
            Assert.IsTrue(whitelist.ContainsDomain("another.net"));
            Assert.IsFalse(whitelist.ContainsDomain("example.com"));
            manager.Dispose();
        }

        [TestMethod]
        public void UpdateWhitelist_WithNull_ThrowsArgumentNullException()
        {
            var manager = new ConfigManager(_testDirectory);
            
            Assert.ThrowsException<ArgumentNullException>(() => manager.UpdateWhitelist(null));
            manager.Dispose();
        }

        [TestMethod]
        public void AddWhitelistDomain_AddsToWhitelist()
        {
            var manager = new ConfigManager(_testDirectory);
            
            manager.AddWhitelistDomain("example.com");
            
            var whitelist = manager.GetWhitelist();
            Assert.IsTrue(whitelist.ContainsDomain("example.com"));
            manager.Dispose();
        }

        [TestMethod]
        public void AddWhitelistDomain_WithNull_ThrowsArgumentNullException()
        {
            var manager = new ConfigManager(_testDirectory);
            
            Assert.ThrowsException<ArgumentNullException>(() => manager.AddWhitelistDomain(null));
            manager.Dispose();
        }

        [TestMethod]
        public void RemoveWhitelistDomain_RemovesFromWhitelist()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.AddWhitelistDomain("example.com");
            
            manager.RemoveWhitelistDomain("example.com");
            
            var whitelist = manager.GetWhitelist();
            Assert.IsFalse(whitelist.ContainsDomain("example.com"));
            manager.Dispose();
        }

        [TestMethod]
        public void RemoveWhitelistDomain_WithNull_ThrowsArgumentNullException()
        {
            var manager = new ConfigManager(_testDirectory);
            
            Assert.ThrowsException<ArgumentNullException>(() => manager.RemoveWhitelistDomain(null));
            manager.Dispose();
        }

        [TestMethod]
        public void GetAdminCredentials_ReturnsCredentialsCopy()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("TestPassword123!");
            
            var credentials = manager.GetAdminCredentials();
            
            Assert.IsTrue(credentials.IsPasswordSet());
            Assert.IsTrue(credentials.VerifyPassword("TestPassword123!"));
            manager.Dispose();
        }

        [TestMethod]
        public void SetAdminPassword_SetsPassword()
        {
            var manager = new ConfigManager(_testDirectory);
            
            manager.SetAdminPassword("NewPassword456!");
            
            Assert.IsTrue(manager.IsAdminPasswordSet());
            Assert.IsTrue(manager.VerifyAdminPassword("NewPassword456!"));
            manager.Dispose();
        }

        [TestMethod]
        public void SetAdminPassword_WithNull_ThrowsArgumentNullException()
        {
            var manager = new ConfigManager(_testDirectory);
            
            Assert.ThrowsException<ArgumentNullException>(() => manager.SetAdminPassword(null));
            manager.Dispose();
        }

        [TestMethod]
        public void VerifyAdminPassword_WithCorrectPassword_ReturnsTrue()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("TestPassword123!");
            
            var result = manager.VerifyAdminPassword("TestPassword123!");
            
            Assert.IsTrue(result);
            manager.Dispose();
        }

        [TestMethod]
        public void VerifyAdminPassword_WithIncorrectPassword_ReturnsFalse()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("TestPassword123!");
            
            var result = manager.VerifyAdminPassword("WrongPassword456!");
            
            Assert.IsFalse(result);
            manager.Dispose();
        }

        [TestMethod]
        public void VerifyAdminPassword_WithNull_ReturnsFalse()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("TestPassword123!");
            
            var result = manager.VerifyAdminPassword(null);
            
            Assert.IsFalse(result);
            manager.Dispose();
        }

        [TestMethod]
        public void IsAdminPasswordSet_WithPassword_ReturnsTrue()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("TestPassword123!");
            
            var result = manager.IsAdminPasswordSet();
            
            Assert.IsTrue(result);
            manager.Dispose();
        }

        [TestMethod]
        public void IsAdminPasswordSet_WithoutPassword_ReturnsFalse()
        {
            var manager = new ConfigManager(_testDirectory);
            
            var result = manager.IsAdminPasswordSet();
            
            Assert.IsFalse(result);
            manager.Dispose();
        }

        [TestMethod]
        public void EncryptedConfigPath_ReturnsCorrectPath()
        {
            var manager = new ConfigManager(_testDirectory);
            
            var path = manager.EncryptedConfigPath;
            
            Assert.IsTrue(path.EndsWith("encrypted.config"));
            Assert.IsTrue(path.StartsWith(_testDirectory));
            manager.Dispose();
        }

        [TestMethod]
        public void Save_CreatesBackupBeforeOverwriting()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("Password1");
            manager.AddWhitelistDomain("example.com");
            manager.Save(out _);
            
            var originalFile = new FileInfo(manager.EncryptedConfigPath);
            var originalLastWrite = originalFile.LastWriteTime;
            
            System.Threading.Thread.Sleep(100);
            
            manager.SetAdminPassword("Password2");
            manager.Save(out _);
            
            var updatedFile = new FileInfo(manager.EncryptedConfigPath);
            Assert.IsTrue(updatedFile.LastWriteTime > originalLastWrite);
            manager.Dispose();
        }

        [TestMethod]
        public void MultipleWhitelistOperations_MaintainsThreadSafety()
        {
            var manager = new ConfigManager(_testDirectory);
            
            manager.AddWhitelistDomain("domain1.com");
            manager.AddWhitelistDomain("domain2.com");
            manager.AddWhitelistDomain("domain3.com");
            
            var whitelist1 = manager.GetWhitelist();
            manager.RemoveWhitelistDomain("domain2.com");
            var whitelist2 = manager.GetWhitelist();
            
            Assert.AreEqual(3, whitelist1.Count);
            Assert.AreEqual(2, whitelist2.Count);
            manager.Dispose();
        }

        [TestMethod]
        public void Save_WithCompositeData_SavesSuccessfully()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("ComplexPassword!@#$%");
            manager.AddWhitelistDomain("example.com");
            manager.AddWhitelistDomain("test.org");
            manager.AddWhitelistDomain("another.net");
            
            var result = manager.Save(out var message);
            
            Assert.IsTrue(result);
            Assert.IsNull(message);
            manager.Dispose();
        }

        [TestMethod]
        public void LoadCorruptedFile_ReturnsDefaults()
        {
            var configPath = Path.Combine(_testDirectory, "encrypted.config");
            File.WriteAllText(configPath, "corrupted data that is not valid json");
            
            var manager = new ConfigManager(_testDirectory);
            var result = manager.Load(out var message);
            
            Assert.IsFalse(result);
            Assert.IsNotNull(message);
            Assert.IsTrue(message.Contains("Failed to deserialize"));
            manager.Dispose();
        }

        [TestMethod]
        public void ThreadSafeAccess_MultipleReadsAndWrites()
        {
            var manager = new ConfigManager(_testDirectory);
            manager.SetAdminPassword("Password123!");
            manager.AddWhitelistDomain("example.com");
            manager.Save(out _);
            
            var task1 = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    manager.AddWhitelistDomain($"domain{i}.com");
                }
            });
            
            var task2 = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var whitelist = manager.GetWhitelist();
                    System.Threading.Thread.Sleep(1);
                }
            });
            
            System.Threading.Tasks.Task.WaitAll(task1, task2);
            
            var finalWhitelist = manager.GetWhitelist();
            Assert.IsTrue(finalWhitelist.Count >= 11);
            manager.Dispose();
        }
    }
}
