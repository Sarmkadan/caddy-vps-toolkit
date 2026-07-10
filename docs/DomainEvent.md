# DomainEvent

`DomainEvent` is a lightweight in-process event bus that facilitates decoupled communication between domain components. It provides typed subscription, unsubscription, and asynchronous publication of domain events, allowing aggregates and services to react to state changes without direct coupling. Each event carries a unique identifier, the time it occurred, and the identifier of the originating aggregate.

## API

### `string EventId`

Gets the unique identifier for this event instance. This value is assigned upon creation and remains immutable for the lifetime of the event.

### `DateTime OccurredAt`

Gets the UTC timestamp at which this event was raised. This value is captured once during instantiation and does not change.

### `string AggregateId`

Gets the identifier of the aggregate root that produced this event. This establishes traceability back to the source entity within the domain model.

### `void Subscribe<TEvent>()`

Registers the calling component as a subscriber for events of type `TEvent`. The type parameter must be a concrete type derived from `DomainEvent`. Subsequent calls to `PublishAsync<TEvent>` will notify this subscriber.

- **Type parameter `TEvent`**: The specific event type to subscribe to.
- **Throws**: `InvalidOperationException` if the subscriber is already registered for the same event type.

### `void Unsubscribe<TEvent>()`

Removes a previously registered subscription for events of type `TEvent`. After this call, the component will no longer receive notifications when `PublishAsync<TEvent>` is invoked.

- **Type parameter `TEvent`**: The specific event type to unsubscribe from.
- **Throws**: `InvalidOperationException` if no subscription exists for the given event type.

### `async Task PublishAsync<TEvent>()`

Asynchronously notifies all current subscribers of the specified event type. The event instance itself is passed to each subscriber's handler. Execution order among subscribers is non-deterministic.

- **Type parameter `TEvent`**: The event type being published.
- **Returns**: A `Task` that completes when all subscriber handlers have finished executing.
- **Throws**: `AggregateException` wrapping any exceptions thrown by individual subscriber handlers. The publication attempt continues to all subscribers even if one handler fails.

### `int GetSubscriberCount<TEvent>()`

Returns the number of active subscribers currently registered for the specified event type.

- **Type parameter `TEvent`**: The event type to query.
- **Returns**: A non-negative integer representing the subscriber count.

## Usage

### Example 1: Basic subscription and publication

```csharp
public class OrderPlaced : DomainEvent
{
    public string OrderId { get; init; }
}

// Subscriber registers interest
var orderService = new OrderService();
orderService.Subscribe<OrderPlaced>();

// Elsewhere in the domain, an aggregate raises the event
var evt = new OrderPlaced
{
    EventId = Guid.NewGuid().ToString(),
    OccurredAt = DateTime.UtcNow,
    AggregateId = "order-123",
    OrderId = "order-123"
};

await evt.PublishAsync<OrderPlaced>();

// Later, subscriber cleans up
orderService.Unsubscribe<OrderPlaced>();
```

### Example 2: Multiple subscribers with error handling

```csharp
public class InventoryReserved : DomainEvent
{
    public string ReservationId { get; init; }
}

var inventoryHandler = new InventoryHandler();
var auditHandler = new AuditHandler();

inventoryHandler.Subscribe<InventoryReserved>();
auditHandler.Subscribe<InventoryReserved>();

var evt = new InventoryReserved
{
    EventId = Guid.NewGuid().ToString(),
    OccurredAt = DateTime.UtcNow,
    AggregateId = "reservation-456",
    ReservationId = "reservation-456"
};

try
{
    await evt.PublishAsync<InventoryReserved>();
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
    {
        Console.WriteLine($"Handler failed: {inner.Message}");
    }
}

Console.WriteLine($"Subscribers: {evt.GetSubscriberCount<InventoryReserved>()}");
```

## Notes

- Subscriptions are bound to the calling object instance, not a static context. Each instance maintains its own subscriber list, meaning two instances of the same class must subscribe independently.
- Calling `Subscribe<TEvent>` when already subscribed throws `InvalidOperationException`. Check with `GetSubscriberCount<TEvent>` or maintain your own tracking flag if duplicate registration is a concern.
- `PublishAsync<TEvent>` captures the subscriber list at the moment of invocation. Subscribers added or removed during publication do not affect the in-flight notification batch.
- Exceptions thrown by subscriber handlers are aggregated and surfaced after all handlers have been invoked. A single failing handler does not prevent other subscribers from receiving the event.
- This type is not thread-safe by default. Concurrent calls to `Subscribe`, `Unsubscribe`, and `PublishAsync` from multiple threads may result in race conditions. External synchronization is required if the event bus is shared across threads.
- The `AggregateId` property is purely informational and does not influence subscription routing or delivery logic.
