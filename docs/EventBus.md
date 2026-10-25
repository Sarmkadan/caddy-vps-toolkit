# EventBus

EventBus is the in-memory publish/subscribe channel for domain events in the Caddy VPS Toolkit. Services publish a `DomainEvent` and every handler subscribed to that event type runs. Handlers run concurrently, and a failing handler does not stop the others.

The source lives in `Events/EventBus.cs` and contains:

- `EventBus` implements `IEventBus`. It is the only implementation in the codebase and is `sealed`.
- `DomainEvent` is the abstract base class for all events. See [DomainEvent](DomainEvent.md).
- `IEventHandler<TEvent>` is the interface that every handler implements.

The bus lives in process memory. Subscriptions are lost on restart, and events are not persisted or delivered to other processes. The bus is intended for single-server deployments.

`AddEventBus()` registers `EventBus` as a singleton behind `IEventBus`, so all consumers that resolve `IEventBus` share one set of subscriptions.

## Core Types

### `DomainEvent`

```csharp
public abstract class DomainEvent
{
    public string EventId { get; }        // Guid string, assigned at construction
    public DateTime OccurredAt { get; }   // UTC, assigned at construction
    public string AggregateId { get; set; }
}
```

`EventId` and `OccurredAt` are set once when the event is created. `AggregateId` is optional and can be set by the publisher to identify the entity the event is about.

### `IEventHandler<in TEvent>`

```csharp
public interface IEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent @event);
}
```

The interface is contravariant in `TEvent`. A handler for a base event type can therefore be used where a handler for a derived type is expected.

### `IEventBus`

```csharp
public interface IEventBus
{
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent;
    void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent;
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
}
```

Code that depends on eventing should take `IEventBus`. `GetSubscriberCount` and `SubscribeWithToken` exist only on `EventBus` and are not part of the interface.

## API

### `EventBus`

#### `void Subscribe<TEvent>(IEventHandler<TEvent> handler)`

Registers `handler` for events of type `TEvent`.

- **Parameters**
  - `handler`: Handler to invoke when a `TEvent` is published.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentNullException` if `handler` is `null`.

The same handler instance can be registered more than once. Each registration is stored separately, so the handler runs once per registration on every publish. Registration is keyed by the generic type argument `TEvent`, not by the runtime type of the event (see [Behaviour Notes](#behaviour-notes)).

#### `void Unsubscribe<TEvent>(IEventHandler<TEvent> handler)`

Removes one registration of `handler` for `TEvent`.

- **Parameters**
  - `handler`: Handler to remove.
- **Return value**: None.
- **Exceptions**: None. A `null` handler, an unknown event type, or a handler that is not registered is ignored.

Only the first matching registration is removed. If the handler was subscribed twice, call `Unsubscribe` twice to remove both. Handlers are compared with `Equals`, which is reference equality unless the handler type overrides it.

#### `Task PublishAsync<TEvent>(TEvent @event)`

Delivers `@event` to every handler registered for `TEvent`.

- **Parameters**
  - `@event`: Event to deliver.
- **Return value**: A `Task` that completes when every handler has finished.
- **Exceptions**: Throws `ArgumentNullException` if `@event` is `null`. Exceptions thrown by handlers are not propagated. Each one is written to the console as `Error handling event {TEvent}: {message}` and the remaining handlers still run.

The handler list is copied under the lock before any handler is invoked. Handlers subscribed or unsubscribed while a publish is in progress do not affect that publish. If no handler is registered for `TEvent`, the method returns a completed task.

Handlers are started in subscription order, but they run concurrently. The method does not wait for one handler before starting the next, so a handler should not assume that another handler has finished.

#### `int GetSubscriberCount<TEvent>()`

Returns the number of registrations for `TEvent`.

- **Parameters**: None.
- **Return value**: The count of registrations, including duplicates. `0` if the event type has no registrations.
- **Exceptions**: None.

#### `IDisposable SubscribeWithToken<TEvent>(IEventHandler<TEvent> handler)`

Subscribes `handler` and returns a token. Disposing the token unsubscribes the handler.

- **Parameters**
  - `handler`: Handler to register.
- **Return value**: An `IDisposable` token. Disposing it removes this registration. Disposing it more than once has no further effect.
- **Exceptions**: Throws `ArgumentNullException` if `handler` is `null`, through the underlying `Subscribe` call.

The token removes only the registration it created. If the same handler was subscribed twice, disposing one token leaves the other registration in place.

## Usage Examples

### Registering the bus

Resolve the bus from the container. The application does not need to create it by hand.

```csharp
var services = new ServiceCollection();
services.AddEventBus();

using var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IEventBus>();
```

### Writing a handler

```csharp
public sealed class ServiceStatusLogger : IEventHandler<ServiceStatusChangedEvent>
{
    public Task HandleAsync(ServiceStatusChangedEvent @event)
    {
        Console.WriteLine(
            $"{@event.ServiceName}: {@event.OldStatus} -> {@event.NewStatus} at {@event.ChangedAt:O}");
        return Task.CompletedTask;
    }
}
```

### Subscribing and publishing

```csharp
var bus = new EventBus();
var logger = new ServiceStatusLogger();

bus.Subscribe(logger);

await bus.PublishAsync(new ServiceStatusChangedEvent
{
    ServiceName = "api",
    OldStatus = ServiceStatus.Stopped,
    NewStatus = ServiceStatus.Running,
    ChangedAt = DateTime.UtcNow,
    AggregateId = "api"
});

bus.Unsubscribe(logger);
```

### Scoped subscriptions with `SubscribeWithToken`

Use the token when a subscription should last only as long as a component, such as a dashboard view or a background worker.

`EventBus` itself is not disposable, so the token is the only thing that needs a scope:

```csharp
var bus = new EventBus();

using (bus.SubscribeWithToken(new ServiceStatusLogger()))
{
    await bus.PublishAsync(statusEvent);   // handler runs
}

await bus.PublishAsync(statusEvent);       // handler no longer registered
```

### Checking registrations

```csharp
int listeners = bus.GetSubscriberCount<ServiceStatusChangedEvent>();
if (listeners == 0)
{
    // Nothing is listening; the publish would do nothing.
}
```

## Behaviour Notes

- **Dispatch uses the generic type argument.** `PublishAsync<TEvent>` looks up handlers by `typeof(TEvent)`, not by the runtime type of the object. Calling `PublishAsync<DomainEvent>(new ServiceCreatedEvent { ... })` reaches only handlers subscribed to `DomainEvent`. Handlers subscribed to `ServiceCreatedEvent` are not called. Let the compiler infer `TEvent`, or pass the concrete type explicitly.
- **Handler failures are swallowed.** An exception from `HandleAsync` is logged to `Console` and does not reach the caller of `PublishAsync`. Handlers that must not fail silently should catch and report errors themselves.
- **Publish waits for every handler.** `PublishAsync` returns only after all handlers complete, so a slow handler delays the publisher. Hand off slow work to a background queue inside the handler if the publisher must not wait. Ordering between handlers is not guaranteed, because they run concurrently.
- **Duplicate subscriptions are allowed.** Subscribing the same handler twice does not deduplicate. It runs twice per publish.
- **Subscriptions are not persistent.** Restarting the process clears every registration. Components must subscribe again on startup.
- **Thread safety.** `Subscribe`, `Unsubscribe`, `PublishAsync`, and `GetSubscriberCount` take the same lock and are safe to call from multiple threads. The lock is not held while handlers run, so a handler can subscribe or publish without deadlocking.
- **Unsubscribe matches by equality.** A handler type that overrides `Equals` may be removed by a different instance that compares equal. Handlers without an override are matched by reference only.
