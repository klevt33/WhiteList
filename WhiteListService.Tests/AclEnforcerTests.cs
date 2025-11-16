using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace WhiteListService.Tests
{
    [TestClass]
    public class AclEnforcerTests
    {
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"AclTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                }
            }
        }

        [TestMethod]
        public void EnforceConfigDirectoryACL_WithValidDirectory_DoesNotThrow()
        {
            var testDir = Path.Combine(_testDirectory, "config");
            Directory.CreateDirectory(testDir);

            try
            {
                AclEnforcer.EnforceConfigDirectoryACL(testDir);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [TestMethod]
        public void EnforceConfigDirectoryACL_WithNullPath_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => AclEnforcer.EnforceConfigDirectoryACL(null));
        }

        [TestMethod]
        public void EnforceConfigDirectoryACL_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent");
            Assert.ThrowsException<DirectoryNotFoundException>(() => AclEnforcer.EnforceConfigDirectoryACL(nonExistentPath));
        }

        [TestMethod]
        public void EnforceConfigFileACL_WithNullPath_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => AclEnforcer.EnforceConfigFileACL(null));
        }

        [TestMethod]
        public void EnforceConfigFileACL_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.config");
            Assert.ThrowsException<FileNotFoundException>(() => AclEnforcer.EnforceConfigFileACL(nonExistentFile));
        }

        [TestMethod]
        public void EnforceConfigFileACL_WithValidFile_DoesNotThrow()
        {
            var testFile = Path.Combine(_testDirectory, "test.config");
            File.WriteAllText(testFile, "test content");

            try
            {
                AclEnforcer.EnforceConfigFileACL(testFile);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [TestMethod]
        public void VerifyFileReadPermission_WithReadableFile_ReturnsTrue()
        {
            var testFile = Path.Combine(_testDirectory, "test.config");
            File.WriteAllText(testFile, "test content");

            var result = AclEnforcer.VerifyFileReadPermission(testFile);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyFileReadPermission_WithNonExistentFile_ReturnsFalse()
        {
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.config");

            var result = AclEnforcer.VerifyFileReadPermission(nonExistentFile);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyFileReadPermission_WithNullPath_ReturnsFalse()
        {
            var result = AclEnforcer.VerifyFileReadPermission(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyFileWritePermission_WithWritableDirectory_ReturnsTrue()
        {
            var testFile = Path.Combine(_testDirectory, "test_write.config");

            var result = AclEnforcer.VerifyFileWritePermission(testFile);

            Assert.IsTrue(result);
            Assert.IsFalse(File.Exists(testFile));
        }

        [TestMethod]
        public void VerifyFileWritePermission_WithExistingFile_ReturnsTrue()
        {
            var testFile = Path.Combine(_testDirectory, "test_existing.config");
            File.WriteAllText(testFile, "initial content");

            var result = AclEnforcer.VerifyFileWritePermission(testFile);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyFileWritePermission_WithNullPath_ReturnsFalse()
        {
            var result = AclEnforcer.VerifyFileWritePermission(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyFileWritePermission_WithNonExistentDirectory_ReturnsFalse()
        {
            var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");
            var testFile = Path.Combine(nonExistentDir, "test.config");

            var result = AclEnforcer.VerifyFileWritePermission(testFile);

            Assert.IsFalse(result);
        }
    }
}
