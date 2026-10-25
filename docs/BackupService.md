# BackupService

The `BackupService` class creates, restores, lists, and verifies JSON backups of managed services, application configuration, and the Caddyfile. It implements the `IBackupService` interface in the `CaddyVpsToolkit.Services` namespace. Each backup is a single self-contained JSON file (a serialized `BackupManifest`) that can be copied to another host and restored from there.

## API

### `public interface IBackupService`
The contract for backup operations. Register it in the DI container and inject it wherever backups are needed. `Program.cs` registers `BackupService` as a singleton for this interface.

### `public sealed class BackupService : IBackupService`
The default implementation. It is constructed from two repositories:

* `IServiceRepository serviceRepository`: reads and writes managed services.
* `IConfigurationRepository configRepository`: reads and writes application configuration.

Passing `null` for either repository throws `ArgumentNullException`.

```csharp
public BackupService(IServiceRepository serviceRepository, IConfigurationRepository configRepository)
```

### `CreateBackupAsync`
```csharp
Task<string> CreateBackupAsync(string? outputPath = null, string description = "")
```
Snapshots all managed services, all configuration key-value pairs, and the Caddyfile (if it exists at `/etc/caddy/Caddyfile`), then writes them to a JSON file.

* **Parameters**:
  * `outputPath`: Full path of the backup file to write. The parent directory is created if it does not exist. When `null`, the file is written to the config directory as `backup-yyyyMMdd_HHmmss.backup.json`, using the manifest's UTC `CreatedAt` time.
  * `description`: Optional note stored in `BackupManifest.Description`.
* **Return Value**: The full path of the written file.
* **Behavior**: The manifest is serialized with indented JSON and a SHA-256 checksum is stored in `Sha256Checksum`. If the Caddyfile is missing, `CaddyfileContent` is `null`.

### `RestoreBackupAsync`
```csharp
Task<BackupManifest> RestoreBackupAsync(string backupFilePath)
```
Applies a backup file to the current system and returns the manifest that was read.

* **Parameters**:
  * `backupFilePath`: Path to the backup file.
* **Return Value**: The deserialized `BackupManifest`.
* **Exceptions**:
  * `ArgumentException`: `backupFilePath` is `null`, empty, or whitespace.
  * `CaddyVpsException` with code `BACKUP_NOT_FOUND`: the file does not exist.
  * `CaddyVpsException` with code `BACKUP_READ_ERROR`: the file cannot be read.
  * `CaddyVpsException` with code `BACKUP_PARSE_ERROR`: the content is empty or is not valid JSON.
* **Behavior**: Each service is added if its `Id` is unknown and updated otherwise. Each configuration entry is written with `SetValueAsync`. If `CaddyfileContent` is not `null`, the Caddyfile is overwritten at `/etc/caddy/Caddyfile`, and its directory is created if needed. The checksum is **not** checked during restore.

### `ListBackupsAsync`
```csharp
Task<IReadOnlyList<string>> ListBackupsAsync(string? backupDirectory = null)
```
Lists backup files in a directory.

* **Parameters**:
  * `backupDirectory`: Directory to search. When `null`, the config directory is used.
* **Return Value**: Full paths of all files matching `*.backup.json`, sorted case-insensitively by ordinal comparison. The list is empty if the directory does not exist. The search is not recursive.

### `VerifyBackupAsync`
```csharp
Task<BackupManifest.BackupIntegrityResult> VerifyBackupAsync(string backupFilePath)
```
Reads a backup file and compares its stored `Sha256Checksum` with a recomputed checksum.

* **Parameters**:
  * `backupFilePath`: Path to the backup file.
* **Return Value**: A `BackupManifest.BackupIntegrityResult` with:
  * `IsSuccess` / `IsIntact`: `true` when the checksums match.
  * `ExpectedChecksum`: the checksum stored in the file.
  * `ComputedChecksum`: the checksum recomputed from the file.
  * `Manifest`: the parsed manifest.
  * `ErrorCode` and `ErrorMessage`: set to `BACKUP_INTEGRITY_CHECK_FAILED` and `Backup integrity check failed: checksum mismatch` on failure.
* **Exceptions**: The same `ArgumentException` and `CaddyVpsException` cases as `RestoreBackupAsync`.

## Usage

### Example 1: Creating a Backup with a Description
This example writes a backup to an explicit path on the local disk and prints the result.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;

public class PreUpgradeBackup
{
    private readonly IBackupService _backupService;

    public PreUpgradeBackup(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task<string> RunAsync()
    {
        string path = "/var/backups/caddy-vps/pre-upgrade.backup.json";

        string written = await _backupService.CreateBackupAsync(path, "Pre-upgrade snapshot");
        Console.WriteLine($"Backup written to {written}");

        return written;
    }
}
```

### Example 2: Listing and Verifying Backups
This example lists the backups in a directory and checks each one's integrity.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;

public class BackupAuditor
{
    private readonly IBackupService _backupService;

    public BackupAuditor(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task AuditAsync(string backupDirectory)
    {
        var backups = await _backupService.ListBackupsAsync(backupDirectory);
        Console.WriteLine($"Found {backups.Count} backup(s).");

        foreach (var file in backups)
        {
            var result = await _backupService.VerifyBackupAsync(file);

            if (result.IsIntact)
            {
                Console.WriteLine($"OK      {file} ({result.Manifest?.ServiceCount} services)");
            }
            else
            {
                Console.Error.WriteLine($"FAILED  {file}: {result.ErrorMessage}");
            }
        }
    }
}
```

### Example 3: Restoring a Backup on Another Host
This example verifies a backup before restoring it. Only restore if the integrity check passes.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Services;

public class BackupRestorer
{
    private readonly IBackupService _backupService;

    public BackupRestorer(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task<bool> RestoreIfIntactAsync(string backupFilePath)
    {
        try
        {
            var integrity = await _backupService.VerifyBackupAsync(backupFilePath);
            if (!integrity.IsIntact)
            {
                Console.Error.WriteLine($"Refusing to restore: {integrity.ErrorMessage}");
                return false;
            }

            var manifest = await _backupService.RestoreBackupAsync(backupFilePath);
            Console.WriteLine($"Restored {manifest.ServiceCount} service(s) from {manifest.CreatedAt:u}.");
            return true;
        }
        catch (CaddyVpsException ex)
        {
            Console.Error.WriteLine($"Restore failed [{ex.ErrorCode}]: {ex.Message}");
            return false;
        }
    }
}
```

## Notes

*   **Checksum verification of newly created backups**: In the current implementation, `CreateBackupAsync` computes the SHA-256 checksum before it sets `Sha256Checksum`. `VerifyBackupAsync` recomputes the checksum with the stored value included. The two values therefore differ, and `VerifyBackupAsync` reports a checksum mismatch for backups created by this service. Until this is fixed, do not rely on `IsIntact` as a safety gate. The existing tests in `tests/caddy-vps-toolkit.Tests/Services/BackupServiceTests.cs` do not cover `VerifyBackupAsync`.
*   **Default directory on Linux**: The default config directory is built from `%PROGRAMDATA%/caddy-vps-toolkit`. On Linux, `Environment.ExpandEnvironmentVariables` does not expand `%PROGRAMDATA%`, so the default is a relative path under the working directory. Pass an explicit `outputPath` (or `backupDirectory`) on Linux hosts.
*   **Bare filenames**: `CreateBackupAsync` uses `Path.GetDirectoryName(outputPath)` to find the target directory. For a bare filename such as `backup.json`, this returns an empty string, which is not replaced by the default directory and makes directory creation fail. Pass a full path.
*   **Restore is not transactional**: Services, configuration, and the Caddyfile are written one after another. If one step fails, earlier steps stay applied. Take a backup of the current state before restoring over a running system.
*   **Restore overwrites**: Existing services with matching `Id` values are replaced. Existing configuration keys are overwritten. Keys and services that are not in the backup are left unchanged. The Caddyfile is replaced only when the backup contains its content.
*   **Concurrency**: Each method performs file I/O and repository calls asynchronously. Avoid running `CreateBackupAsync` and `RestoreBackupAsync` concurrently on the same instance, because they both write to the repositories and may write to the same file path.
*   **Backup file naming**: Files created without `outputPath` use the `.backup.json` suffix. `ListBackupsAsync` matches only that suffix, so backups saved under other names are not listed.
