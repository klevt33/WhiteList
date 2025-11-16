using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WhiteListService.Tests
{
    [TestClass]
    public class DpapiEncryptorTests
    {
        [TestMethod]
        public void Encrypt_WithValidPlainText_ReturnsEncryptedString()
        {
            var plainText = "sensitive_data_to_encrypt";
            
            var encrypted = DpapiEncryptor.Encrypt(plainText);
            
            Assert.IsFalse(string.IsNullOrEmpty(encrypted));
            Assert.AreNotEqual(plainText, encrypted);
        }

        [TestMethod]
        public void Decrypt_WithValidEncryptedText_ReturnsOriginalPlainText()
        {
            var plainText = "sensitive_data_to_encrypt";
            var encrypted = DpapiEncryptor.Encrypt(plainText);
            
            var decrypted = DpapiEncryptor.Decrypt(encrypted);
            
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void Encrypt_WithNullPlainText_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DpapiEncryptor.Encrypt(null));
        }

        [TestMethod]
        public void Encrypt_WithEmptyPlainText_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DpapiEncryptor.Encrypt(string.Empty));
        }

        [TestMethod]
        public void Decrypt_WithNullEncryptedText_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DpapiEncryptor.Decrypt(null));
        }

        [TestMethod]
        public void Decrypt_WithEmptyEncryptedText_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => DpapiEncryptor.Decrypt(string.Empty));
        }

        [TestMethod]
        public void Decrypt_WithInvalidBase64_ThrowsInvalidOperationException()
        {
            Assert.ThrowsException<InvalidOperationException>(() => DpapiEncryptor.Decrypt("not-valid-base64!!!"));
        }

        [TestMethod]
        public void EncryptDecrypt_WithMultipleCalls_AllReturnConsistentResults()
        {
            var plainText = "test_data";
            
            var encrypted1 = DpapiEncryptor.Encrypt(plainText);
            var encrypted2 = DpapiEncryptor.Encrypt(plainText);
            
            var decrypted1 = DpapiEncryptor.Decrypt(encrypted1);
            var decrypted2 = DpapiEncryptor.Decrypt(encrypted2);
            
            Assert.AreEqual(plainText, decrypted1);
            Assert.AreEqual(plainText, decrypted2);
            Assert.AreEqual(decrypted1, decrypted2);
        }

        [TestMethod]
        public void EncryptDecrypt_WithSpecialCharacters_PreservesContent()
        {
            var plainText = "Special chars: !@#$%^&*()_+-=[]{}|;':\"<>?,./";
            
            var encrypted = DpapiEncryptor.Encrypt(plainText);
            var decrypted = DpapiEncryptor.Decrypt(encrypted);
            
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EncryptDecrypt_WithUnicodeCharacters_PreservesContent()
        {
            var plainText = "Unicode: 你好世界 Привет مرحبا";
            
            var encrypted = DpapiEncryptor.Encrypt(plainText);
            var decrypted = DpapiEncryptor.Decrypt(encrypted);
            
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EncryptDecrypt_WithLargeText_PreservesContent()
        {
            var plainText = new string('A', 10000);
            
            var encrypted = DpapiEncryptor.Encrypt(plainText);
            var decrypted = DpapiEncryptor.Decrypt(encrypted);
            
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void Encrypt_WithUserScope_ProducesEncryptedString()
        {
            var plainText = "user_scoped_data";
            
            var encrypted = DpapiEncryptor.Encrypt(plainText, useUserScope: true);
            
            Assert.IsFalse(string.IsNullOrEmpty(encrypted));
            Assert.AreNotEqual(plainText, encrypted);
        }

        [TestMethod]
        public void Decrypt_WithUserScope_ReturnsOriginalPlainText()
        {
            var plainText = "user_scoped_data";
            var encrypted = DpapiEncryptor.Encrypt(plainText, useUserScope: true);
            
            var decrypted = DpapiEncryptor.Decrypt(encrypted, useUserScope: true);
            
            Assert.AreEqual(plainText, decrypted);
        }
    }
}
