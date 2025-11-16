using System;
using System.Security.Cryptography;
using System.Text;

namespace WhiteListService
{
    public static class DpapiEncryptor
    {
        public static string Encrypt(string plainText, bool useUserScope = false)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            var dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
            var scope = useUserScope ? DataProtectionScope.CurrentUser : DataProtectionScope.LocalMachine;

            var encryptedData = ProtectedData.Protect(dataToEncrypt, null, scope);
            return Convert.ToBase64String(encryptedData);
        }

        public static string Decrypt(string encryptedText, bool useUserScope = false)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                throw new ArgumentNullException(nameof(encryptedText));
            }

            try
            {
                var encryptedData = Convert.FromBase64String(encryptedText);
                var scope = useUserScope ? DataProtectionScope.CurrentUser : DataProtectionScope.LocalMachine;

                var decryptedData = ProtectedData.Unprotect(encryptedData, null, scope);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Failed to decrypt data: invalid format.", ex);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to decrypt data: decryption failed.", ex);
            }
        }
    }
}
