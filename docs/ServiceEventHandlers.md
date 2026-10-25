# ServiceEventHandlers

`Events/ServiceEventHandlers.cs` defines four event handlers for service lifecycle and health events. Each handler implements `IEventHandler<TEvent>` and does two things in order: it writes a log entry through `ILogger`, then it sends a webhook through `IWebhookHandler`.

The four handlers are:

| Handler | Event | Log level | Webhook event type |
|---------|-------|-----------|--------------------|
| `ServiceCreatedEventHandler` | `ServiceCreatedEvent` | Info | `WebhookEventType.ServiceCreated` |
| `ServiceStatusChangedEventHandler` | `ServiceStatusChangedEvent` | Info | `WebhookEventType.ServiceStatusChanged` |
| `ServiceHealthCheckFailedEventHandler` | `ServiceHealthCheckFailedEvent` | Warning | `WebhookEventType.HealthCheckFailed` |
| `ServiceHealthRecoveredEventHandler` | `ServiceHealthRecoveredEvent` | Info | `WebhookEventType.HealthCheckRecovered` |

All four classes are `sealed` and live in the `CaddyVpsToolkit.Events` namespace. They are stateless after construction, so one instance can be subscribed to an event bus and reused for every publish.

The handlers do not catch exceptions. If the logger or the webhook handler throws, the exception propagates out of `HandleAsync`. Because the log call comes first, a logging failure also prevents the webhook from being sent.

`IWebhookHandler.TriggerAsync` returns `Task<bool>`. The handlers discard that result, so a webhook delivery that reports `false` does not fail the handler.

## API

### `ServiceCreatedEventHandler(ILogger logger, IWebhookHandler webhookHandler)`

```csharp
public ServiceCreatedEventHandler(ILogger logger, IWebhookHandler webhookHandler)
```

Creates a handler for `ServiceCreatedEvent`.

**Parameters**
- `logger` (`ILogger`): Receives the info-level message.
- `webhookHandler` (`IWebhookHandler`): Receives the `ServiceCreated` notification.

**Exceptions**
- `ArgumentNullException`: Thrown when `logger` or `webhookHandler` is `null`. The parameter name identifies the argument.

### `ServiceCreatedEventHandler.HandleAsync(ServiceCreatedEvent @event)`

```csharp
public async Task HandleAsync(ServiceCreatedEvent @event)
```

Logs the creation and sends the webhook. This method is the `IEventHandler<ServiceCreatedEvent>` implementation.

Log message:

```
Service created: {<ServiceName>} on port <Port>
```

Webhook payload (anonymous object):

| Property | Source |
|----------|--------|
| `ServiceName` | `@event.ServiceName` |
| `ServiceType` | `@event.ServiceType` |
| `Port` | `@event.Port` |
| `CreatedAt` | `@event.OccurredAt` |

**Parameters**
- `@event` (`ServiceCreatedEvent`): The event to handle.

**Return value**
- `Task`: Completes after the log call and the webhook call have both finished.

### `ServiceStatusChangedEventHandler(ILogger logger, IWebhookHandler webhookHandler)`

```csharp
public ServiceStatusChangedEventHandler(ILogger logger, IWebhookHandler webhookHandler)
```

Creates a handler for `ServiceStatusChangedEvent`.

**Parameters**
- `logger` (`ILogger`): Receives the info-level message.
- `webhookHandler` (`IWebhookHandler`): Receives the `ServiceStatusChanged` notification.

**Exceptions**
- `ArgumentNullException`: Thrown when `logger` or `webhookHandler` is `null`.

### `ServiceStatusChangedEventHandler.HandleAsync(ServiceStatusChangedEvent @event)`

```csharp
public async Task HandleAsync(ServiceStatusChangedEvent @event)
```

Logs the status transition and sends the webhook. This method is the `IEventHandler<ServiceStatusChangedEvent>` implementation.

Log message:

```
Service status changed: {<ServiceName>} from <OldStatus> to <NewStatus>
```

Webhook payload (anonymous object):

| Property | Source |
|----------|--------|
| `ServiceName` | `@event.ServiceName` |
| `OldStatus` | `@event.OldStatus` (`ServiceStatus`) |
| `NewStatus` | `@event.NewStatus` (`ServiceStatus`) |
| `ChangedAt` | `@event.ChangedAt` |

**Parameters**
- `@event` (`ServiceStatusChangedEvent`): The event to handle.

**Return value**
- `Task`: Completes after the log call and the webhook call have both finished.

### `ServiceHealthCheckFailedEventHandler(ILogger logger, IWebhookHandler webhookHandler)`

```csharp
public ServiceHealthCheckFailedEventHandler(ILogger logger, IWebhookHandler webhookHandler)
```

Creates a handler for `ServiceHealthCheckFailedEvent`.

**Parameters**
- `logger` (`ILogger`): Receives the warning-level message.
- `webhookHandler` (`IWebhookHandler`): Receives the `HealthCheckFailed` notification.

**Exceptions**
- `ArgumentNullException`: Thrown when `logger` or `webhookHandler` is `null`.

### `ServiceHealthCheckFailedEventHandler.HandleAsync(ServiceHealthCheckFailedEvent @event)`

```csharp
public async Task HandleAsync(ServiceHealthCheckFailedEvent @event)
```

Logs the health check failure as a warning and sends the webhook. This method is the `IEventHandler<ServiceHealthCheckFailedEvent>` implementation.

Log message:

```
Health check failed for {<ServiceName>}: <ErrorMessage> (consecutive: {@event.ConsecutiveFailures})
```

The `ConsecutiveFailures` value is not interpolated in the current code. See [Known issues](#known-issues).

Webhook payload (anonymous object):

| Property | Source |
|----------|--------|
| `ServiceName` | `@event.ServiceName` |
| `ErrorMessage` | `@event.ErrorMessage` |
| `ConsecutiveFailures` | `@event.ConsecutiveFailures` |

**Parameters**
- `@event` (`ServiceHealthCheckFailedEvent`): The event to handle.

**Return value**
- `Task`: Completes after the log call and the webhook call have both finished.

### `ServiceHealthRecoveredEventHandler(ILogger logger, IWebhookHandler webhookHandler)`

```csharp
public ServiceHealthRecoveredEventHandler(ILogger logger, IWebhookHandler webhookHandler)
```

Creates a handler for `ServiceHealthRecoveredEvent`.

**Parameters**
- `logger` (`ILogger`): Receives the info-level message.
- `webhookHandler` (`IWebhookHandler`): Receives the `HealthCheckRecovered` notification.

**Exceptions**
- `ArgumentNullException`: Thrown when `logger` or `webhookHandler` is `null`.

### `ServiceHealthRecoveredEventHandler.HandleAsync(ServiceHealthRecoveredEvent @event)`

```csharp
public async Task HandleAsync(ServiceHealthRecoveredEvent @event)
```

Logs the recovery and sends the webhook. This method is the `IEventHandler<ServiceHealthRecoveredEvent>` implementation.

Log message:

```
Service recovered: {<ServiceName>} (response: <ResponseTimeMs>ms)
```

Webhook payload (anonymous object):

| Property | Source |
|----------|--------|
| `ServiceName` | `@event.ServiceName` |
| `ResponseTimeMs` | `@event.ResponseTimeMs` |
| `RecoveredAt` | `@event.OccurredAt` |

**Parameters**
- `@event` (`ServiceHealthRecoveredEvent`): The event to handle.

**Return value**
- `Task`: Completes after the log call and the webhook call have both finished.

## Usage

### Example 1: Subscribing the handlers to the event bus

The handlers are normally wired up at startup. Each handler is subscribed to the event type it handles.

```csharp
using CaddyVpsToolkit.Events;
using CaddyVpsToolkit.Integration;
using CaddyVpsToolkit.Middleware;

public static class ServiceEventWiring
{
    public static void Subscribe(IEventBus bus, ILogger logger, IWebhookHandler webhooks)
    {
        bus.Subscribe<ServiceCreatedEvent>(new ServiceCreatedEventHandler(logger, webhooks));
        bus.Subscribe<ServiceStatusChangedEvent>(new ServiceStatusChangedEventHandler(logger, webhooks));
        bus.Subscribe<ServiceHealthCheckFailedEvent>(new ServiceHealthCheckFailedEventHandler(logger, webhooks));
        bus.Subscribe<ServiceHealthRecoveredEvent>(new ServiceHealthRecoveredEventHandler(logger, webhooks));
    }
}
```

### Example 2: Publishing a service creation event

Publishing the event runs the subscribed handler. The handler logs the creation and sends a `ServiceCreated` webhook.

```csharp
await bus.PublishAsync(new ServiceCreatedEvent
{
    ServiceName = "web",
    ServiceType = ServiceType.WebApplication,
    Port = 8080,
    ExecutablePath = "/opt/web/app"
});

// Log: Service created: {web} on port 8080
// Webhook: ServiceCreated { ServiceName, ServiceType, Port, CreatedAt }
```

### Example 3: Reporting a status change and a health recovery

```csharp
await bus.PublishAsync(new ServiceStatusChangedEvent
{
    ServiceName = "web",
    OldStatus = ServiceStatus.Running,
    NewStatus = ServiceStatus.Failed,
    ChangedAt = DateTime.UtcNow
});

await bus.PublishAsync(new ServiceHealthRecoveredEvent
{
    ServiceName = "web",
    ResponseTimeMs = 42
});

// Log: Service status changed: {web} from Running to Failed
// Log: Service recovered: {web} (response: 42ms)
```

### Example 4: Calling a handler directly

A handler can be called without an event bus, for example in a unit test. Pass a logger and a webhook handler that you control.

```csharp
var handler = new ServiceHealthCheckFailedEventHandler(logger, webhookHandler);

await handler.HandleAsync(new ServiceHealthCheckFailedEvent
{
    ServiceName = "api",
    ErrorMessage = "connection refused",
    ConsecutiveFailures = 3
});
```

### Example 5: Unsubscribing a handler

`IEventBus.Unsubscribe` removes a handler by reference. Keep the instance you subscribed so you can pass the same one back.

```csharp
var created = new ServiceCreatedEventHandler(logger, webhooks);

bus.Subscribe<ServiceCreatedEvent>(created);
// ... later
bus.Unsubscribe<ServiceCreatedEvent>(created);
```

## Known issues

- **`ConsecutiveFailures` is not interpolated.** In `ServiceHealthCheckFailedEventHandler.HandleAsync`, the log string uses `{{@event.ConsecutiveFailures}}`. The doubled braces produce a literal `{`, and the expression is not evaluated. The log line therefore reads `(consecutive: {@event.ConsecutiveFailures})`. The webhook payload is not affected and carries the correct value. The fix is to write `{@event.ConsecutiveFailures}` in the interpolated string.

## Notes

- Subscribe each handler to exactly one event type. Each handler's `HandleAsync` parameter is typed to one event class.
- The handlers do not check for duplicate subscriptions. If the same handler instance is subscribed twice, it runs twice for each publish.
- Do not depend on the order in which different handlers run for the same event. Each handler is independent.
- `CreatedAt` and `RecoveredAt` are copied from `OccurredAt`, which `DomainEvent` sets to `DateTime.UtcNow` when the event is constructed. `ChangedAt` is set by the code that publishes the event, so its time zone depends on that code.
