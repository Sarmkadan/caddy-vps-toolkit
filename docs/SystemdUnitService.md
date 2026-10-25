# SystemdUnitService

SystemdUnitService wraps `systemctl` and the unit directory (`/etc/systemd/system`) for the Caddy VPS Toolkit. It writes and deletes unit files, runs service lifecycle commands (start, stop, restart, reload, enable, disable), and reads unit state. It has no state of its own, so one instance can be reused.

The source lives in `Services/SystemdUnitService.cs` and contains two public types:

- `SystemdUnitService` is a `sealed` class. It manages unit files and runs `systemctl` commands.
- `SystemdUnitStatus` is a `sealed` class. It holds the fields parsed from `systemctl show`.

Unit configuration is described by [SystemdUnitConfig](SystemdUnitConfig.md). The service-level model is `ManagedService`.

## Prerequisites

- **systemd must be present.** The constructor runs `systemctl --version` and throws if it is missing or fails.
- **Root privileges** are needed to write to `/etc/systemd/system` and to run lifecycle commands against system units.
- **Unit names** are passed as-is to `systemctl`, for example `orders-api.service`.

## API

### `SystemdUnitService`

#### `SystemdUnitService()`

Creates a service instance and checks that systemd is available.

- **Parameters**: None.
- **Exceptions**: Throws `SystemdOperationException` with the message `Failed to verify systemd availability` if `systemctl --version` cannot be started, does not exit within five seconds, or exits with a non-zero code.

#### `Task<bool> CreateUnitFileAsync(SystemdUnitConfig config, ManagedService service, bool dryRun = false)`

Validates `config`, renders the unit content, writes it to `/etc/systemd/system/<UnitName>`, and runs `systemctl daemon-reload`. With `dryRun: true`, it prints the content to the console and changes nothing.

- **Parameters**
  - `config`: Unit definition. `Validate()` runs first, and the content comes from `GenerateSystemdContent()`.
  - `service`: The managed service the unit belongs to. It is only checked for `null`; its fields are not used.
  - `dryRun`: When `true`, prints the unit content and the `daemon-reload` step it would run, then returns without writing anything. Defaults to `false`.
- **Return value**: `true` after the file is written and the daemon is reloaded, or after a dry run.
- **Exceptions**
  - `ArgumentNullException` if `config` or `service` is `null`.
  - `ValidationException` if `config` fails validation (for example, empty `UnitName` or `ExecStart`, or a negative `RestartDelaySeconds`). This exception is not wrapped.
  - `SystemdOperationException` with the message `Failed to create unit file: ...` if the directory is missing, the file cannot be written, or the daemon reload fails.

The method does not enable or start the unit. Call `EnableServiceAsync` and `StartServiceAsync` afterwards.

#### `Task<bool> DeleteUnitFileAsync(string unitName)`

Deletes the unit file from `/etc/systemd/system` and runs `systemctl daemon-reload`. It does not stop or disable the unit.

- **Parameters**
  - `unitName`: File name of the unit, for example `orders-api.service`.
- **Return value**: `true` if the file was deleted. `false` if no file with that name exists.
- **Exceptions**
  - `ArgumentException` if `unitName` is null, empty, or whitespace.
  - `SystemdOperationException` with the message `Failed to delete unit file: ...` if the delete or the daemon reload fails.

#### `Task<bool> StartServiceAsync(string unitName)`
#### `Task<bool> StopServiceAsync(string unitName)`
#### `Task<bool> RestartServiceAsync(string unitName)`
#### `Task<bool> ReloadServiceAsync(string unitName)`
#### `Task<bool> EnableServiceAsync(string unitName)`
#### `Task<bool> DisableServiceAsync(string unitName)`

Each method runs `systemctl <verb> <unitName>`. They differ only in the verb: `start`, `stop`, `restart`, `reload`, `enable`, and `disable`.

- **Parameters**
  - `unitName`: Name of the unit, for example `orders-api.service`.
- **Return value**: `true` when `systemctl` exits with code `0`. Failure is reported by exception, never by `false`.
- **Exceptions**: `SystemdOperationException` with a message such as `Failed to start service: ...` if the command exits non-zero, times out, or cannot be started. `ReloadServiceAsync` fails for units that do not define `ExecReload`, because `systemctl reload` then exits non-zero.

#### `Task<ServiceStatus> GetServiceStatusAsync(string unitName)`

Runs `systemctl is-active <unitName>` and maps the output to a `ServiceStatus` value from `CaddyVpsToolkit.Core`.

- **Parameters**
  - `unitName`: Name of the unit.
- **Return value**: `Running` for `active`, `Stopped` for `inactive`, `Restarting` for `activating`, `Failed` for `failed`, and `Unknown` for anything else. Any exception also gives `Unknown`.
- **Exceptions**: None. Every error is caught and returned as `ServiceStatus.Unknown`.

Read the behaviour note on this method before relying on the `Stopped` or `Failed` values.

#### `bool UnitFileExists(string unitName)`

Checks whether a unit file exists in `/etc/systemd/system`. It does not ask systemd whether the unit is loaded.

- **Parameters**
  - `unitName`: File name of the unit.
- **Return value**: `true` if the file exists.
- **Exceptions**: Throws whatever `PathUtilities.SafeCombine` throws for an invalid file name.

#### `string GenerateUnitPreview(SystemdUnitConfig config)`

Validates `config` and returns the unit file content it would write. It touches no files and runs no commands.

- **Parameters**
  - `config`: Unit definition.
- **Return value**: The unit file content as a string.
- **Exceptions**
  - `ArgumentNullException` if `config` is `null`.
  - `ValidationException` if `config` fails validation.

#### `Task<string> GetUnitFileContentAsync(string unitName)`

Reads the content of a unit file from `/etc/systemd/system`.

- **Parameters**
  - `unitName`: File name of the unit.
- **Return value**: The full file content.
- **Exceptions**: `SystemdOperationException` with the message `Unit file not found: <unitName>` if the file does not exist. Read errors are not wrapped.

#### `Task<List<string>> ListUnitsAsync()`

Lists the `*.service` files in `/etc/systemd/system`. It returns file names only, with no state.

- **Parameters**: None.
- **Return value**: The file names, for example `orders-api.service`. The list is empty if the directory does not exist.
- **Exceptions**: `SystemdOperationException` with the message `Failed to list units: ...` if the directory cannot be read.

#### `Task<SystemdUnitStatus> GetDetailedStatusAsync(string unitName)`

Runs `systemctl show <unitName>` and parses four properties: `ActiveState`, `Type`, `ExecMainPID`, and `TimeoutStartUSec`.

- **Parameters**
  - `unitName`: Name of the unit.
- **Return value**: A `SystemdUnitStatus` whose `UnitName` is set to `unitName`. Properties that are missing from the output keep their defaults. For an unknown unit, systemd reports `ActiveState=inactive` and an empty `Type`, so no exception is thrown.
- **Exceptions**: `SystemdOperationException` with the message `Failed to get status: ...` if `systemctl` fails to run.

### `SystemdUnitStatus`

A plain model with public settable properties. It has no constructor parameters.

| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `UnitName` | `string` | Set by `GetDetailedStatusAsync` | Unit name that was queried. |
| `ActiveState` | `string` | `ActiveState=` | For example `active`, `inactive`, `failed`. `null` until set. |
| `Type` | `string` | `Type=` | For example `simple` or `notify`. `null` until set. |
| `MainPid` | `int` | `ExecMainPID=` | `0` when there is no main process. |
| `TimeoutStartUsec` | `ulong` | `TimeoutStartUSec=` | See the behaviour note below. Stays `0` in practice. |

#### `string ToString()`

Returns a single-line summary of the four values, for example `SystemdUnitStatus { UnitName = orders-api.service, ActiveState = active, Type = simple, MainPid = 4242, TimeoutStartUsec = 0 }`.

## Usage

### Creating, enabling, and starting a unit

Preview the unit first with a dry run, then write it and start the service.

```csharp
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;

var systemd = new SystemdUnitService();

var service = new ManagedService
{
    Name = "orders-api",
    ExecutablePath = "/opt/orders/bin/orders"
};

var config = new SystemdUnitConfig
{
    ServiceId = service.Id,
    UnitName = "orders-api.service",
    ExecStart = "/opt/orders/bin/orders --port 5000",
    WorkingDirectory = "/opt/orders",
    User = "orders",
    Group = "orders",
    RestartPolicy = RestartPolicy.OnFailure
};

// Print the unit content without writing anything
await systemd.CreateUnitFileAsync(config, service, dryRun: true);

// Write the unit file and run daemon-reload
await systemd.CreateUnitFileAsync(config, service);

await systemd.EnableServiceAsync("orders-api.service");
await systemd.StartServiceAsync("orders-api.service");
```

### Checking detailed state

`GetDetailedStatusAsync` returns the raw `ActiveState` string, which is useful when the simple `ServiceStatus` mapping is not enough.

```csharp
var status = await systemd.GetDetailedStatusAsync("orders-api.service");

Console.WriteLine(status);

if (status.ActiveState == "failed")
{
    Console.WriteLine($"Main PID was {status.MainPid}; check: journalctl -u {status.UnitName}");
}
```

### Handling failures

Lifecycle commands report failure by throwing, so wrap calls that can fail.

```csharp
using CaddyVpsToolkit.Core;

try
{
    await systemd.RestartServiceAsync("orders-api.service");
}
catch (SystemdOperationException ex)
{
    Console.Error.WriteLine($"Restart failed: {ex.Message}");
}
```

### Previewing and reading unit content

```csharp
// Build the content without touching disk
string preview = systemd.GenerateUnitPreview(config);

// Read a unit that is already installed
if (systemd.UnitFileExists("orders-api.service"))
{
    string installed = await systemd.GetUnitFileContentAsync("orders-api.service");
}
```

### Removing a unit

`DeleteUnitFileAsync` removes only the file. Stop and disable the unit first.

```csharp
await systemd.StopServiceAsync("orders-api.service");
await systemd.DisableServiceAsync("orders-api.service");

bool deleted = await systemd.DeleteUnitFileAsync("orders-api.service");
// deleted is false if the file was already gone
```

## Behaviour Notes

- **Only `systemctl` and the unit directory are used.** Commands run as child processes with `UseShellExecute = false`, so no shell is involved. Unit names are placed directly into the argument string, though, so a name that contains spaces is split into several arguments. Pass plain unit names.
- **Timeouts.** Each command has a limit of `AppConstants.SystemdCommandTimeoutSeconds` (10 seconds). The limit is applied after stdout and stderr have been read to the end. A command that keeps its pipes open can therefore block longer than the limit.
- **`GetServiceStatusAsync` rarely returns `Stopped` or `Failed`.** `systemctl is-active` exits with a non-zero code for an inactive unit (`inactive` exits with `4` on the systemd 259 host this was checked on). The non-zero exit becomes an exception inside the method, and the catch returns `Unknown`. Only `active` (exit code `0`) reaches the status mapping. For a stopped or failed state, use `GetDetailedStatusAsync` and read `ActiveState`.
- **`TimeoutStartUsec` is not parsed.** `systemctl show` prints `TimeoutStartUSec` in human-readable form, for example `1min 30s`, not as a number of microseconds. The parse into `ulong` fails silently, so the field stays `0`. Use the raw string from `systemctl show` if you need the value.
- **`CreateUnitFileAsync` does not enable or start.** Its XML doc summary says "create and enable", but the code only writes the file and runs `daemon-reload`. The `service` argument is only null-checked.
- **Errors during the file write are re-wrapped.** `CreateUnitFileAsync` wraps every failure in the write path with the prefix `Failed to create unit file:`. Validation errors from `SystemdUnitConfig.Validate()` are thrown as `ValidationException` before that path starts.
- **Status values use `systemctl show`, which does not fail for unknown units.** `GetDetailedStatusAsync` returns `ActiveState=inactive` for a unit that does not exist. Check `ActiveState` or use `UnitFileExists` if you need to tell a missing unit from a stopped one.
- **No mutable state.** The class has no fields after construction, so one instance can be shared, and concurrent calls do not interfere with each other at the service level. Each call starts its own `systemctl` process.
- **Validation helpers.** `SystemdUnitServiceValidation.Validate(this SystemdUnitStatus)` checks a `SystemdUnitStatus` and returns a list of error messages. It lives in `Services/SystemdUnitServiceValidation.cs`.
