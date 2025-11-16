# Configuration & Encryption Module Documentation

## Overview

The Configuration & Encryption Module provides secure storage and management of sensitive data for the WhiteList Service, including:

- **DPAPI Encryption**: Machine-wide or user-specific encryption for sensitive configuration data
- **BCrypt Password Hashing**: Secure password storage with salted hashing for admin credentials
- **SHA-256 Integrity Verification**: Configuration integrity checking to detect tampering or corruption
- **Thread-Safe Access**: Concurrent read and exclusive write access patterns using `ReaderWriterLockSlim`
- **ACL Enforcement**: File and directory access control restrictions
- **Secure Defaults**: Empty whitelist equals all blocked (deny-by-default model)

## Architecture

### Core Components

#### 1. DpapiEncryptor
Provides static methods for encrypting and decrypting sensitive data using DPAPI (Data Protection API).

```csharp
// Encrypt data using machine-wide scope
var encrypted = DpapiEncryptor.Encrypt("sensitive_data");

// Decrypt the data
var plaintext = DpapiEncryptor.Decrypt(encrypted);

// Use user-scope encryption for per-user secrets
var userEncrypted = DpapiEncryptor.Encrypt("user_secret", useUserScope: true);
var userDecrypted = DpapiEncryptor.Decrypt(userEncrypted, useUserScope: true);
```

**Key Features:**
- Base64-encoded output for easy storage and transport
- Automatic exception handling with meaningful error messages
- Support for both machine-wide (LocalMachine) and user-specific (CurrentUser) scopes
- Handles UTF-8 text encoding/decoding transparently

**Security Considerations:**
- Machine-wide scope: Data encrypted with SYSTEM account credentials, accessible only to SYSTEM and administrators
- User-scope: Data encrypted with user's own credentials, more portable across system restarts but only accessible by that user
- All encryption is done using Windows CNG (Cryptography Next Generation) infrastructure
- Keys are managed by the operating system, not stored in plaintext

#### 2. AdminCredentials
Manages admin password storage using bcrypt with automatic salting.

```csharp
// Create new credentials from a password
var credentials = AdminCredentials.CreateFromPassword("MySecurePassword123!");

// Verify a password attempt
if (credentials.VerifyPassword("MySecurePassword123!"))
{
    // Password correct
}

// Check if password is set
if (credentials.IsPasswordSet())
{
    // Password exists
}

// Clear password
credentials.ClearPassword();
```

**Key Features:**
- **Bcrypt Work Factor**: 12 (configurable, provides ~0.3 second hashing time)
- **Automatic Salting**: Each hash includes a unique salt
- **Non-Reversible**: Passwords cannot be recovered from hashes
- **Safe Construction**: Always use `CreateFromPassword()` for new credentials
- **Copy Constructor**: `new AdminCredentials(hash)` for deserialization

**Security Properties:**
- Bcrypt with work factor 12 provides strong protection against brute-force attacks
- Each hash generation produces a unique hash even for the same password
- Timing-safe comparison to prevent timing attacks
- Passwords are never stored in memory after hashing

#### 3. IntegrityVerifier
Implements SHA-256 hashing for configuration integrity checking.

```csharp
// Compute hash of configuration data
var hash = IntegrityVerifier.ComputeHash(configJsonData);

// Verify configuration hasn't been tampered with
if (IntegrityVerifier.VerifyHash(configJsonData, storedHash))
{
    // Configuration is valid and unmodified
}

// Hash raw bytes
var bytesHash = IntegrityVerifier.ComputeHashBytes(dataBytes);
```

**Key Features:**
- **SHA-256**: 256-bit hash (64-character hex string)
- **Case-Insensitive Verification**: Handles both uppercase and lowercase hashes
- **Two Overloads**: String data and raw byte arrays
- **Consistent Output**: Same input always produces same hash

**Integrity Checking Flow:**
1. When saving configuration, hashes are computed over combined whitelist + credentials JSON
2. Hash is stored alongside encrypted data
3. On load, hash is recomputed and compared with stored hash
4. Any mismatch indicates corruption or tampering

#### 4. WhitelistStore
Manages the collection of whitelisted domains.

```csharp
var store = new WhitelistStore();

// Add domains
store.AddDomain("example.com");
store.AddDomain("test.org");

// Check membership
if (store.ContainsDomain("example.com"))
{
    // Domain is whitelisted
}

// Remove domains
store.RemoveDomain("example.com");

// Get all domains (sorted, read-only)
var allDomains = store.GetDomains();

// Check if empty
if (store.IsEmpty)
{
    // All traffic is blocked (secure default)
}

// Clear all domains
store.Clear();
```

**Key Features:**
- **Automatic Normalization**: Domains converted to lowercase, trimmed
- **Duplicate Prevention**: Case-insensitive deduplication
- **Sorted Storage**: Domains stored in alphabetical order
- **Immutable Views**: `GetDomains()` returns read-only collection
- **Serializable**: Fully JSON-serializable using DataContractJsonSerializer
- **Empty-by-Default**: Creates empty whitelist for secure defaults

**Domain Validation:**
- All domains are normalized to lowercase
- Whitespace is automatically trimmed
- Duplicates (case-insensitive) are automatically handled
- Empty/null domains are rejected with ArgumentNullException on add operations

#### 5. ConfigManager
Central orchestrator for all configuration operations with thread-safe access.

```csharp
var manager = new ConfigManager("path/to/config/directory");

// Load configuration from disk
manager.Load();

// Save configuration to disk
if (manager.Save(out var error))
{
    // Saved successfully
}
else
{
    // error contains diagnostic message
}

// Whitelist operations
manager.AddWhitelistDomain("example.com");
manager.RemoveWhitelistDomain("example.com");
var whitelist = manager.GetWhitelist();
manager.UpdateWhitelist(new WhitelistStore(domains));

// Admin password operations
manager.SetAdminPassword("NewPassword123!");
if (manager.VerifyAdminPassword("NewPassword123!"))
{
    // Password is correct
}

bool hasPassword = manager.IsAdminPasswordSet();

// Cleanup
manager.Dispose();
```

**Thread Safety:**
- Uses `ReaderWriterLockSlim` for concurrent access
- Multiple readers can read simultaneously
- Writers get exclusive access
- All public methods are thread-safe

**Configuration File Format:**
```json
{
  "WhitelistStoreEncrypted": "base64_encrypted_whitelist_json",
  "AdminCredentialsEncrypted": "base64_encrypted_credentials_json",
  "ConfigurationHash": "sha256_hash_hex_string",
  "Version": 1
}
```

**Save/Load Process:**
1. **Save**: Serialize → Encrypt → Hash → Write file
   - Whitelist and credentials are independently encrypted
   - Hash is computed over both plaintext JSONs combined
   - ACLs are enforced on the saved file

2. **Load**: Read file → Decrypt → Verify hash → Deserialize
   - Configuration file is parsed
   - Hash is verified to detect corruption
   - Encrypted data is decrypted
   - JSON is deserialized back into objects
   - On hash mismatch: Use safe defaults (empty whitelist, no password)

**Diagnostic Messages:**
- Informative `out string diagnosticMessage` parameter for error reporting
- Distinguishes between missing files, deserialization errors, decryption failures, and corruption
- Service can log these for troubleshooting

#### 6. AclEnforcer
Manages file and directory access control lists for configuration security.

```csharp
// Enforce ACLs on configuration directory
AclEnforcer.EnforceConfigDirectoryACL("path/to/config");

// Enforce ACLs on specific configuration file
AclEnforcer.EnforceConfigFileACL("path/to/config/encrypted.config");

// Verify permissions before operations
bool canRead = AclEnforcer.VerifyFileReadPermission("path/to/file");
bool canWrite = AclEnforcer.VerifyFileWritePermission("path/to/file");
```

**Access Control Policies:**

**File ACL:**
- **SYSTEM**: Full Control (read, write, delete, modify)
- **Administrators**: Read, Write, Delete
- **Everyone Else**: No Access (explicitly denied via SetAccessRuleProtection)

**Directory ACL:**
- Same as file, with inheritance flags for new files:
  - ContainerInherit: Applies to subdirectories
  - ObjectInherit: Applies to files within directory
- All new files inherit parent directory ACLs

**ACL Enforcement:**
- `SetAccessRuleProtection(true, false)`: Disables inheritance, protects from parent changes
- Clears all existing rules before applying new ones
- Unwind of existing rules prevents permission leakage
- Exceptions converted to InvalidOperationException with diagnostic information

## Security Properties

### Encryption Security

**DPAPI (Data Protection API):**
- Leverages Windows cryptographic infrastructure (CNG - Cryptography Next Generation)
- Keys are managed by Windows, never exposed to application
- Machine-wide scope: Uses SYSTEM account credentials
- User scope: Uses current user credentials
- Suitable for protecting data at rest on Windows systems

**Limitations:**
- Only works on Windows (designed for Windows services)
- Machine-wide encrypted data persists across reboots (keys held in registry)
- User-scoped data requires original user identity (may not work after account deletion)

### Password Security

**Bcrypt:**
- Adaptive hash function: Hash computation time increases with work factor
- Work factor 12: ~300ms per hash (deters brute force attacks)
- Unique salt per hash: Prevents rainbow table attacks
- Verification uses timing-safe comparison: Prevents timing attacks

**Never:**
- Store plaintext passwords
- Use simple hashing (MD5, SHA1 without salt)
- Use the same salt for multiple passwords
- Compare passwords in constant-time fashion (bcrypt.NET-Next handles this)

### Integrity Security

**SHA-256:**
- 256-bit cryptographic hash: 2^256 possible outputs
- Collision-resistant for practical purposes
- Any single-bit modification changes hash completely (avalanche effect)
- One-way function: Cannot reverse hash to plaintext

**Detection Capability:**
- Detects accidental file corruption
- Detects unauthorized modification of configuration
- Does NOT provide authentication (attacker can modify both config and hash)
- For authentication, use: signed configurations (digital signatures), file signing with certificates

### Access Control Security

**ACL Enforcement:**
- Restricts configuration file access to SYSTEM and Administrators only
- Inheritance protection: Prevents child objects from inheriting parent permissions
- Rule protection: Disables inheritance and prevents modification by non-admin processes
- Verification methods: Can test read/write permissions before operations

**Limitations:**
- ACL enforcement requires Administrator permissions at setup time
- ACL changes are immediate but not cryptographically signed
- Determined administrator with elevation can change ACLs
- For ultimate security: Use encryption + ACLs together

## Integration with WhiteList Service

### Service Initialization

```csharp
// In WhiteListService.OnStart()
_configManager = new ConfigManager(AppDomain.CurrentDomain.BaseDirectory);
_configManager.Load();

// Enforce ACLs after configuration directory is created
var configPath = _configManager.EncryptedConfigPath;
var directory = Path.GetDirectoryName(configPath);
AclEnforcer.EnforceConfigDirectoryACL(directory);
```

### Configuration Reloading

```csharp
// When configuration needs to be reloaded
private void ReloadConfiguration()
{
    var diagnostics = _configManager.Load(out var message);
    if (diagnostics)
    {
        LogInformation("Configuration reloaded successfully.");
    }
    else
    {
        LogWarning($"Configuration reload issues: {message}");
        // Service enters safe state (empty whitelist, no admin password)
    }
}
```

### Admin Credential Management

```csharp
// Set admin password via IPC or REST API
public void SetAdminPassword(string newPassword)
{
    _configManager.SetAdminPassword(newPassword);
    if (!_configManager.Save(out var error))
    {
        LogError($"Failed to save new admin password: {error}");
        throw new InvalidOperationException("Failed to update admin credentials.");
    }
}

// Verify password during authentication
public bool AuthenticateAdmin(string password)
{
    return _configManager.VerifyAdminPassword(password);
}
```

### Whitelist Management

```csharp
// Add domain to whitelist
public void AllowDomain(string domain)
{
    _configManager.AddWhitelistDomain(domain);
    if (!_configManager.Save(out var error))
    {
        LogError($"Failed to save whitelist: {error}");
        throw new InvalidOperationException("Failed to update whitelist.");
    }
}

// Get current whitelist for access control
public bool IsDomainAllowed(string domain)
{
    var whitelist = _configManager.GetWhitelist();
    return whitelist.ContainsDomain(domain);
}
```

## Unit Tests

Comprehensive test suite covering:

### DpapiEncryptorTests
- ✓ Encryption/decryption roundtrip
- ✓ Null/empty input validation
- ✓ Special and Unicode character preservation
- ✓ Large text handling
- ✓ Machine-wide and user-scope encryption
- ✓ Invalid base64 handling

### AdminCredentialsTests
- ✓ Password hashing and verification
- ✓ Null/empty input validation
- ✓ Password clearing
- ✓ Non-reversible hashing
- ✓ Different hashes for same password (salt randomization)
- ✓ Password hash serialization/deserialization

### IntegrityVerifierTests
- ✓ Hash computation consistency
- ✓ Hash verification with correct/incorrect data
- ✓ SHA-256 output format (64-char hex)
- ✓ Case-insensitive verification
- ✓ Byte array hashing
- ✓ Null/empty input validation

### WhitelistStoreTests
- ✓ Domain addition and removal
- ✓ Domain normalization (lowercase, trim)
- ✓ Duplicate prevention
- ✓ Case-insensitive lookups
- ✓ Sorted output
- ✓ Clear and count operations
- ✓ Null/empty domain handling

### ConfigManagerTests
- ✓ Configuration save/load roundtrip
- ✓ Data preservation across serialization
- ✓ Thread-safe concurrent access
- ✓ Corrupted configuration handling
- ✓ Whitelist operations (add, remove, update)
- ✓ Admin credential operations
- ✓ Permission verification

### AclEnforcerTests
- ✓ File and directory ACL enforcement
- ✓ Read/write permission verification
- ✓ Non-existent path handling
- ✓ Access denied scenarios

## Usage Examples

### Complete Configuration Setup

```csharp
public class SecureWhiteListManager
{
    private readonly ConfigManager _config;

    public SecureWhiteListManager(string configDir)
    {
        _config = new ConfigManager(configDir);
        
        // Load existing configuration
        _config.Load(out var diagnostics);
        if (!string.IsNullOrEmpty(diagnostics))
        {
            Console.WriteLine($"Warning: {diagnostics}");
        }
        
        // Enforce security
        AclEnforcer.EnforceConfigDirectoryACL(configDir);
    }

    public void AddAllowedDomain(string domain)
    {
        _config.AddWhitelistDomain(domain);
        if (!_config.Save(out var error))
        {
            throw new Exception($"Failed to save: {error}");
        }
    }

    public void SetAdminPassword(string password)
    {
        _config.SetAdminPassword(password);
        if (!_config.Save(out var error))
        {
            throw new Exception($"Failed to save: {error}");
        }
    }

    public bool CheckAccess(string domain, string adminPassword)
    {
        // Verify admin
        if (!_config.VerifyAdminPassword(adminPassword))
        {
            return false;
        }

        // Check if domain is whitelisted
        var whitelist = _config.GetWhitelist();
        return whitelist.ContainsDomain(domain);
    }

    public void Cleanup()
    {
        _config?.Dispose();
    }
}
```

## Deployment Considerations

### Prerequisites
- Windows Server 2012 R2 or later (for DPAPI support)
- .NET Framework 4.8
- BCrypt.Net-Next NuGet package

### Configuration Directory Setup
1. Create configuration directory (e.g., `C:\ProgramData\WhiteListService`)
2. Run service as SYSTEM account
3. ACLs will be automatically enforced during initialization

### Initial Password Setup
1. Set admin password via admin tool after service starts
2. Password hash is encrypted and stored in configuration
3. No plaintext password ever written to disk

### Recovery Procedures

**Corrupted Configuration:**
- Service detects hash mismatch on load
- Automatically switches to safe defaults (empty whitelist)
- Administrator must restore from backup or reconfigure

**Lost Admin Password:**
- No recovery possible (bcrypt is one-way)
- Must manually delete configuration file
- Service will reinitialize with empty configuration
- Administrator can set new password

**DPAPI Key Issues:**
- On machine key corruption: Restore from system backup
- On user key issues: Recreate user profile or use machine-wide scope
- For recovery: Keep encrypted.config backups and document key protection scope

## Performance Characteristics

- **Encryption/Decryption**: ~1-2ms per operation (DPAPI overhead)
- **Bcrypt Verification**: ~300ms per attempt (intentional slowness for security)
- **SHA-256 Hash**: <1ms for typical config sizes
- **Thread Lock Contention**: Minimal under normal load (readers > writers)
- **File I/O**: Standard Windows disk I/O performance

## Troubleshooting

### Issue: "Failed to decrypt data: decryption failed"
- **Cause**: DPAPI key not available (wrong scope, account change)
- **Solution**: Verify service account, check Windows Event Log for details

### Issue: "Configuration hash mismatch"
- **Cause**: Configuration file corruption or unauthorized modification
- **Solution**: Service enters safe state (all blocked), restore from backup

### Issue: "Access denied" when reading configuration
- **Cause**: ACL permissions too restrictive or service account not in allowed groups
- **Solution**: Check ACL with `icacls` command, re-run ACL enforcement

### Issue: "Admin password always fails verification"
- **Cause**: Bcrypt verification failed (corrupted hash)
- **Solution**: Delete configuration and set new password

## Future Enhancements

- [ ] Registry-based configuration storage option
- [ ] Configuration encryption key rotation
- [ ] Digital signature support for configuration authentication
- [ ] Multi-factor authentication for admin access
- [ ] Configuration version management
- [ ] Distributed configuration sync
- [ ] Hardware security module (HSM) support
