# ServiceCreatedEvent

`ServiceCreatedEvent` is a plain data structure used to convey information about a service lifecycle change within the Caddy VPS Toolkit. It is typically published by domain‑specific handlers when a service is created, updated, or encounters an error, allowing subscribers to react to creation events, configuration modifications, or failure conditions.

## API

| Member | Type | Description |
|--------|------|-------------|
| `ServiceName` | `string` | The logical identifier of the service associated with the event. Never null; empty string indicates an unspecified service. |
| `ServiceType` | `ServiceType` | An enumeration describing the kind of service (e.g., HTTP, TCP, TLS). Provides context for how the service should be managed. |
| `Port` | `int` | The network port on which the service listens. Zero or a negative value indicates that the service does not bind to a specific port (e.g., a pure exec service). |
| `ExecutablePath` | `string` | Absolute or relative path to the executable or script that implements the service. May be null if the service is defined purely by configuration. |
| `OldStatus` | `ServiceStatus` | The status of the service before the change that triggered the event. Relevant for update or transition events. |
| `NewStatus` | `ServiceStatus` | The status of the service after the change. Relevant for update or transition events. |
| `ChangedAt` | `DateTime` | UTC timestamp indicating when the status change occurred. Always set to a non‑`DateTime.MinValue` value. |
| `ConfigurationKey` | `string` | The name of the configuration entry that was modified, when the event represents a configuration change. Empty string if the event is not configuration‑related. |
| `OldValue` | `string` | The previous value of `ConfigurationKey` before the modification. Null if the key did not previously exist. |
| `NewValue` | `string` | The new value of `ConfigurationKey` after the modification. Null if the key was removed. |
| `ErrorMessage` | `string` | A human‑readable description of an error that occurred during service creation or execution. Null or empty when no error is present. |
| `ConsecutiveFailures` | `int` | Count of successive failed attempts to start or health‑check the service. Reset to zero on a successful start. |
| `ResponseTimeMs` | `int` | Measured latency (in milliseconds) of the service’s last successful response. Zero indicates that no response has been recorded yet. |

All members are public fields; they have no parameters, return values, or explicit throw behavior. Invalid states (e.g., negative `Port` when a port is expected) are not enforced by the type itself; consumers should validate fields as needed.

## Usage

### Publishing a service creation event

```csharp
using CaddyVpsToolkit.Events;
using CaddyVpsToolkit.Enums;

var @event = new ServiceCreatedEvent
{
    ServiceName        = "reverse-proxy",
    ServiceType        = ServiceType.Http,
    Port               = 80,
    ExecutablePath     = "/usr/local/bin/caddy",
    OldStatus          = ServiceStatus.Stopped,
    NewStatus          = ServiceStatus.Running,
    ChangedAt          = DateTime.UtcNow,
    ConfigurationKey   = string.Empty,
    OldValue           = null,
    NewValue           = null,
    ErrorMessage       = null,
    ConsecutiveFailures = 0,
    ResponseTimeMs     = 0
};

eventPublisher.Publish(@event);
```

### Handling a configuration‑change event

```csharp
using CaddyVpsToolkit.Events;
using CaddyVpsToolkit.Enums;

void OnServiceCreated(object sender, ServiceCreatedEvent e)
{
    if (!string.IsNullOrEmpty(e.ConfigurationKey))
    {
        Console.WriteLine(
            $"[{e.ServiceName}] Configuration '{e.ConfigurationKey}' changed from " +
            $"'{e.OldValue ?? "(null)"}' to '{e.NewValue ?? "(null)"}' at {e:O}");
    }

    if (!string.IsNullOrEmpty(e.ErrorMessage))
    {
        Console.Error.WriteLine(
            $"[{e.ServiceName}] Error: {e.ErrorMessage} (failures: {e.ConsecutiveFailures})");
    }
}

// Subscribe
eventPublisher.ServiceCreated += OnServiceCreated;
```

## Notes

- The type does not perform any validation; consumers must check that `Port` is within the valid range (1‑65535) when a port is required, and that `ExecutablePath` points to an existing file if the service expects an executable.
- `ChangedAt` is always expressed in UTC; converting to local time should be done explicitly by the caller.
- Because `ServiceCreatedEvent` is a simple mutable class with public fields, it is **not** thread‑safe for concurrent writes. Instances should be fully populated before being published, after which they should be treated as immutable by handlers.
- Duplicate field names in the source listing (e.g., multiple `ServiceName` entries) represent the same logical member; the documentation covers each unique field once.
- No inheritance or interfaces are implied by the listed members; if the actual type derives from a base class or implements an interface, those details are outside the scope of this document.
