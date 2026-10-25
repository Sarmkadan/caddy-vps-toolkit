# WebhookHandler

The `WebhookHandler` class lets external systems subscribe to important events raised by `caddy-vps-toolkit`, such as service creation or a failed health check. Subscribers register a URL for one `WebhookEventType`. When that event is triggered, the handler sends an HTTP POST to every registered URL. The types live in the `CaddyVpsToolkit.Integration` namespace, in `Integration/WebhookHandler.cs`.

## API

### `public enum WebhookEventType`
The events that can be triggered by the system:

* `ServiceCreated`: a managed service was created.
* `ServiceDeleted`: a managed service was deleted.
* `ServiceStatusChanged`: the status of a managed service changed.
* `HealthCheckFailed`: a service health check failed.
* `HealthCheckRecovered`: a service health check recovered after failing.
* `ConfigurationUpdated`: the application configuration was updated.

### `public interface IWebhookHandler`
The contract for webhook registration and delivery. The event handlers in `Events/ServiceEventHandlers.cs` depend on this interface, so inject it rather than the concrete class.

```csharp
void Register(string url, WebhookEventType eventType);
void Unregister(string url, WebhookEventType eventType);
Task<bool> TriggerAsync(WebhookEventType eventType, object payload);
```

### `public sealed class WebhookHandler : IWebhookHandler`
The default implementation. It keeps registrations in memory and delivers payloads with an `IHttpClient` (see `Integration/HttpClientWrapper.cs`).

```csharp
public WebhookHandler(IHttpClient httpClient)
```

Passing `null` throws `ArgumentNullException`.

### `Register`
```csharp
void Register(string url, WebhookEventType eventType)
```
Adds `url` to the list of URLs notified for `eventType`.

* **Parameters**:
  * `url`: The endpoint that receives the POST request.
  * `eventType`: The event to subscribe to.
* **Behavior**: Registering the same URL for the same event twice has no effect. The list is kept in insertion order.
* **Exceptions**:
  * `ArgumentException`: `url` is `null` or empty.

### `Unregister`
```csharp
void Unregister(string url, WebhookEventType eventType)
```
Removes `url` from the list for `eventType`.

* **Parameters**:
  * `url`: The endpoint to remove.
  * `eventType`: The event to unsubscribe from.
* **Behavior**: Does nothing if the URL was not registered for that event.
* **Exceptions**:
  * `ArgumentException`: `url` is `null` or empty.

### `TriggerAsync`
```csharp
Task<bool> TriggerAsync(WebhookEventType eventType, object payload)
```
Sends a POST request to every URL registered for `eventType`.

* **Parameters**:
  * `eventType`: The event that occurred.
  * `payload`: Any object describing the event. It is serialized as-is inside the request body.
* **Return Value**: `true` if every delivery succeeded, or if no URL is registered for the event. `false` if at least one delivery failed.
* **Request Body**: Each POST body is an anonymous object with three properties:
  * `EventType`: The name of the event, for example `ServiceCreated`.
  * `Timestamp`: The UTC time when `TriggerAsync` ran.
  * `Payload`: The `payload` argument.
* **Behavior**: URLs are notified one at a time, in registration order. A delivery counts as failed when the response is not a success status code, or when the request throws. A failure for one URL does not stop delivery to the others. The method does not throw for delivery errors.

### `GetRegistrations`
```csharp
public List<string> GetRegistrations(WebhookEventType eventType)
```
Returns a copy of the URLs registered for `eventType`. The list is empty if none are registered. This method is on the `WebhookHandler` class only, not on `IWebhookHandler`. Changes to the returned list do not affect the registrations.

### `public sealed class WebhookPayload`
A standard payload model with snake_case JSON names. Use it to build or read the `payload` value of a webhook, or to deserialize a payload on the receiving side.

| Property | JSON name | Type | Description |
|----------|-----------|------|-------------|
| `EventType` | `event_type` | `string` | The event name. |
| `Timestamp` | `timestamp` | `DateTime` | The event time. The constructor sets it to `DateTime.UtcNow`. |
| `Data` | `data` | `Dictionary<string, object>` | Event-specific fields. The constructor creates an empty dictionary. |

`WebhookHandler` does not use `WebhookPayload` itself. It wraps whatever object it receives in its own envelope, and that envelope uses PascalCase names with no JSON name attributes. If your receiver expects `WebhookPayload`'s snake_case format, build the payload yourself or adapt the receiver.

## Usage

### Example 1: Subscribing an External System
This example registers a receiver for service creation and health failure events. It uses an `IWebhookHandler` obtained from the DI container.

```csharp
using System;
using CaddyVpsToolkit.Integration;

public class AlertSubscriptions
{
    private readonly IWebhookHandler _webhooks;

    public AlertSubscriptions(IWebhookHandler webhooks)
    {
        _webhooks = webhooks;
    }

    public void Subscribe(string receiverUrl)
    {
        _webhooks.Register(receiverUrl, WebhookEventType.ServiceCreated);
        _webhooks.Register(receiverUrl, WebhookEventType.HealthCheckFailed);

        Console.WriteLine($"Subscribed {receiverUrl} to service and health events.");
    }
}
```

### Example 2: Triggering an Event and Checking the Result
This example sends a `HealthCheckFailed` event with a small payload object. It logs a warning when at least one delivery fails.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Integration;

public class HealthFailureNotifier
{
    private readonly IWebhookHandler _webhooks;

    public HealthFailureNotifier(IWebhookHandler webhooks)
    {
        _webhooks = webhooks;
    }

    public async Task NotifyAsync(string serviceName, string errorMessage)
    {
        bool allDelivered = await _webhooks.TriggerAsync(
            WebhookEventType.HealthCheckFailed,
            new
            {
                ServiceName = serviceName,
                ErrorMessage = errorMessage
            });

        if (!allDelivered)
        {
            Console.Error.WriteLine($"Some webhooks failed for {serviceName}.");
        }
    }
}
```

### Example 3: Creating the Handler Directly and Unsubscribing
This example creates a `WebhookHandler` by hand, for example in a test or a small tool. It uses `HttpClientWrapper` as the `IHttpClient`. It then removes a subscription.

```csharp
using System;
using CaddyVpsToolkit.Integration;

public class ManualWebhookSetup
{
    public void Run()
    {
        var handler = new WebhookHandler(new HttpClientWrapper());
        const string url = "https://hooks.example.com/caddy";

        handler.Register(url, WebhookEventType.ConfigurationUpdated);
        handler.Register(url, WebhookEventType.ConfigurationUpdated); // Ignored: already registered.

        foreach (var registered in handler.GetRegistrations(WebhookEventType.ConfigurationUpdated))
        {
            Console.WriteLine($"Registered: {registered}");
        }

        handler.Unregister(url, WebhookEventType.ConfigurationUpdated);
    }
}
```

## Notes
* **Delivery is best effort.** There are no retries, no queue, and no persistence. Registrations are lost when the process restarts, so register subscribers again at startup.
* **Thread safety.** The registration dictionary is a plain `Dictionary`. Calls to `Register` and `Unregister` should not run at the same time as `TriggerAsync` on different threads.
* **Timeouts.** Delivery time depends on the `IHttpClient` implementation. `HttpClientWrapper` applies its own default timeout.
