# IObserver

The `IObserver` interface defines a dual‑role contract that combines the classic Observer pattern with a publish‑subscribe mechanism. Implementations maintain an internal observable state and a list of attached observers, as well as a separate subscriber list for event‑style notifications. The interface provides methods to manage both observer attachments and subscriber registrations, to query and modify the observable state, and to trigger notifications to either group.

## API

### `Observable`
- **Type:** `Observable` (property)
- **Description:** Gets the underlying `Observable` instance that this `IObserver` wraps or represents. The returned object exposes the state and notification logic.
- **Returns:** The `Observable` object associated with this observer.

### `void Attach(IObserver observer)`
- **Description:** Registers an observer to receive state‑change notifications. The attached observer will be notified when `NotifyObservers` is called.
- **Parameters:** `observer` – the `IObserver` instance to attach.
- **Throws:** `ArgumentNullException` if `observer` is `null`. May throw `InvalidOperationException` if the observer is already attached.

### `void Detach(IObserver observer)`
- **Description:** Removes a previously attached observer from the notification list.
- **Parameters:** `observer` – the `IObserver` instance to detach.
- **Throws:** `ArgumentNullException` if `observer` is `null`. May throw `InvalidOperationException` if the observer is not currently attached.

### `void NotifyObservers()`
- **Description:** Triggers a notification to all currently attached observers. Typically called after the internal state has changed via `SetState`.
- **Throws:** Nothing documented; implementations may throw if the observer list is modified during iteration.

### `T GetState()`
- **Description:** Returns the current observable state.
- **Type parameter:** `T` – the type of the state.
- **Returns:** The current state value.
- **Throws:** `InvalidOperationException` if the state has not been initialized.

### `void SetState(T newState)`
- **Description:** Updates the observable state to the specified value. This method may automatically call `NotifyObservers` depending on the implementation.
- **Type parameter:** `T` – the type of the state.
- **Parameters:** `newState` – the new state value.
- **Throws:** Nothing documented; implementations may validate the state.

### `int GetObserverCount()`
- **Description:** Returns the number of observers currently attached via `Attach`.
- **Returns:** The count of attached observers.

### `void Subscribe(Action<object> handler)`
- **Description:** Subscribes a delegate to receive publish‑style notifications. The handler will be invoked when `Publish` is called.
- **Parameters:** `handler` – the delegate to invoke on publication.
- **Throws:** `ArgumentNullException` if `handler` is `null`. May throw `InvalidOperationException` if the handler is already subscribed.

### `void Unsubscribe(Action<object> handler)`
- **Description:** Removes a previously subscribed handler from the subscriber list.
- **Parameters:** `handler` – the delegate to unsubscribe.
- **Throws:** `ArgumentNullException` if `handler` is `null`. May throw `InvalidOperationException` if the handler is not currently subscribed.

### `void Publish(object data)`
- **Description:** Sends a notification to all currently subscribed handlers, passing the provided data.
- **Parameters:** `data` – the data object to deliver to each subscriber.
- **Throws:** Nothing documented; implementations may throw if the subscriber list is modified during iteration.

### `int GetSubscriberCount()`
- **Description:** Returns the number of handlers currently subscribed via `Subscribe`.
- **Returns:** The count of subscribed handlers.

## Usage

### Example 1: Observer pattern for state monitoring

```csharp
public class TemperatureSensor : IObserver
{
    private double currentTemperature;

    public void UpdateTemperature(double temp)
    {
        SetState(temp);
        NotifyObservers();
    }

    // IObserver members
    public Observable Observable => throw new NotImplementedException();
    public void Attach(IObserver observer) => throw new NotImplementedException();
    public void Detach(IObserver observer) => throw new NotImplementedException();
    public void NotifyObservers() => throw new NotImplementedException();
    public T GetState<T>() => (T)(object)currentTemperature;
    public void SetState<T>(T newState) => currentTemperature = (double)(object)newState;
    public int GetObserverCount() => throw new NotImplementedException();
    public void Subscribe(Action<object> handler) => throw new NotImplementedException();
    public void Unsubscribe(Action<object> handler) => throw new NotImplementedException();
    public void Publish(object data) => throw new NotImplementedException();
    public int GetSubscriberCount() => throw new NotImplementedException();
}

// Usage
var sensor = new TemperatureSensor();
sensor.Attach(displayObserver);
sensor.UpdateTemperature(25.3);
```

### Example 2: Publish‑subscribe for event broadcasting

```csharp
public class EventBus : IObserver
{
    private readonly List<Action<object>> subscribers = new();

    public void Broadcast(string eventName, object payload)
    {
        Publish(new { Event = eventName, Data = payload });
    }

    // IObserver members
    public Observable Observable => throw new NotImplementedException();
    public void Attach(IObserver observer) => throw new NotImplementedException();
    public void Detach(IObserver observer) => throw new NotImplementedException();
    public void NotifyObservers() => throw new NotImplementedException();
    public T GetState<T>() => throw new NotImplementedException();
    public void SetState<T>(T newState) => throw new NotImplementedException();
    public int GetObserverCount() => throw new NotImplementedException();
    public void Subscribe(Action<object> handler) => subscribers.Add(handler);
    public void Unsubscribe(Action<object> handler) => subscribers.Remove(handler);
    public void Publish(object data)
    {
        foreach (var handler in subscribers.ToArray())
            handler(data);
    }
    public int GetSubscriberCount() => subscribers.Count;
}

// Usage
var bus = new EventBus();
bus.Subscribe(data => Console.WriteLine(((dynamic)data).Event));
bus.Broadcast("UserLoggedIn", new { UserId = 42 });
```

## Notes

- **Thread safety:** The interface does not mandate thread‑safe implementations. Concurrent calls to `Attach`, `Detach`, `NotifyObservers`, `Subscribe`, `Unsubscribe`, or `Publish` may cause race conditions or corrupted internal lists. Implementations should synchronize access (e.g., with locks) if used from multiple threads.
- **Observer vs. Subscriber:** The two notification channels are independent. Attaching an observer does not subscribe it to publish events, and subscribing a handler does not make it an observer. State changes are propagated only via `NotifyObservers`; `Publish` is a separate mechanism.
- **Null arguments:** All methods that accept an observer or handler throw `ArgumentNullException` when the argument is `null`.
- **Duplicate registration:** Attaching an observer or subscribing a handler that is already registered may throw `InvalidOperationException` or be silently ignored, depending on the implementation.
- **Modification during enumeration:** Calling `Attach`, `Detach`, `Subscribe`, or `Unsubscribe` from within a `NotifyObservers` or `Publish` callback can lead to undefined behavior (e.g., `InvalidOperationException`). Implementations should snapshot the list before iterating or defer modifications.
- **State initialization:** `GetState` may throw if `SetState` has never been called. It is recommended to initialize the state in the constructor or call `SetState` before any observer queries.
