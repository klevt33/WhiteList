using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace WhiteListService.Tests
{
    [TestClass]
    public class IntegrityVerifierTests
    {
        [TestMethod]
        public void ComputeHash_WithValidData_ReturnsHashString()
        {
            var data = "test_data_for_hashing";
            
            var hash = IntegrityVerifier.ComputeHash(data);
            
            Assert.IsFalse(string.IsNullOrEmpty(hash));
            Assert.AreNotEqual(data, hash);
        }

        [TestMethod]
        public void ComputeHash_WithNullData_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => IntegrityVerifier.ComputeHash(null));
        }

        [TestMethod]
        public void ComputeHash_WithEmptyData_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => IntegrityVerifier.ComputeHash(string.Empty));
        }

        [TestMethod]
        public void ComputeHash_WithSameData_ReturnsSameHash()
        {
            var data = "test_data_for_hashing";
            
            var hash1 = IntegrityVerifier.ComputeHash(data);
            var hash2 = IntegrityVerifier.ComputeHash(data);
            
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void ComputeHash_WithDifferentData_ReturnsDifferentHash()
        {
            var data1 = "test_data_1";
            var data2 = "test_data_2";
            
            var hash1 = IntegrityVerifier.ComputeHash(data1);
            var hash2 = IntegrityVerifier.ComputeHash(data2);
            
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void ComputeHash_ReturnsSha256HexString()
        {
            var data = "test_data";
            
            var hash = IntegrityVerifier.ComputeHash(data);
            
            Assert.AreEqual(64, hash.Length);
            Assert.IsTrue(IsValidHexString(hash));
        }

        [TestMethod]
        public void VerifyHash_WithCorrectHash_ReturnsTrue()
        {
            var data = "test_data_for_verification";
            var hash = IntegrityVerifier.ComputeHash(data);
            
            var result = IntegrityVerifier.VerifyHash(data, hash);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void VerifyHash_WithIncorrectHash_ReturnsFalse()
        {
            var data = "test_data_for_verification";
            var wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";
            
            var result = IntegrityVerifier.VerifyHash(data, wrongHash);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyHash_WithModifiedData_ReturnsFalse()
        {
            var data = "original_data";
            var hash = IntegrityVerifier.ComputeHash(data);
            
            var result = IntegrityVerifier.VerifyHash("modified_data", hash);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyHash_WithNullData_ReturnsFalse()
        {
            var hash = IntegrityVerifier.ComputeHash("test");
            
            var result = IntegrityVerifier.VerifyHash(null, hash);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyHash_WithNullHash_ReturnsFalse()
        {
            var result = IntegrityVerifier.VerifyHash("test_data", null);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyHash_WithEmptyData_ReturnsFalse()
        {
            var hash = IntegrityVerifier.ComputeHash("test");
            
            var result = IntegrityVerifier.VerifyHash(string.Empty, hash);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void VerifyHash_WithEmptyHash_ReturnsFalse()
        {
            var result = IntegrityVerifier.VerifyHash("test_data", string.Empty);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ComputeHashBytes_WithValidData_ReturnsHashString()
        {
            var data = Encoding.UTF8.GetBytes("test_data_for_hashing");
            
            var hash = IntegrityVerifier.ComputeHashBytes(data);
            
            Assert.IsFalse(string.IsNullOrEmpty(hash));
            Assert.AreEqual(64, hash.Length);
        }

        [TestMethod]
        public void ComputeHashBytes_WithNullData_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => IntegrityVerifier.ComputeHashBytes(null));
        }

        [TestMethod]
        public void ComputeHashBytes_WithSameData_ReturnsSameHash()
        {
            var data = Encoding.UTF8.GetBytes("test_data");
            
            var hash1 = IntegrityVerifier.ComputeHashBytes(data);
            var hash2 = IntegrityVerifier.ComputeHashBytes(data);
            
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void ComputeHashBytes_MatchesComputeHash_ForSameData()
        {
            var stringData = "test_data";
            var bytesData = Encoding.UTF8.GetBytes(stringData);
            
            var hashFromString = IntegrityVerifier.ComputeHash(stringData);
            var hashFromBytes = IntegrityVerifier.ComputeHashBytes(bytesData);
            
            Assert.AreEqual(hashFromString, hashFromBytes);
        }

        [TestMethod]
        public void VerifyHash_IsCaseInsensitive()
        {
            var data = "test_data";
            var hash = IntegrityVerifier.ComputeHash(data);
            var lowerHash = hash.ToLowerInvariant();
            var upperHash = hash.ToUpperInvariant();
            
            Assert.IsTrue(IntegrityVerifier.VerifyHash(data, lowerHash));
            Assert.IsTrue(IntegrityVerifier.VerifyHash(data, upperHash));
        }

        private static bool IsValidHexString(string value)
        {
            foreach (var c in value)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
