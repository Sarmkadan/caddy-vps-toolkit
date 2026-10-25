# ObserverPattern

`Utilities/ObserverPattern.cs` provides two in-memory notification mechanisms in the `CaddyVpsToolkit.Utilities` namespace:

- **Observer pattern** (`IObserver<T>`, `IObservable<T>`, `Observable<T>`): an observable holds a state value and pushes it to attached `IObserver<T>` instances when `NotifyObservers` or `SetState` is called.
- **Subject** (`Subject<T>`): a lightweight publish/subscribe channel where plain `Action<T>` delegates subscribe and receive published values. `Subject<T>` does not implement `IObservable<T>`.

Both mechanisms are synchronous, process-local, and have no persistence. They are not a replacement for the domain event bus; see [EventBus](EventBus.md) for asynchronous domain events.

## Core Types

### `IObserver<T>`

```csharp
public interface IObserver<T>
{
    void Update(T subject);
}
```

Implement this interface to receive state changes. `Update` is called with the observable's current state.

### `IObservable<T>`

```csharp
public interface IObservable<T>
{
    void Attach(IObserver<T> observer);
    void Detach(IObserver<T> observer);
    void NotifyObservers();
}
```

Contract for anything that can be observed. `Observable<T>` is the only implementation in the file.

## API

### `Observable<T>`

```csharp
public sealed class Observable<T> : IObservable<T> where T : class
```

Generic observable with a single state value. `T` must be a reference type.

#### `Observable(T initialState)`

Creates an observable with `initialState` as its state. `initialState` is not validated, so `null` is accepted.

#### `void Attach(IObserver<T> observer)`

Adds `observer` to the notification list.

- **Parameters**
  - `observer`: Observer to add.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentNullException` if `observer` is `null`.

Attaching an observer that is already attached is a no-op. The observer is not added twice.

#### `void Detach(IObserver<T> observer)`

Removes `observer` from the notification list.

- **Parameters**
  - `observer`: Observer to remove.
- **Return value**: None.
- **Exceptions**: None. A `null` observer or an observer that is not attached is ignored.

#### `void NotifyObservers()`

Calls `Update(state)` on every attached observer, passing the current state.

- **Return value**: None.
- **Exceptions**: Exceptions thrown by an observer's `Update` are not caught. They propagate to the caller, and observers after the failing one are not notified during that call.

Observers are called in attach order from a snapshot of the list taken before the first call. See [Behaviour Notes](#behaviour-notes).

#### `T GetState()`

Returns the current state.

- **Return value**: The state value, which may be `null` if `null` was passed to the constructor or to `SetState`.

#### `void SetState(T state)`

Replaces the state and then calls `NotifyObservers`.

- **Parameters**
  - `state`: New state value.
- **Return value**: None.
- **Exceptions**: Exceptions thrown by an observer propagate, as described for `NotifyObservers`.

The state is replaced under the internal lock. Notification runs after the lock is released.

#### `int GetObserverCount()`

Returns the number of attached observers.

- **Return value**: The count of observers currently attached.

#### `IDisposable AttachWithToken(IObserver<T> observer)`

Attaches `observer` and returns a token. Disposing the token detaches the observer.

- **Parameters**
  - `observer`: Observer to attach.
- **Return value**: An `IDisposable` token. Calling `Dispose` more than once is safe.
- **Exceptions**: Throws `ArgumentNullException` if `observer` is `null`.

The token calls `Detach` on dispose. This happens even if the observer was attached earlier with `Attach`, so prefer this method for scoped subscriptions where the caller owns the attachment.

### `Subject<T>`

```csharp
public sealed class Subject<T>
```

Delegate-based publish/subscribe channel. `T` has no constraints, so value types are allowed.

#### `void Subscribe(Action<T> handler)`

Adds `handler` to the subscriber list.

- **Parameters**
  - `handler`: Delegate to invoke on each publish.
- **Return value**: None.
- **Exceptions**: None. A `null` handler is ignored.

Subscribing the same delegate more than once adds it more than once. It is invoked once per entry on each publish.

#### `void Unsubscribe(Action<T> handler)`

Removes one entry for `handler`.

- **Parameters**
  - `handler`: Delegate to remove. Pass the same delegate instance used for `Subscribe`.
- **Return value**: None.
- **Exceptions**: None. A `null` handler or a handler that is not subscribed is ignored.

Only the first matching entry is removed. Delegates are compared with `Delegate` equality, which matches the target and method. Two separately created lambdas are generally not equal, so keep a reference to the delegate you subscribed.

#### `void Publish(T value)`

Invokes every subscriber with `value`.

- **Parameters**
  - `value`: Value to deliver.
- **Return value**: None.
- **Exceptions**: None. Exceptions thrown by a subscriber are swallowed, and the remaining subscribers are still invoked.

#### `int GetSubscriberCount()`

Returns the number of subscriber entries.

- **Return value**: The count of subscribed handlers, including duplicate entries.

#### `IDisposable SubscribeWithToken(Action<T> handler)`

Subscribes `handler` and returns a token. Disposing the token removes that entry.

- **Parameters**
  - `handler`: Delegate to subscribe.
- **Return value**: An `IDisposable` token. Calling `Dispose` more than once is safe.
- **Exceptions**: None.

## Usage

### Example 1: Observable state with `IObserver<T>`

```csharp
using CaddyVpsToolkit.Utilities;

public sealed record ServiceStatus(string Name, bool Healthy);

public sealed class StatusLogger : IObserver<ServiceStatus>
{
    public void Update(ServiceStatus subject)
    {
        Console.WriteLine($"{subject.Name}: {(subject.Healthy ? "healthy" : "down")}");
    }
}

var status = new Observable<ServiceStatus>(new ServiceStatus("blog", true));
var logger = new StatusLogger();

status.Attach(logger);
status.SetState(new ServiceStatus("blog", false));  // logger.Update is called
status.Detach(logger);
```

### Example 2: Scoped attachment with `AttachWithToken`

```csharp
using CaddyVpsToolkit.Utilities;

var status = new Observable<ServiceStatus>(new ServiceStatus("api", true));

using (status.AttachWithToken(new StatusLogger()))
{
    status.SetState(new ServiceStatus("api", false));  // observer receives this update
}

status.SetState(new ServiceStatus("api", true));       // observer was detached on dispose
Console.WriteLine(status.GetObserverCount());          // 0
```

### Example 3: Delegate subscriptions with `Subject<T>`

```csharp
using CaddyVpsToolkit.Utilities;

var alerts = new Subject<string>();

Action<string> printer = message => Console.WriteLine(message);

alerts.Subscribe(printer);
alerts.Publish("Certificate expires in 7 days");

alerts.Unsubscribe(printer);  // must be the same delegate instance
alerts.Publish("This message is not printed");
```

### Example 4: Scoped subscription with `SubscribeWithToken`

```csharp
using CaddyVpsToolkit.Utilities;

var backups = new Subject<string>();

using (backups.SubscribeWithToken(msg => Console.WriteLine($"[audit] {msg}")))
{
    backups.Publish("Backup completed");  // printed
}

backups.Publish("Backup completed again");  // no subscribers
```

## Behaviour Notes

- **Snapshot notification:** `NotifyObservers` and `Publish` iterate a copy of the list. Attaching, detaching, subscribing, or unsubscribing from inside a callback is safe. The change takes effect on the next notification, not the current one.
- **Synchronous callbacks:** Observers and subscribers run on the thread that calls `SetState`, `NotifyObservers`, or `Publish`. No lock is held during callbacks, so a callback may call back into the same observable.
- **Error handling differs:** `Observable<T>` lets observer exceptions propagate and stops the notification loop. `Subject<T>` swallows subscriber exceptions and continues. Put error handling inside `Update` or the subscriber when a failure must be visible.
- **Duplicate registration:** `Observable<T>` ignores a duplicate `Attach`. `Subject<T>` keeps every `Subscribe` call as a separate entry, and each `Unsubscribe` removes one entry.
- **Concurrent state changes:** `NotifyObservers` reads the state field without holding the lock. Under concurrent `SetState` calls, observers receive whichever state is current when they are notified, which may be newer than the value that triggered the notification.
- **Null arguments:** `Observable<T>.Attach` and `AttachWithToken` throw `ArgumentNullException` for `null`. All other null handling in this file is silent: `Detach`, `Unsubscribe`, and `Subscribe` ignore `null`.
- **Thread safety:** Registration, removal, counts, and state access lock on a private object, so they can be called from multiple threads. Callbacks themselves are not synchronized with each other.
