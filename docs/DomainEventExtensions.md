# DomainEventExtensions

Provides extension methods for working with domain events, particularly for calculating event age, formatting, and comparing events within the same aggregate.

## API

### `GetAge(this IDomainEvent @event)`

Calculates the time elapsed since the domain event occurred.

- **Parameters**:
  - `@event`: The domain event to calculate the age of.
- **Return value**: A `TimeSpan` representing the time elapsed since the event occurred.
- **Throws**: `ArgumentNullException` if `@event` is `null`.

### `ToDetailedString(this IDomainEvent @event)`

Formats the domain event into a human-readable string with detailed information.

- **Parameters**:
  - `@event`: The domain event to format.
- **Return value**: A `string` containing the detailed representation of the event.
- **Throws**: `ArgumentNullException` if `@event` is `null`.

### `IsRecent(this IDomainEvent @event, TimeSpan threshold)`

Determines whether the domain event is recent based on a given time threshold.

- **Parameters**:
  - `@event`: The domain event to evaluate.
  - `threshold`: The maximum allowed age for the event to be considered recent.
- **Return value**: `true` if the event's age is less than or equal to the threshold; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `@event` is `null`.

### `HasSameAggregate(this IDomainEvent @event, IAggregateRoot aggregate)`

Checks whether the domain event belongs to a specific aggregate root.

- **Parameters**:
  - `@event`: The domain event to check.
  - `aggregate`: The aggregate root to compare against.
- **Return value**: `true` if the event's aggregate identifier matches the provided aggregate's identifier; otherwise, `false`.
- **Throws**: `ArgumentNullException` if either `@event` or `aggregate` is `null`.

## Usage

```csharp
// Example 1: Checking if an event is recent
var @event = new OrderCreatedEvent(OrderId.New(), DateTime.UtcNow.AddMinutes(-5));
var isRecent = @event.IsRecent(TimeSpan.FromMinutes(10)); // Returns true

// Example 2: Comparing events within the same aggregate
var aggregate = new OrderAggregate(OrderId.New());
var event1 = new OrderCreatedEvent(aggregate.Id, DateTime.UtcNow);
var event2 = new OrderPaidEvent(aggregate.Id, DateTime.UtcNow.AddSeconds(10));
var hasSameAggregate = event1.HasSameAggregate(aggregate); // Returns true
```

## Notes

- **Thread-safety**: All methods are thread-safe as they only read immutable data or perform stateless computations.
- **Edge cases**:
  - `GetAge` and `IsRecent` assume the event's timestamp is in UTC; no timezone conversion is performed.
  - `HasSameAggregate` performs a direct identifier comparison; no additional validation is applied.
  - If an event's timestamp is in the future (unlikely but possible), `GetAge` will return a negative `TimeSpan`.
