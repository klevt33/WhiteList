using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace WhiteListService.Tests
{
    [TestClass]
    public class WhitelistStoreTests
    {
        [TestMethod]
        public void Constructor_WithoutParameters_CreatesEmptyStore()
        {
            var store = new WhitelistStore();
            
            Assert.AreEqual(0, store.Count);
            Assert.IsTrue(store.IsEmpty);
            Assert.AreEqual(0, store.Domains.Length);
        }

        [TestMethod]
        public void Constructor_WithDomains_LoadsDomains()
        {
            var domains = new[] { "example.com", "test.org" };
            
            var store = new WhitelistStore(domains);
            
            Assert.AreEqual(2, store.Count);
            Assert.IsFalse(store.IsEmpty);
        }

        [TestMethod]
        public void Constructor_WithNull_CreatesEmptyStore()
        {
            var store = new WhitelistStore(null);
            
            Assert.AreEqual(0, store.Count);
            Assert.IsTrue(store.IsEmpty);
        }

        [TestMethod]
        public void AddDomain_AddsNewDomain()
        {
            var store = new WhitelistStore();
            
            store.AddDomain("example.com");
            
            Assert.AreEqual(1, store.Count);
            Assert.IsTrue(store.ContainsDomain("example.com"));
        }

        [TestMethod]
        public void AddDomain_WithNullDomain_ThrowsArgumentNullException()
        {
            var store = new WhitelistStore();
            
            Assert.ThrowsException<ArgumentNullException>(() => store.AddDomain(null));
        }

        [TestMethod]
        public void AddDomain_WithEmptyDomain_ThrowsArgumentNullException()
        {
            var store = new WhitelistStore();
            
            Assert.ThrowsException<ArgumentNullException>(() => store.AddDomain(string.Empty));
        }

        [TestMethod]
        public void AddDomain_WithWhitespaceDomain_ThrowsArgumentNullException()
        {
            var store = new WhitelistStore();
            
            Assert.ThrowsException<ArgumentNullException>(() => store.AddDomain("   "));
        }

        [TestMethod]
        public void AddDomain_NormalizesDomainToLowercase()
        {
            var store = new WhitelistStore();
            
            store.AddDomain("EXAMPLE.COM");
            
            Assert.IsTrue(store.ContainsDomain("example.com"));
        }

        [TestMethod]
        public void AddDomain_TrimsWhitespace()
        {
            var store = new WhitelistStore();
            
            store.AddDomain("  example.com  ");
            
            Assert.IsTrue(store.ContainsDomain("example.com"));
        }

        [TestMethod]
        public void AddDomain_DuplicateDomain_DoesNotAddDuplicate()
        {
            var store = new WhitelistStore();
            store.AddDomain("example.com");
            
            store.AddDomain("example.com");
            
            Assert.AreEqual(1, store.Count);
        }

        [TestMethod]
        public void AddDomain_CaseInsensitiveDuplicate_DoesNotAddDuplicate()
        {
            var store = new WhitelistStore();
            store.AddDomain("example.com");
            
            store.AddDomain("EXAMPLE.COM");
            
            Assert.AreEqual(1, store.Count);
        }

        [TestMethod]
        public void RemoveDomain_RemovesExistingDomain()
        {
            var store = new WhitelistStore(new[] { "example.com", "test.org" });
            
            store.RemoveDomain("example.com");
            
            Assert.AreEqual(1, store.Count);
            Assert.IsFalse(store.ContainsDomain("example.com"));
            Assert.IsTrue(store.ContainsDomain("test.org"));
        }

        [TestMethod]
        public void RemoveDomain_WithNonExistingDomain_DoesNothing()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            store.RemoveDomain("notfound.com");
            
            Assert.AreEqual(1, store.Count);
        }

        [TestMethod]
        public void RemoveDomain_WithNullDomain_DoesNothing()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            store.RemoveDomain(null);
            
            Assert.AreEqual(1, store.Count);
        }

        [TestMethod]
        public void RemoveDomain_WithEmptyDomain_DoesNothing()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            store.RemoveDomain(string.Empty);
            
            Assert.AreEqual(1, store.Count);
        }

        [TestMethod]
        public void RemoveDomain_CaseInsensitive()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            store.RemoveDomain("EXAMPLE.COM");
            
            Assert.AreEqual(0, store.Count);
        }

        [TestMethod]
        public void ContainsDomain_WithExistingDomain_ReturnsTrue()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            var result = store.ContainsDomain("example.com");
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ContainsDomain_WithNonExistingDomain_ReturnsFalse()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            var result = store.ContainsDomain("notfound.com");
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ContainsDomain_CaseInsensitive()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            Assert.IsTrue(store.ContainsDomain("EXAMPLE.COM"));
            Assert.IsTrue(store.ContainsDomain("Example.Com"));
        }

        [TestMethod]
        public void ContainsDomain_WithNullDomain_ReturnsFalse()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            var result = store.ContainsDomain(null);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ContainsDomain_WithEmptyDomain_ReturnsFalse()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            var result = store.ContainsDomain(string.Empty);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Clear_RemovesAllDomains()
        {
            var store = new WhitelistStore(new[] { "example.com", "test.org" });
            
            store.Clear();
            
            Assert.AreEqual(0, store.Count);
            Assert.IsTrue(store.IsEmpty);
        }

        [TestMethod]
        public void GetDomains_ReturnsSortedDomains()
        {
            var domains = new[] { "zebra.com", "apple.com", "banana.com" };
            var store = new WhitelistStore(domains);
            
            var result = store.GetDomains();
            
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("apple.com", result[0]);
            Assert.AreEqual("banana.com", result[1]);
            Assert.AreEqual("zebra.com", result[2]);
        }

        [TestMethod]
        public void Constructor_NormalizesDomains()
        {
            var domains = new[] { "EXAMPLE.COM", "  test.org  ", "EXAMPLE.COM" };
            
            var store = new WhitelistStore(domains);
            
            Assert.AreEqual(2, store.Count);
            Assert.IsTrue(store.ContainsDomain("example.com"));
            Assert.IsTrue(store.ContainsDomain("test.org"));
        }

        [TestMethod]
        public void Constructor_RemovesDuplicates()
        {
            var domains = new[] { "example.com", "test.org", "example.com", "test.org" };
            
            var store = new WhitelistStore(domains);
            
            Assert.AreEqual(2, store.Count);
        }

        [TestMethod]
        public void IsEmpty_ReturnsTrueForEmptyStore()
        {
            var store = new WhitelistStore();
            
            Assert.IsTrue(store.IsEmpty);
        }

        [TestMethod]
        public void IsEmpty_ReturnsFalseForNonEmptyStore()
        {
            var store = new WhitelistStore(new[] { "example.com" });
            
            Assert.IsFalse(store.IsEmpty);
        }

        [TestMethod]
        public void Count_ReturnsCorrectCount()
        {
            var store = new WhitelistStore(new[] { "example.com", "test.org", "another.net" });
            
            Assert.AreEqual(3, store.Count);
        }

        [TestMethod]
        public void MultipleOperations_MaintainsConsistency()
        {
            var store = new WhitelistStore();
            
            store.AddDomain("example.com");
            store.AddDomain("test.org");
            store.AddDomain("another.net");
            Assert.AreEqual(3, store.Count);
            
            store.RemoveDomain("test.org");
            Assert.AreEqual(2, store.Count);
            
            store.AddDomain("test.org");
            Assert.AreEqual(3, store.Count);
            
            store.Clear();
            Assert.AreEqual(0, store.Count);
            Assert.IsTrue(store.IsEmpty);
        }
    }
}
