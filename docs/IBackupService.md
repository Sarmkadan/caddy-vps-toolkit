# IBackupService

The `IBackupService` interface defines the contract for managing backup operations within the `caddy-vps-toolkit` ecosystem. It provides asynchronous methods to create new backups, restore existing ones based on a manifest, and enumerate available backup identifiers, ensuring non-blocking I/O operations suitable for server-side execution.

## API

### `BackupService`
Represents the concrete implementation type associated with this interface. While the interface defines the contract, `BackupService` is the primary class instantiated to perform the actual backup logic, typically configured via dependency injection or direct instantiation with required storage providers.

### `CreateBackupAsync`
```csharp
public async Task<string> CreateBackupAsync
```
Initiates the creation of a new backup snapshot.
*   **Purpose**: Captures the current state of the configured data sources and persists them to the storage backend.
*   **Parameters**: None.
*   **Return Value**: Returns a `string` representing the unique identifier (ID) or name of the newly created backup. This ID is required for subsequent restore operations.
*   **Exceptions**: Throws an exception if the storage backend is unavailable, disk space is insufficient, or if a concurrent backup operation prevents initiation.

### `RestoreBackupAsync`
```csharp
public async Task<BackupManifest> RestoreBackupAsync
```
Restores data from a specific backup.
*   **Purpose**: Retrieves a previously created backup and applies its contents to the target system, effectively reverting the state to the point of the backup.
*   **Parameters**: Implicitly requires a target identifier or context (often passed via constructor or internal state) to locate the specific backup to restore.
*   **Return Value**: Returns a `BackupManifest` object containing metadata about the restored backup, including timestamps, included files, and integrity checksums.
*   **Exceptions**: Throws an exception if the specified backup does not exist, the manifest is corrupted, or the restore process fails due to permission errors or data conflicts.

### `ListBackupsAsync`
```csharp
public Task<IReadOnlyList<string>> ListBackupsAsync
```
Retrieves a list of all available backups.
*   **Purpose**: Enumerates the identifiers of all backups currently stored in the configured backend.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IReadOnlyList<string>>` containing a read-only list of backup IDs.
*   **Exceptions**: Throws an exception if the storage backend cannot be accessed or if the directory listing operation fails.

## Usage

### Creating and Listing Backups
The following example demonstrates how to instantiate the service, create a new backup, and verify its presence in the list.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;

public class BackupOrchestrator
{
    private readonly IBackupService _backupService;

    public BackupOrchestrator(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task RunDailyBackupAsync()
    {
        Console.WriteLine("Starting backup process...");
        
        // Create a new backup and capture its ID
        string backupId = await _backupService.CreateBackupAsync();
        Console.WriteLine($"Backup created successfully with ID: {backupId}");

        // Verify the backup appears in the list
        var backups = await _backupService.ListBackupsAsync();
        Console.WriteLine($"Total backups available: {backups.Count}");
        
        if (!backups.Contains(backupId))
        {
            throw new InvalidOperationException("Backup was created but not listed.");
        }
    }
}
```

### Restoring from a Specific Backup
This example illustrates the logic for restoring a system to a previous state using a known backup identifier.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Models;
using CaddyVpsToolkit.Services;

public class RecoveryManager
{
    private readonly IBackupService _backupService;

    public RecoveryManager(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task<bool> AttemptRestoreAsync(string targetBackupId)
    {
        try
        {
            Console.WriteLine($"Initiating restore for backup: {targetBackupId}");
            
            // Execute the restore operation
            BackupManifest manifest = await _backupService.RestoreBackupAsync();

            // Validate the manifest matches the expected restoration context
            if (manifest == null)
            {
                Console.Error.WriteLine("Restore completed but returned no manifest.");
                return false;
            }

            Console.WriteLine($"Restore successful. Timestamp: {manifest.CreatedAt}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Restore failed: {ex.Message}");
            return false;
        }
    }
}
```

## Notes

*   **Thread Safety**: As the methods return `Task` objects and involve I/O, the implementation is designed to be asynchronous. However, concurrent calls to `CreateBackupAsync` and `RestoreBackupAsync` may result in resource contention or data corruption depending on the underlying storage provider. It is recommended to serialize these write-intensive operations using external locking mechanisms if multiple threads access the same service instance.
*   **Edge Cases**: 
    *   Calling `RestoreBackupAsync` when no backups exist will likely result in an exception; callers should verify existence via `ListBackupsAsync` first if the target ID is dynamic.
    *   `ListBackupsAsync` returns an `IReadOnlyList`, ensuring the caller cannot modify the underlying collection, but the list represents a snapshot in time and may become stale immediately after retrieval if concurrent creations or deletions occur.
    *   The `CreateBackupAsync` method does not accept parameters for naming; the system generates identifiers automatically. Relying on the returned string is the only supported way to track new backups.
