# CoreEnums

`CoreEnums` covers the enumerations defined in `Core/Enums.cs` under the `CaddyVpsToolkit.Core` namespace. The file contains three public enums:

- `ServiceStatus`: the runtime state of a managed service.
- `ServiceType`: the kind of workload a managed service runs.
- `HealthCheckType`: the protocol used by a health check.

All enums use the default `int` backing type and start at `0`. The numeric values are part of the public contract and are stored by value, so do not reorder or renumber existing members. New members should be appended with the next free value.

## ServiceStatus

Represents the current state of a managed service. `ManagedService.Status` defaults to `Stopped`.

| Member | Value | Meaning |
|--------|-------|---------|
| `Stopped` | `0` | The service is not running. This is the default for new services. |
| `Running` | `1` | The service is running. |
| `Restarting` | `2` | The service is starting or restarting. |
| `Failed` | `3` | The service stopped because of an error. |
| `Unknown` | `4` | The state cannot be determined. |

`SystemdUnitService` maps systemd `ActiveState` values to this enum: `active` becomes `Running`, `inactive` becomes `Stopped`, `activating` becomes `Restarting`, `failed` becomes `Failed`, and any other value becomes `Unknown`.

`ServiceManagementService` refuses to delete a service whose status is `Running` and counts `Running` services when reporting how many are active.

## ServiceType

Describes the workload category of a managed service. It is stored in `ManagedService.Type` and is included in service events and log messages. The enum does not change how a service is started or monitored.

| Member | Value | Meaning |
|--------|-------|---------|
| `WebApplication` | `0` | A web application that serves HTTP content. |
| `ApiService` | `1` | An HTTP API service. |
| `Worker` | `2` | A background process that does not serve requests. |
| `Database` | `3` | A database server. |
| `Cache` | `4` | A cache server. |
| `Custom` | `5` | A workload that does not fit the other categories. |

`ServiceCreatedEvent` and `ServiceDeletedEvent` carry this value, and the event extensions include it in their descriptions. Every value is valid, so `ServiceCreatedEventValidation` does not check it.

## HealthCheckType

Selects the protocol used by a health check. It is stored in `HealthCheckConfig.Type`.

| Member | Value | Meaning |
|--------|-------|---------|
| `Http` | `0` | Sends an HTTP request to `Endpoint`. `Endpoint` is required. |
| `Tcp` | `1` | Checks that a TCP connection can be opened to the service. |
| `Exec` | `2` | Command-based check. Validation requires `ExpectedResponse`. |
| `Grpc` | `3` | gRPC health check. Defined but not implemented. |

`HealthCheckConfig.Validate()` enforces the required fields for `Http` and `Exec`. For `Http`, `HealthMonitoringService` also treats `ExpectedResponse` as an optional substring that the response body must contain.

`HealthMonitoringService` dispatches only `Http` and `Tcp`. A check with type `Exec` or `Grpc` throws `NotSupportedException` at runtime, even though `Exec` configurations pass validation. No code path performs a `Grpc` check.

`HealthMonitoringService` writes the enum name (for example `"Http"`) into `HealthCheckResult.CheckType`.

## Notes

- Treat the numeric values as stable. Changing or reordering them can break data that was written with the old values.
- `ToString()` returns the member name, which is what the toolkit writes into log messages and `HealthCheckResult.CheckType`.
- `HealthCheckType` has more members than the monitoring service supports. Check the dispatch in `HealthMonitoringService` before you configure a check of type `Exec` or `Grpc`.
