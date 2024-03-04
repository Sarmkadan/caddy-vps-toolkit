# ServiceCreatedEventHandler

Represents an asynchronous event handler that processes `ServiceCreatedEvent` notifications within the Caddy VPS Toolkit. It is typically invoked by an event bus or mediator when a new service registration occurs, allowing downstream logic such as persistence, health‑check initialization, or notification dispatch to run in a non‑blocking manner.

## API

### ServiceCreatedEventHandler()
Initializes a new instance of the `ServiceCreatedEventHandler` class. The constructor does not take any parameters and prepares the handler for use. It does not throw exceptions under normal circumstances.

### public async Task HandleAsync(ServiceCreatedEvent @event, CancellationToken cancellationToken = default)
Handles a `ServiceCreatedEvent` asynchronously.

- **@event** – The event object containing details about the newly created service (e.g., service ID, configuration, endpoints). Must not be `null`.
- **cancellationToken** – Optional token to monitor for cancellation requests. The default value is `None`.

**Return Value**  
A `Task` that completes when the event handling logic finishes. The method completes successfully, the task ends in a faulted state if an exception occurs during processing.

**Exceptions**  
- `ArgumentNull` that represents the asynchronous operation. The task completes when all handling steps have been executed.

**When it Throws**  
- `ArgumentNullException` if `@event` is `null`.  
- `InvalidOperationException` if required internal dependencies (e.g., a service repository or health‑check manager) have not been initialized.  
- Any exception thrown by called dependencies (e.g., data access layer, external APIs) is propagated back to the caller.

## Usage

### Example 1: Registering with an event mediator
```csharp
using Caddy.Vps.Toolkit.Events;
using Caddy.Vps.Toolkit.Handlers;
using MediatR;

// Assume mediator is an instance of IMediator already configured
var mediator = serviceProvider.GetRequiredService<IMediator>();

// Register the handler for ServiceCreatedEvent
mediator.Publish(new ServiceCreatedEvent
{
    ServiceId = Guid.NewGuid(),
    Name = "web-api",
    Endpoint = "http://localhost:5000"
});
// The mediator will invoke ServiceCreatedEventHandler.HandleAsync internally.
```

### Example 2: Direct invocation in a unit test
```csharp
using System.Threading;
using System.Threading.Tasks;
using Caddy.Vps.Toolkit.Events;
using Caddy.Vps.Toolkit.Handlers;
using Xunit;

public class ServiceCreatedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_InvokesDependencies_WhenEventIsValid()
    {
        // Arrange
        var handler = new ServiceCreatedEventHandler(); // dependencies mocked via constructor or properties
        var @event = new ServiceCreatedEvent
        {
            ServiceId = Guid.Empty,
            Name = "test-service",
            Endpoint = "http://test.local"
        };
        var cts = new CancellationTokenSource();

        // Act
        await handler.HandleAsync(@event, cts.Token);

        // Assert – verify that expected side‑effects occurred (e.g., repository.Save called)
        // Assertions omitted for brevity
    }
}
```

## Notes
- The handler is stateless after construction; multiple threads can invoke `HandleAsync` concurrently without internal race conditions. Any state held by injected dependencies must be thread‑safe or appropriately synchronized.
- If the handler captures mutable state (e.g., caching or counters), external synchronization is required to guarantee correct behavior under concurrent calls.
- The method respects the supplied `cancellationToken`; operation will cease as soon as cancellation is requested, throwing `OperationCanceledException` if the token is triggered before completion.
- Do not rely on the order of execution relative to other event handlers; treat each invocation as independent.
