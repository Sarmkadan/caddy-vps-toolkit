# ServiceEvents

`ServiceEvents` is the set of domain events that describe the lifecycle and health of a managed service. The source lives in `Events/ServiceEvents.cs` under the `CaddyVpsToolkit.Events` namespace. Every type in the file derives from [DomainEvent](DomainEvent.md), so each instance receives a generated `EventId` and an `OccurredAt` timestamp automatically, and can be published through the [EventBus](EventBus.md).

The file defines six sealed event classes:

| Event | Raised when |
|-------|-------------|
| `ServiceCreatedEvent` | A new managed service is created. |
| `ServiceDeletedEvent` | A managed service is deleted. |
| `ServiceStatusChangedEvent` | A service moves from one `ServiceStatus` to another. |
| `ServiceConfigurationUpdatedEvent` | One configuration key of a service is changed. |
| `ServiceHealthCheckFailedEvent` | A health check for a service fails. |
| `ServiceHealthRecoveredEvent` | A service that was unhealthy passes a health check again. |

The file itself does not publish events. Publishing is the responsibility of the component that performs the action, such as a service management or health monitoring service.

The events depend on two enumerations from `CaddyVpsToolkit.Core` (see `Core/Enums.cs`):

- `ServiceType`: `WebApplication`, `ApiService`, `Worker`, `Database`, `Cache`, `Custom`.
- `ServiceStatus`: `Stopped`, `Running`, `Restarting`, `Failed`, `Unknown`.

## API

All properties have public getters and setters. None of the event classes define a custom constructor, so instances are created with an object initializer. Properties that are not set keep their default values (`null` for strings, `0` for integers, the first enum member for enums, and `DateTime.MinValue` for `ChangedAt`).

### `ServiceCreatedEvent`

Raised when a new service is registered with the toolkit.

| Property | Type | Description |
|----------|------|-------------|
| `ServiceName` | `string` | Name of the service that was created. |
| `ServiceType` | `ServiceType` | Kind of service that was created. |
| `Port` | `int` | Port on which the service listens. |
| `ExecutablePath` | `string` | Path to the executable that runs the service. |

#### `override string ToString()`

Returns a single-line representation of the event that includes `ServiceName`, `ServiceType`, `Port`, and `ExecutablePath`, in the form `ServiceCreatedEvent { ServiceName = ..., ServiceType = ..., Port = ..., ExecutablePath = ... }`.

- **Parameters**: None.
- **Return value**: The formatted string. Unset string properties appear as empty text.
- **Exceptions**: None.

This is the only event in the file that overrides `ToString`. The other events use the default `object.ToString`, which returns the type name.

### `ServiceDeletedEvent`

Raised when a service is removed.

| Property | Type | Description |
|----------|------|-------------|
| `ServiceName` | `string` | Name of the service that was deleted. |
| `ServiceType` | `ServiceType` | Kind of service that was deleted. |

### `ServiceStatusChangedEvent`

Raised when the status of a service changes.

| Property | Type | Description |
|----------|------|-------------|
| `ServiceName` | `string` | Name of the service whose status changed. |
| `OldStatus` | `ServiceStatus` | Status before the change. |
| `NewStatus` | `ServiceStatus` | Status after the change. |
| `ChangedAt` | `DateTime` | Time at which the change happened. The event does not set this value itself, so the publisher should assign it. |

### `ServiceConfigurationUpdatedEvent`

Raised when a single configuration value of a service is updated.

| Property | Type | Description |
|----------|------|-------------|
| `ServiceName` | `string` | Name of the service whose configuration changed. |
| `ConfigurationKey` | `string` | Key of the configuration value that changed. |
| `OldValue` | `string` | Value before the update. |
| `NewValue` | `string` | Value after the update. |

### `ServiceHealthCheckFailedEvent`

Raised when a health check for a service fails.

| Property | Type | Description |
|----------|------|-------------|
| `ServiceName` | `string` | Name of the service whose health check failed. |
| `ErrorMessage` | `string` | Description of the failure. |
| `ConsecutiveFailures` | `int` | Number of failed checks in a row, including this one. |

### `ServiceHealthRecoveredEvent`

Raised when a service that had been failing its health checks responds successfully again.

| Property | Type | Description |
|----------|------|-------------|
| `ServiceName` | `string` | Name of the recovered service. |
| `ResponseTimeMs` | `int` | Response time of the successful check, in milliseconds. |

## Usage

### Example 1: Publishing a service creation

```csharp
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Events;

IEventBus bus = provider.GetRequiredService<IEventBus>();

await bus.PublishAsync(new ServiceCreatedEvent
{
    AggregateId = "billing-api",
    ServiceName = "billing-api",
    ServiceType = ServiceType.ApiService,
    Port = 5100,
    ExecutablePath = "/opt/billing-api/billing-api"
});
```

`EventId` and `OccurredAt` are set by the base class, so the caller does not need to provide them.

### Example 2: Handling a status change

```csharp
using System.Threading.Tasks;
using CaddyVpsToolkit.Events;

public sealed class ServiceStatusHandler : IEventHandler<ServiceStatusChangedEvent>
{
    public Task HandleAsync(ServiceStatusChangedEvent @event)
    {
        if (@event.NewStatus == ServiceStatus.Failed)
        {
            Console.WriteLine($"{@event.ServiceName} failed at {@event.ChangedAt:O}");
        }

        return Task.CompletedTask;
    }
}

bus.Subscribe(new ServiceStatusHandler());
```

### Example 3: Publishing a status change

```csharp
await bus.PublishAsync(new ServiceStatusChangedEvent
{
    ServiceName = "billing-api",
    OldStatus = ServiceStatus.Running,
    NewStatus = ServiceStatus.Failed,
    ChangedAt = DateTime.UtcNow
});
```

### Example 4: Reporting health failures and recovery

A health monitor can count consecutive failures and publish the failure event on each miss. It publishes the recovery event once the service responds again.

```csharp
int failures = 0;

async Task OnHealthCheckAsync(string serviceName, bool healthy, string? error, int responseTimeMs)
{
    if (healthy)
    {
        if (failures > 0)
        {
            await bus.PublishAsync(new ServiceHealthRecoveredEvent
            {
                ServiceName = serviceName,
                ResponseTimeMs = responseTimeMs
            });
        }

        failures = 0;
        return;
    }

    failures++;

    await bus.PublishAsync(new ServiceHealthCheckFailedEvent
    {
        ServiceName = serviceName,
        ErrorMessage = error ?? "Unknown error",
        ConsecutiveFailures = failures
    });
}
```

### Example 5: Recording a configuration change

```csharp
await bus.PublishAsync(new ServiceConfigurationUpdatedEvent
{
    ServiceName = "billing-api",
    ConfigurationKey = "RateLimitPerMinute",
    OldValue = "100",
    NewValue = "250"
});
```

## Notes

- Each event class is `sealed`. To extend the set of events, add a new class that derives from `DomainEvent` rather than inheriting from one of these types.
- The events are plain data carriers. They do not validate their values, so an empty `ServiceName` or a negative `Port` is accepted and passed to subscribers as is. Validation belongs in the publisher or in a handler.
- `ServiceStatusChangedEvent.ChangedAt` is not assigned automatically. Unlike `OccurredAt` from the base class, which is set to the UTC time at creation, `ChangedAt` keeps its default value unless the publisher sets it.
- `ServiceHealthCheckFailedEvent.ConsecutiveFailures` is supplied by the caller. The event does not count failures, so the publisher is responsible for resetting the count after a success.
- Handlers that subscribe to these events run through the [EventBus](EventBus.md), which runs handlers concurrently and does not stop other handlers when one fails.
