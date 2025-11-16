using System;
using System.Security.Cryptography;
using System.Text;

namespace WhiteListService
{
    public static class IntegrityVerifier
    {
        public static string ComputeHash(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (var sha256 = SHA256.Create())
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var hashBytes = sha256.ComputeHash(dataBytes);
                return Convert.ToHexString(hashBytes);
            }
        }

        public static bool VerifyHash(string data, string hash)
        {
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(hash))
            {
                return false;
            }

            try
            {
                var computedHash = ComputeHash(data);
                return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string ComputeHashBytes(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(data);
                return Convert.ToHexString(hashBytes);
            }
        }
    }
}
