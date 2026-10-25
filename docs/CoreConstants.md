# CoreConstants

`CoreConstants` covers the constant values defined in `Core/Constants.cs` under the `CaddyVpsToolkit.Core` namespace. The file contains two static classes:

- `AppConstants`: application identity, filesystem paths, default ports and health-check settings, validation limits, and timeouts.
- `ErrorMessages`: standardized error message strings used in exception messages and error responses.

Every member is either a `const` or a `static readonly` field, so no code can reassign the values at runtime. The classes have no constructors and no methods. Callers read the values directly, for example `AppConstants.DefaultCaddyAdminPort`.

Most `AppConstants` values are plain constants. The directory and file path values are `static readonly` because they are computed at startup. `ConfigDirectory`, `LogsDirectory`, `CacheDirectory`, and `DatabasePath` pass their strings through `Environment.ExpandEnvironmentVariables` using `%PROGRAMDATA%`. That variable exists on Windows but not on a typical Linux host. If it is unset, the placeholder is left in the path unchanged (for example `%PROGRAMDATA%/caddy-vps-toolkit/services.db`). See [Notes](#notes).

## AppConstants

### Application identity

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `AppName` | `string` | `"Caddy VPS Toolkit"` | Human-readable name shown in CLI output. |
| `AppVersion` | `string` | `"1.0.0"` | Current version, in semantic versioning format. |
| `ApplicationId` | `string` | `"caddy-vps-toolkit"` | Machine-readable identifier for logs and configuration. |

`AppName` and `AppVersion` are used in the CLI version banner (`CliCommandHandler`), in `ConfigurationService` as the `app:name` and `app:version` cache entries, and as the default `AppVersion` of `BackupManifest`. `ApplicationId` has no references in the current source.

### Directories and file paths

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `ConfigDirectory` | `string` (readonly) | `%PROGRAMDATA%/caddy-vps-toolkit` | Base directory for toolkit configuration and backups. |
| `LogsDirectory` | `string` (readonly) | `%PROGRAMDATA%/caddy-vps-toolkit/logs` | Directory read by `LogAggregationService`. |
| `CacheDirectory` | `string` (readonly) | `%PROGRAMDATA%/caddy-vps-toolkit/cache` | Cache directory. No references in the current source. |
| `DatabasePath` | `string` (readonly) | `%PROGRAMDATA%/caddy-vps-toolkit/services.db` | SQLite database file. Every repository builds its connection string from it. |
| `SystemdUnitsDirectory` | `string` (readonly) | `/etc/systemd/system` | Directory where `SystemdUnitService` writes and reads `.service` units. |
| `CaddyConfigDirectory` | `string` (readonly) | `/etc/caddy` | Directory that holds the Caddy configuration. |
| `CaddyfilePath` | `string` (readonly) | `/etc/caddy/Caddyfile` | Full path to the active Caddyfile. Used by `BackupService` and `UpstreamManagerService`. |

The two Linux paths are the standard locations for a Caddy and systemd install from a package manager. The `%PROGRAMDATA%` paths are covered in [Notes](#notes).

### Ports

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `DefaultCaddyAdminPort` | `int` | `2019` | Default port of the Caddy admin API. Used by `CaddyClient` and as the default value in `ConfigurationService`. |
| `DefaultHttpPort` | `int` | `80` | Standard HTTP port. No references in the current source. |
| `DefaultHttpsPort` | `int` | `443` | Standard HTTPS port. Default `Port` of `CertificateMonitorOptions`, and the port `CertificateMonitor` connects to when it checks a domain. |
| `DefaultServicePort` | `int` | `8080` | Fallback port for a managed service. No references in the current source. |

### Defaults

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `DefaultHealthCheckIntervalSeconds` | `int` | `30` | Time between health checks. Written to the generated Caddyfile as `health_interval` in `ReverseProxyConfig`. |
| `DefaultHealthCheckTimeoutSeconds` | `int` | `5` | Time a health check may take before it counts as failed. No references in the current source. |
| `DefaultHealthCheckUnhealthyThreshold` | `int` | `3` | Number of consecutive failures before a service is marked unhealthy. No references in the current source. |
| `DefaultServiceUser` | `string` | `"root"` | User account that runs a managed service by default. No references in the current source. |

### File names

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `CaddyfileName` | `string` | `"Caddyfile"` | File name of the Caddy configuration. Combined with `CaddyConfigDirectory` by `CaddyConfigurationService`. |
| `AppSettingsFileName` | `string` | `"appsettings.json"` | Configuration file name. Combined with `ConfigDirectory` by `ConfigurationService`. |
| `SystemdUnitFileSuffix` | `string` | `".service"` | Extension for systemd unit files. No references in the current source. |

### CLI

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `DefaultCommandTimeoutSeconds` | `int` | `30` | Default timeout for a CLI command. No references in the current source. |

### Validation limits

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `MinServiceNameLength` | `int` | `3` | Shortest allowed service name. |
| `MaxServiceNameLength` | `int` | `255` | Longest allowed service name. |
| `MinPortNumber` | `int` | `1` | Lowest valid TCP/UDP port. |
| `MaxPortNumber` | `int` | `65535` | Highest valid TCP/UDP port. |

`MinPortNumber` and `MaxPortNumber` are used by `CaddyClient` to check the admin port, and by `ReverseProxyConfig` to validate upstream ports. `MinServiceNameLength` is also declared as a private constant in `Utilities/ValidationHelper.cs` with the same value. `MaxServiceNameLength` has no references in the current source.

### Timeouts

| Member | Type | Value | Meaning |
|--------|------|-------|---------|
| `HealthCheckSocketTimeoutMs` | `int` | `5000` | Timeout for a health check, in milliseconds. `HealthMonitoringService` uses it for the HTTP client timeout (converted to seconds) and for the TCP connection timeout. |
| `SystemdCommandTimeoutSeconds` | `int` | `10` | Time `SystemdUnitService` waits for a `systemctl` process to exit. The code multiplies it by 1000 to get milliseconds. |
| `CaddyAdminApiTimeoutSeconds` | `int` | `5` | Timeout for calls to the Caddy admin API. No references in the current source. |

Note the units. `HealthCheckSocketTimeoutMs` is in milliseconds, but the other two timeouts are in seconds. Check the suffix before using a value.

## ErrorMessages

`ErrorMessages` holds fixed strings for common failures so that exception messages and error responses stay consistent. Each value is a `const string`.

| Member | Value |
|--------|-------|
| `ServiceNotFound` | `"Service not found"` |
| `ServiceAlreadyExists` | `"Service already exists"` |
| `InvalidServiceConfiguration` | `"Invalid service configuration"` |
| `FailedToStartService` | `"Failed to start service"` |
| `FailedToStopService` | `"Failed to stop service"` |
| `FailedToRestartService` | `"Failed to restart service"` |
| `SystemdNotAvailable` | `"Systemd is not available"` |
| `CaddyNotInstalled` | `"Caddy is not installed"` |
| `HealthCheckFailed` | `"Health check failed"` |
| `DatabaseError` | `"Database error occurred"` |

The messages are generic. The exception types in [CoreExceptions](CoreExceptions.md) use them only as a default or a fallback. Pass a more specific message when there is one. Production code does not currently reference `ErrorMessages`. Its only consumer is the test suite (`ValidationHelperTests`).

## Usage

Reference the values through the class name and do not copy them into local literals, so a change in `Core/Constants.cs` applies everywhere:

```csharp
using CaddyVpsToolkit.Core;

var adminUrl = $"http://localhost:{AppConstants.DefaultCaddyAdminPort}";
var unitPath = PathUtilities.SafeCombine(AppConstants.SystemdUnitsDirectory, unitName);

if (port < AppConstants.MinPortNumber || port > AppConstants.MaxPortNumber)
    throw new ValidationException(ErrorMessages.InvalidServiceConfiguration);
```

Paths are joined with `PathUtilities.SafeCombine`, not `Path.Combine`. `Path.Combine` is banned by `BannedSymbols.txt`.

## Notes

- **`%PROGRAMDATA%` paths.** `ConfigDirectory`, `LogsDirectory`, `CacheDirectory`, and `DatabasePath` are built from the Windows `%PROGRAMDATA%` variable. The rest of the toolkit assumes Linux, with `/etc/caddy` and `/etc/systemd/system`. On Linux, these four values are only correct if `PROGRAMDATA` is set in the environment. Otherwise the literal placeholder remains in the path. Changing these values would change runtime behavior, so this document does not propose a fix.
- **Duplicated value.** `ValidationHelper` repeats `MinServiceNameLength` as a private constant. If one value changes, update the other.
- **Unused members.** Several members have no references in the current source, listed above. They are kept as documented defaults and may be used by future code. Do not assume they affect behavior.
- **Static initialization.** The `static readonly` path values are computed when `AppConstants` is first accessed. Changing the environment after that does not change them.
