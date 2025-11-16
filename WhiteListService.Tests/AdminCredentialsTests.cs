using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WhiteListService.Tests
{
    [TestClass]
    public class AdminCredentialsTests
    {
        [TestMethod]
        public void CreateFromPassword_WithValidPassword_CreatesCredentialsWithHash()
        {
            var password = "MySecurePassword123!";
            
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            Assert.IsNotNull(credentials);
            Assert.IsFalse(string.IsNullOrEmpty(credentials.PasswordHash));
            Assert.AreNotEqual(password, credentials.PasswordHash);
        }

        [TestMethod]
        public void CreateFromPassword_WithNullPassword_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => AdminCredentials.CreateFromPassword(null));
        }

        [TestMethod]
        public void CreateFromPassword_WithEmptyPassword_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => AdminCredentials.CreateFromPassword(string.Empty));
        }

        [TestMethod]
        public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            var result = credentials.VerifyPassword(password);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
        {
            var password = "MySecurePassword123!";
            var wrongPassword = "WrongPassword456";
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            var result = credentials.VerifyPassword(wrongPassword);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyPassword_WithNullPassword_ReturnsFalse()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            var result = credentials.VerifyPassword(null);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            var result = credentials.VerifyPassword(string.Empty);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPasswordSet_WithHashSet_ReturnsTrue()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            var result = credentials.IsPasswordSet();
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsPasswordSet_WithoutHash_ReturnsFalse()
        {
            var credentials = new AdminCredentials();
            
            var result = credentials.IsPasswordSet();
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ClearPassword_ClearsTheHash()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            credentials.ClearPassword();
            
            Assert.IsFalse(credentials.IsPasswordSet());
            Assert.IsTrue(string.IsNullOrEmpty(credentials.PasswordHash));
        }

        [TestMethod]
        public void VerifyPassword_AfterClear_ReturnsFalse()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            credentials.ClearPassword();
            
            var result = credentials.VerifyPassword(password);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void PasswordHash_IsNotPlaintext()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            
            Assert.AreNotEqual(password, credentials.PasswordHash);
            Assert.IsFalse(credentials.PasswordHash.Contains(password));
        }

        [TestMethod]
        public void CreateFromPassword_WithSamePassword_GeneratesDifferentHashes()
        {
            var password = "MySecurePassword123!";
            
            var credentials1 = AdminCredentials.CreateFromPassword(password);
            var credentials2 = AdminCredentials.CreateFromPassword(password);
            
            Assert.AreNotEqual(credentials1.PasswordHash, credentials2.PasswordHash);
        }

        [TestMethod]
        public void CreateFromPassword_DifferentHashesStillVerifyToSamePassword()
        {
            var password = "MySecurePassword123!";
            
            var credentials1 = AdminCredentials.CreateFromPassword(password);
            var credentials2 = AdminCredentials.CreateFromPassword(password);
            
            Assert.IsTrue(credentials1.VerifyPassword(password));
            Assert.IsTrue(credentials2.VerifyPassword(password));
        }

        [TestMethod]
        public void PasswordConstructor_WithValidHash_CreatesCredentials()
        {
            var password = "MySecurePassword123!";
            var credentials = AdminCredentials.CreateFromPassword(password);
            var hash = credentials.PasswordHash;
            
            var newCredentials = new AdminCredentials(hash);
            
            Assert.IsTrue(newCredentials.VerifyPassword(password));
        }

        [TestMethod]
        public void PasswordConstructor_WithNullHash_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new AdminCredentials(null));
        }

        [TestMethod]
        public void PasswordConstructor_WithEmptyHash_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new AdminCredentials(string.Empty));
        }
    }
}
