# INotificationStateStore

`INotificationStateStore` tracks the recent history of notifications so that repeated alerts for the same condition can be suppressed, and so that a rise in severity can be detected and sent even while a suppression window is open. It is the building block for duplicate suppression and escalation in the notification pipeline.

The interface and its in-memory implementation are declared in `Notifications/INotificationStateStore.cs` under the `CaddyVpsToolkit.Notifications` namespace. The file also declares `INotificationStateStoreFactory` and `InMemoryNotificationStateStoreFactory`, which create store instances. The severity type, `NotificationPriority`, is declared in `Notifications/NotificationService.cs` in the same namespace.

State is kept per suppression key. A key is any string that identifies "the same notification" to the caller, for example a service name plus an alert type. The store does not persist anything across process restarts when the in-memory implementation is used.

## API

### INotificationStateStore

```csharp
public interface INotificationStateStore : IDisposable
```

A store that records notifications by key and decides whether each one should be sent. Because it derives from `IDisposable`, callers should dispose it when they are done with it. Implementations may be in-memory or backed by persistent storage.

### RecordNotification

```csharp
(bool ShouldSuppress, bool ShouldEscalate) RecordNotification(string key, NotificationPriority severity, int suppressionWindow)
```

Records an occurrence of a notification and returns the decision for it. Call this once per notification you are about to send. The return value is a tuple:

- `ShouldSuppress`: `true` when the notification is a duplicate that falls inside the suppression window and is not an escalation. The caller should not send it.
- `ShouldEscalate`: `true` when `severity` is higher than the severity last recorded for `key`. This is reported regardless of whether the window has elapsed.

When the notification is not suppressed, the store records the current UTC time and `severity` as the new state for `key`. When it is suppressed, the stored state is left unchanged, so the window is measured from the last notification that was actually sent.

**Parameters:**
- `key` (`string`): The suppression key that identifies the notification. Notifications with the same key share state.
- `severity` (`NotificationPriority`): The priority of this occurrence. Ordered `Low` < `Normal` < `High` < `Critical`.
- `suppressionWindow` (`int`): The window in seconds. A duplicate at the same or lower severity is suppressed while less than this many seconds have passed since the last sent notification for the key. A value of `0` or less disables suppression in practice, because no duplicate falls inside the window.

**Returns:**
- `(bool ShouldSuppress, bool ShouldEscalate)`: The decision described above.

**Decision table** (for a key that already has state):

| Within window | Severity vs. last sent | Result `(ShouldSuppress, ShouldEscalate)` | State updated |
|---------------|------------------------|-------------------------------------------|---------------|
| Yes | Same or lower | `(true, false)` | No |
| Yes | Higher | `(false, true)` | Yes |
| No | Same or lower | `(false, false)` | Yes |
| No | Higher | `(false, true)` | Yes |

A key with no prior state always returns `(false, false)` and is recorded.

Note that a lower severity within the window is suppressed, and the stored severity stays at the higher value. A later notification at that higher severity is therefore not treated as an escalation until a higher one arrives.

**Exceptions:**
- `ObjectDisposedException`: Thrown by `InMemoryNotificationStateStore` when the store has been disposed.

### Clear

```csharp
void Clear()
```

Removes all tracked state, so every key is treated as new on its next `RecordNotification` call. Intended for tests or a manual reset. `InMemoryNotificationStateStore` allows `Clear` to be called even after `Dispose`, where it simply empties an already-empty dictionary.

**Parameters:** None.

**Returns:** `void`.

### InMemoryNotificationStateStore

```csharp
public sealed class InMemoryNotificationStateStore : INotificationStateStore
```

The default implementation. It keeps a dictionary of `key` to `(LastSent, Severity)` in process memory and guards each operation with a lock, so one instance can be shared across threads. State is lost when the process exits.

The class has a finalizer that calls `Dispose`, but relying on it is not recommended. Dispose stores explicitly, usually with a `using` statement.

### Dispose

```csharp
public void Dispose()
```

Clears all state and marks the store as disposed. Calling `Dispose` more than once has no further effect. After disposal, `RecordNotification` throws `ObjectDisposedException`.

**Parameters:** None.

**Returns:** `void`.

### INotificationStateStoreFactory

```csharp
public interface INotificationStateStoreFactory
```

Creates new store instances. Use it when a component needs its own independent store, for example one per notification channel, rather than sharing one.

### Create

```csharp
INotificationStateStore Create()
```

Returns a new, empty store. The caller owns the returned instance and is responsible for disposing it.

**Parameters:** None.

**Returns:**
- `INotificationStateStore`: A new store. Its state is independent of any store returned by an earlier call.

### InMemoryNotificationStateStoreFactory

```csharp
public sealed class InMemoryNotificationStateStoreFactory : INotificationStateStoreFactory
```

Implements `INotificationStateStoreFactory` by returning a new `InMemoryNotificationStateStore` from each `Create` call.

## Usage

### Basic suppression

Check the decision before sending. Use one store for the lifetime of the component that sends alerts, so that its state persists between calls.

```csharp
using CaddyVpsToolkit.Notifications;

using var store = new InMemoryNotificationStateStore();

// 15-minute window, measured in seconds
const int windowSeconds = 15 * 60;

var (suppress, escalate) = store.RecordNotification(
    "disk-usage:/var/lib/docker",
    NotificationPriority.High,
    windowSeconds);

if (!suppress)
{
    // Send the notification. escalate is true when severity rose since the last send.
    await notifier.SendAsync(BuildMessage(escalate));
}
```

The first call for a key is always sent. Identical calls within 15 minutes are suppressed, and the window is not extended by suppressed calls.

### Detecting escalation

A rise in severity is sent immediately, even inside an open window:

```csharp
var key = "service-health:api-gateway";

store.RecordNotification(key, NotificationPriority.Normal, 900);
// -> (false, false): first occurrence, sent

store.RecordNotification(key, NotificationPriority.Normal, 900);
// -> (true, false): duplicate inside window, suppressed

store.RecordNotification(key, NotificationPriority.Critical, 900);
// -> (false, true): severity rose, sent as an escalation

store.RecordNotification(key, NotificationPriority.Critical, 900);
// -> (true, false): stored severity is now Critical, so this is a duplicate
```

### Per-channel stores through the factory

When different notification channels should not affect each other's suppression state, create one store per channel:

```csharp
using CaddyVpsToolkit.Notifications;

INotificationStateStoreFactory factory = new InMemoryNotificationStateStoreFactory();

using var emailStore = factory.Create();
using var telegramStore = factory.Create();

// The same key is tracked independently in each store.
emailStore.RecordNotification("cert-expiry:example.com", NotificationPriority.High, 3600);
var (suppressed, _) = telegramStore.RecordNotification("cert-expiry:example.com", NotificationPriority.High, 3600);
// suppressed is false: telegramStore has not seen this key before
```

### Resetting state in tests

```csharp
store.Clear();

// The next call for any key behaves as a first occurrence.
var (suppress, escalate) = store.RecordNotification("test-key", NotificationPriority.Low, 60);
// -> (false, false)
```

## Notes

- Suppression keys are compared with the default string comparer, so they are case-sensitive. Normalize keys in the caller if `Api` and `api` should count as the same notification.
- The in-memory implementation never evicts entries. Each distinct key stays in memory until `Clear` or `Dispose` is called. Callers that generate unbounded keys should reuse a bounded set of keys or clear the store periodically.
- `RecordNotification` mutates state even when the caller then fails to send the notification. If delivery matters, record only after a successful send, or accept that a failed send will be suppressed as a duplicate on the next attempt.
- As of this writing, no code outside `Notifications/INotificationStateStore.cs` references these types. The contract is available for the notification pipeline to adopt, and no dependency-injection registration is provided in this file.
