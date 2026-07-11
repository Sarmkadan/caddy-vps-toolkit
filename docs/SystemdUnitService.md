# SystemdUnitService

Provides a high‑level wrapper for managing systemd units (services, sockets, timers, etc.) from .NET code. The class encapsulates common operations such as creating or deleting unit files, starting/stopping/enabling/disabling services, and querying unit status.

## API

### SystemdUnitService()
Initializes a new instance of the `SystemdUnitService` class. The instance is not bound to any specific unit until the `UnitName` property is set or a method that requires a unit name is invoked with an explicit name parameter.

### CreateUnitFileAsync()
**Purpose:** Writes a unit file to the systemd configuration directory (`/etc/systemd/system/` or `/usr/lib/systemd/system/` depending on privilege level).  
**Parameters:** None (the unit file content is assumed to be supplied via a prior call to `SetUnitFileContent` or similar internal mechanism; if the class requires content, it must be provided before calling this method).  
**Return Value:** `Task<bool>` – `true` if the unit file was created successfully; `false` if the file already exists and was not overwritten.  
**Exceptions:**  
- `UnauthorizedAccessException` – the process lacks permission to write to the target directory.  
- `IOException` – an I/O error occurred while writing the file.  
- `InvalidOperationException` – the unit name has not been specified.

### DeleteUnitFileAsync()
**Purpose:** Removes the unit file associated with the unit from the systemd configuration directory.  
**Parameters:** None.  
**Return Value:** `Task<bool>` – `true` if the file was deleted; `false` if the file did not exist.  
**Exceptions:**  
- `UnauthorizedAccessException` – insufficient rights to delete the file.  
- `IOException` – an I/O error occurred during deletion.

### StartServiceAsync()
**Purpose:** Instructs systemd to start the unit.  
**Parameters:** None.  
**Return Value:** `Task<bool>` – `true` if the start command was accepted and the unit entered an active state; `false` if systemd reported a failure.  
**Exceptions:**  
- `InvalidOperationException` – the unit name is not set.  
- `System.ComponentModel.Win32Exception` – the underlying `systemctl` call returned a non‑zero exit code indicating an error (e.g., unit not found).

### StopServiceAsync()
**Purpose:** Instructs systemd to stop the unit.  
**Parameters:** None.  
**Return Value:** `Task<bool>` – `true` if the stop command succeeded; `false` otherwise.  
**Exceptions:** Same as `StartServiceAsync`.

### RestartServiceAsync()
**Purpose:** Stops and then starts the unit in a single operation.  
**Parameters:** None.  
**Return Value:** `Task<bool>` – `true` if both stop and start succeeded; `false` if either step failed.  
**Exceptions:** Same as `StartServiceAsync`.

### ReloadServiceAsync()
**Purpose:** Sends a reload signal (SIGHUP) to the unit, prompting it to reload its configuration without a full restart.  
**Parameters:** None.  
**Return Value:** `Task<bool>` – `true` if the reload command was accepted; `false` if the unit does not support reload or the command failed.  
**Exceptions:** Same as `StartServiceAsync`.

### EnableServiceAsync()
**Purpose:** Enables the unit to be started automatically at boot (creates the appropriate symlinks).  
**Parameters:** None.  
**Return Value:** `Task<bool>` – `true` if the unit was enabled; `false` if it was already enabled.  
**Exceptions:** Same as `StartServiceAsync`.

### DisableServiceAsync()
**Purpose:** Disables the unit, removing any boot‑time symlinks.  
**Parameters:** None.  
**Return Value:** `Task<bool>` – `true` if the unit was disabled; `false` if it was already disabled.  
**Exceptions:** Same as `StartServiceAsync`.

### GetServiceStatusAsync()
**Purpose:** Queries systemd for the high‑level active state of the unit (e.g., active, inactive, failed).  
**Parameters:** None.  
**Return Value:** `Task<ServiceStatus>` – an enumeration indicating the current service status.  
**Exceptions:**  
- `InvalidOperationException` – unit name not set.  
- `System.ComponentModel.Win32Exception` – `systemctl is-active` returned an error.

### UnitFileExists
**Purpose:** Gets a value indicating whether a unit file with the current `UnitName` exists on the system.  
**Return Value:** `bool` – `true` if the file exists; otherwise `false`.  
**Exceptions:** None (property is synchronous and performs a file system check; may throw `UnauthorizedAccessException` if the directory cannot be read).

### GetUnitFileContentAsync()
**Purpose:** Reads the contents of the unit file associated with the unit.  
**Parameters:** None.  
**Return Value:** `Task<string>` – the full text of the unit file.  
**Exceptions:**  
- `FileNotFoundException` – the unit file does not exist.  
- `UnauthorizedAccessException` – insufficient rights to read the file.  
- `IOException` – an I/O error occurred.

### ListUnitsAsync()
**Purpose:** Enumerates all unit names known to systemd (equivalent to `systemctl list-units`).  
**Parameters:** None.  
**Return Value:** `Task<List<string>>` – a list of unit names.  
**Exceptions:**  
- `System.ComponentModel.Win32Exception` – the `systemctl` command failed.  
- `UnauthorizedAccessException` – the process lacks permission to query systemd.

### GetDetailedStatusAsync()
**Purpose:** Retrieves detailed runtime information about the unit (main PID, memory usage, etc.).  
**Parameters:** None.  
**Return Value:** `Task<SystemdUnitStatus>` – a structure containing detailed status fields.  
**Exceptions:** Same as `GetServiceStatusAsync`.

### UnitName
**Purpose:** Gets or sets the name of the systemd unit (e.g., `myservice.service`).  
**Return Value:** `string` – the unit name.  
**Exceptions:** None; setting to `null` or whitespace will cause subsequent operations to throw `InvalidOperationException`.

### ActiveState
**Purpose:** Gets the last known active state of the unit (e.g., `active`, `inactive`, `failed`). This property is updated only after a successful call to `GetServiceStatusAsync` or `GetDetailedStatusAsync`.  
**Return Value:** `string`.  
**Exceptions:** None.

### Type
**Purpose:** Gets the unit type as defined in the unit file (`simple`, `forking`, `oneshot`, `service`, `socket`, etc.).  
**Return Value:** `string`.  
**Exceptions:** None.

### MainPid
**Purpose:** Gets the main process ID of the unit, if it is currently running.  
**Return Value:** `int` – the PID, or `0` if the unit is not active.  
**Exceptions:** None.

### TimeoutStartUsec
**Purpose:** Gets the start timeout value configured for the unit, in microseconds.  
**Return Value:** `ulong`.  
**Exceptions:** None.

## Usage

### Example 1: Creating and enabling a service unit
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Systemd; // assumed namespace

class Program
{
    static async Task Main()
    {
        var service = new SystemdUnitService { UnitName = "example.service" };

        // Ensure the unit file does not already exist
        if (!await service.UnitFileExists)
        {
            // Assume unit file content has been set elsewhere or via a helper method
            bool created = await service.CreateUnitFileAsync();
            Console.WriteLine($"Unit file created: {created}");
        }

        // Enable and start the service
        bool enabled = await service.EnableServiceAsync();
        Console.WriteLine($"Service enabled: {enabled}");

        bool started = await service.StartServiceAsync();
        Console.WriteLine($"Service started: {started}");
    }
}
```

### Example 2: Querying detailed status and stopping a service
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Systemd;

class Program
{
    static async Task Main()
    {
        var svc = new SystemdUnitService { UnitName = "nginx.service" };

        // Check if the unit file exists before attempting any operation
        if (svc.UnitFileExists)
        {
            var status = await svc.GetDetailedStatusAsync();
            Console.WriteLine($"Main PID: {status.MainPid}");
            Console.WriteLine($"Active state: {status.ActiveState}");
            Console.WriteLine($"Memory usage: {status.MemoryCurrent} bytes");
        }

        // Stop the service if it is running
        var active = await svc.GetServiceStatusAsync();
        if (active == ServiceStatus.Active)
        {
            bool stopped = await svc.StopServiceAsync();
            Console.WriteLine($"Service stopped: {stopped}");
        }
    }
}
```

## Notes
- The class does **not** synchronize access to its members. Concurrent calls from multiple threads may lead to race conditions (e.g., two threads trying to create the same unit file simultaneously). External synchronization is required if the instance is shared.
- Property values such as `ActiveState`, `MainPid`, and `Type` reflect the state **at the time of the last successful asynchronous query**. They are not automatically updated; call the appropriate `Get*StatusAsync` method to refresh them.
- Most methods rely on invoking `systemctl` or accessing files under `/etc/systemd/system/`. They will throw `UnauthorizedAccessException` if the process lacks sufficient privileges (typically root or membership in the `systemd-journal` group). Running the application with elevated privileges is recommended for modification operations.
- If `UnitName` is not set before calling any operation that requires it, an `InvalidOperationException` will be thrown.
- The `CreateUnitFileAsync` and `DeleteUnitFileAsync` methods do **not** automatically reload the systemd daemon. After creating or deleting a unit file, call `systemctl daemon-reload` via an external process or a future API addition if daemon reload is required for the changes to take effect.
- The `ListUnitsAsync` method returns all units known to systemd, including those that are masked or inactive; filtering by type or state must be performed by the caller.
