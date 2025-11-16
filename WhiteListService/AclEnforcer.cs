using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace WhiteListService
{
    public static class AclEnforcer
    {
        public static void EnforceConfigFileACL(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Configuration file not found.", filePath);
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSecurity = fileInfo.GetAccessControl();

                fileSecurity.SetAccessRuleProtection(true, false);

                var rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                foreach (FileSystemAccessRule rule in rules)
                {
                    fileSecurity.RemoveAccessRule(rule);
                }

                var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    systemSid,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));

                var administratorsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    administratorsSid,
                    FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete,
                    AccessControlType.Allow));

                fileInfo.SetAccessControl(fileSecurity);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enforce ACL on configuration file: {ex.Message}", ex);
            }
        }

        public static void EnforceConfigDirectoryACL(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException("Configuration directory not found.");
            }

            try
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                var dirSecurity = dirInfo.GetAccessControl();

                dirSecurity.SetAccessRuleProtection(true, false);

                var rules = dirSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                foreach (FileSystemAccessRule rule in rules)
                {
                    dirSecurity.RemoveAccessRule(rule);
                }

                var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                dirSecurity.AddAccessRule(new FileSystemAccessRule(
                    systemSid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                var administratorsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                dirSecurity.AddAccessRule(new FileSystemAccessRule(
                    administratorsSid,
                    FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                dirInfo.SetAccessControl(dirSecurity);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enforce ACL on configuration directory: {ex.Message}", ex);
            }
        }

        public static bool VerifyFileReadPermission(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (File.OpenRead(filePath))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool VerifyFileWritePermission(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    var path = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(path))
                    {
                        return false;
                    }

                    using (var file = File.Create(filePath))
                    {
                        file.WriteByte(0);
                    }

                    File.Delete(filePath);
                }
                else
                {
                    using (var file = File.Open(filePath, FileMode.Append, FileAccess.Write))
                    {
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
