# ServiceEventsExtensions

`ServiceEventExtensions` is a static extension class in `Events/ServiceEventsExtensions.cs` that adds three helpers to `DomainEvent`: a human-readable `Describe()` for the service event hierarchy, an `IsCritical()` check for events that need operator attention, and a log-friendly `ToLogString()`. It contains no state and does not change the events it extends.

The file name is `ServiceEventsExtensions.cs`, but the class is named `ServiceEventExtensions` (singular). Use the class name in code.

## API

### `Describe(this DomainEvent @event)`

Returns a concise, human-readable description of the event. The method switches on the concrete event type and handles these service events:

| Event type | Description format |
|------------|--------------------|
| `ServiceCreatedEvent` | `Service '{ServiceName}' of type {ServiceType} created on port {Port}.` |
| `ServiceDeletedEvent` | `Service '{ServiceName}' of type {ServiceType} deleted.` |
| `ServiceStatusChangedEvent` | `Service '{ServiceName}' status changed from {OldStatus} to {NewStatus} at {ChangedAt:u}.` |
| `ServiceConfigurationUpdatedEvent` | `Service '{ServiceName}' configuration key '{ConfigurationKey}' changed from '{OldValue}' to '{NewValue}'.` |
| `ServiceHealthCheckFailedEvent` | `Health check failed for service '{ServiceName}': {ErrorMessage} (consecutive failures: {ConsecutiveFailures}).` |
| `ServiceHealthRecoveredEvent` | `Service '{ServiceName}' recovered with response time {ResponseTimeMs}ms.` |

Any other `DomainEvent` falls back to `ToString()`. If that returns `null`, the result is `string.Empty`.

- **Parameters**
  - `@event`: The event to describe.
- **Return value**: A `string` that is never `null` for a non-null event.
- **Throws**: `NullReferenceException` if `@event` is `null`. The method does not check for `null`.

### `IsCritical(this DomainEvent @event)`

Reports whether the event should be treated as critical. Currently only two event types are critical:

- `ServiceHealthCheckFailedEvent`
- `ServiceDeletedEvent`

All other events, including unknown `DomainEvent` subtypes, return `false`.

- **Parameters**
  - `@event`: The event to check.
- **Return value**: `true` if the event is a health-check failure or a deletion; otherwise, `false`. For a `null` argument the method returns `false`.
- **Throws**: None.

### `ToLogString(this DomainEvent @event)`

Builds a single log line in the form `[{UTC timestamp}] {Describe()}`.

- **Parameters**
  - `@event`: The event to format.
- **Return value**: A `string` such as `[2026-10-25 12:00:00Z] Service 'api' status changed from Running to Failed at 2026-10-25 12:00:00Z.`
- **Throws**: `NullReferenceException` if `@event` is `null`, because it calls `Describe()`.

The timestamp is `DateTime.UtcNow` at the time of the call, not the event's `OccurredAt`. Use `OccurredAt` yourself if you need the original occurrence time in the log.

## Usage

```csharp
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Events;

// Example 1: Describing a status change
var statusEvent = new ServiceStatusChangedEvent
{
    ServiceName = "api",
    OldStatus = ServiceStatus.Running,
    NewStatus = ServiceStatus.Failed,
    ChangedAt = DateTime.UtcNow
};

string text = statusEvent.Describe();
// "Service 'api' status changed from Running to Failed at 2026-10-25 12:00:00Z."

// Example 2: Routing critical events to an alert channel
DomainEvent domainEvent = new ServiceHealthCheckFailedEvent
{
    ServiceName = "api",
    ErrorMessage = "Timeout",
    ConsecutiveFailures = 3
};

if (domainEvent.IsCritical())
{
    // Send to the alert channel
}

// Example 3: Writing a log line
Console.WriteLine(domainEvent.ToLogString());
// "[2026-10-25 12:00:00Z] Health check failed for service 'api': Timeout (consecutive failures: 3)."
```

## Notes

- **Thread-safety**: The methods are stateless and safe to call from multiple threads. `ToLogString` reads the current UTC clock, so two calls on the same event can return different timestamps.
- **Null handling**: `IsCritical` is null-safe and returns `false`. `Describe` and `ToLogString` are not null-safe and throw `NullReferenceException`. Check for `null` before calling them.
- **Pattern matching**: `Describe` and `IsCritical` match on the runtime type. The six service event classes are `sealed`, so no subclass can reach a different branch. A new event type needs its own `case` in `Describe` to get a readable description; otherwise it falls back to `ToString()`.
- **Output format**: The message text is for humans and logs. Do not parse it. Read the event's properties instead.
