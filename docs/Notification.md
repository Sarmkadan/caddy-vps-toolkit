# Notification

The `Notification` class provides a unified structure for creating, configuring, and dispatching alert notifications within the `caddy-vps-toolkit`. It encapsulates the message content, metadata, and priority level, and provides methods to register the notification within the system and dispatch it asynchronously.

## API

### Properties

- **`Id`** (`string`): A unique identifier for the notification instance.
- **`Title`** (`string`): The header or subject of the notification.
- **`Message`** (`string`): The primary content body of the notification.
- **`Priority`** (`NotificationPriority`): Specifies the urgency level, allowing for filtering or prioritization in notification sinks.
- **`CreatedAt`** (`DateTime`): The timestamp indicating when the notification object was instantiated.
- **`Metadata`** (`Dictionary<string, string>`): A collection for key-value pairs, useful for attaching contextual information or routing hints for custom handlers.
- **`NotificationService`** (`NotificationService`): Provides access to the service instance responsible for processing the notification.

### Methods

- **`Register()`** (`void`): Registers this notification with the internal tracking system. This must be invoked before dispatching.
- **`SendAsync()`** (`async Task<bool>`): Dispatches the notification. There are three overloads to accommodate different delivery configurations. All return `true` if the notification was delivered successfully, and `false` otherwise.

## Usage

### Basic Notification

```csharp
var notification = new Notification
{
    Id = Guid.NewGuid().ToString(),
    Title = "Deployment Status",
    Message = "The deployment completed successfully.",
    Priority = NotificationPriority.Information
};

notification.Register();
bool success = await notification.SendAsync();
```

### Notification with Metadata

```csharp
var notification = new Notification
{
    Id = Guid.NewGuid().ToString(),
    Title = "Resource Warning",
    Message = "Memory usage exceeds 90%.",
    Priority = NotificationPriority.Warning,
    Metadata = new Dictionary<string, string> 
    { 
        { "NodeId", "prod-caddy-01" },
        { "Metric", "MemoryUsage" }
    }
};

notification.Register();
bool success = await notification.SendAsync();
```

## Notes

- **Thread Safety**: The `Notification` object is not inherently thread-safe. Concurrent modifications to the `Metadata` dictionary or property assignments from multiple threads should be avoided or externally synchronized.
- **Async Execution**: The `SendAsync` methods are I/O-bound and should be awaited. Blocking on these methods using `.Result` or `.Wait()` can lead to deadlocks depending on the synchronization context.
- **Registration Requirement**: Always invoke `Register()` prior to calling `SendAsync()` to ensure the notification is correctly tracked and initialized by the underlying `NotificationService`.
