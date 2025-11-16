using System;
using BCrypt.Net;

namespace WhiteListService
{
    public class AdminCredentials
    {
        private const int BcryptWorkFactor = 12;

        public string PasswordHash { get; private set; }

        public AdminCredentials()
        {
            PasswordHash = string.Empty;
        }

        public AdminCredentials(string passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash))
            {
                throw new ArgumentNullException(nameof(passwordHash));
            }

            PasswordHash = passwordHash;
        }

        public static AdminCredentials CreateFromPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor);
            return new AdminCredentials(hash);
        }

        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (string.IsNullOrEmpty(PasswordHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
            }
            catch
            {
                return false;
            }
        }

        public bool IsPasswordSet()
        {
            return !string.IsNullOrEmpty(PasswordHash);
        }

        public void ClearPassword()
        {
            PasswordHash = string.Empty;
        }
    }
}
