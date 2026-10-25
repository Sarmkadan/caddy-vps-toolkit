# NotificationService

The `NotificationService` class sends a `Notification` through every registered `INotificationProvider` (console, webhook, email, and so on). It adds duplicate suppression, optional retry with backoff, optional per-provider circuit breaking, and an in-memory dead-letter queue for deliveries that fail. The class, its supporting types, and the `INotificationProvider` contract are all defined in `Notifications/NotificationService.cs` in the `CaddyVpsToolkit.Notifications` namespace.

## API

### `public sealed class NotificationService`
The notification dispatcher. It is constructed from a logger and optional resilience components:

* `ILogger logger`: receives log lines for each provider attempt, suppressed duplicate, and failed delivery. Required.
* `NotificationSuppressionOptions? suppressionOptions`: suppression, retry, circuit breaker, and dead-letter settings. When `null`, default options are used.
* `IRetryPolicy? retryPolicy`: wraps each provider call when `RetryEnabled` is `true`. When `null`, `NoRetryPolicy` is used, so calls are never retried.
* `ICircuitBreakerFactory? circuitBreakerFactory`: creates one circuit breaker per provider when `CircuitBreakerEnabled` is `true`. When `null`, `NoOpCircuitBreakerFactory` is used, so no circuit breaking happens even when the option is enabled.

Passing `null` for `logger` throws `ArgumentNullException`.

```csharp
public NotificationService(
    ILogger logger,
    NotificationSuppressionOptions? suppressionOptions = null,
    IRetryPolicy? retryPolicy = null,
    ICircuitBreakerFactory? circuitBreakerFactory = null)
```

### `Register`
```csharp
void Register(INotificationProvider provider)
```
Adds a provider to the dispatch list. The provider is keyed by its `ProviderName`, so registering a second provider with the same name replaces the first.

* **Parameters**:
  * `provider`: The provider to register.
* **Exceptions**:
  * `ArgumentNullException`: `provider` is `null`.
* **Behavior**: Providers are stored in a plain `Dictionary`, so call `Register` during startup, before the service receives concurrent `SendAsync` calls.

### `SendAsync(Notification)`
```csharp
Task<bool> SendAsync(Notification notification)
```
Sends a notification through all registered providers.

* **Parameters**:
  * `notification`: The notification to send.
* **Return Value**: `true` only when at least one provider is registered and every provider reported success. `false` is returned when no providers are registered or when any provider failed. When the notification is suppressed as a duplicate, the method returns `true` without calling any provider.
* **Exceptions**:
  * `ArgumentNullException`: `notification` is `null`.
* **Behavior**:
  1. The duplicate check runs first. The suppression key is `Title|Message|Priority`. If the same key was seen within `SuppressionWindowSeconds`, an info line is logged and the method returns `true`.
  2. Each provider is called in turn. The call goes through the provider's circuit breaker when enabled, and through the retry policy when `RetryEnabled` is `true`.
  3. An exception thrown by a provider is caught, logged, and counted as a failed delivery. The other providers still run.
  4. Each failed provider is added to the dead-letter queue when `DeadLetterEnabled` is `true`, and a warning is logged.

### `SendAsync(string title, string message, NotificationPriority priority = NotificationPriority.Normal)`
```csharp
Task<bool> SendAsync(string title, string message, NotificationPriority priority = NotificationPriority.Normal)
```
Builds a `Notification` from the given values and calls `SendAsync(Notification)`. This overload does not check `title` or `message` for `null`.

* **Parameters**:
  * `title`: The notification title.
  * `message`: The notification message.
  * `priority`: The notification priority. Defaults to `Normal`.
* **Return Value**: The same value as `SendAsync(Notification)`.

### `GetDeadLetterEntries`
```csharp
List<DeadLetterEntry> GetDeadLetterEntries()
```
Returns a snapshot of the dead-letter queue, oldest first.

* **Return Value**: A new list that is a copy of the queue. Changing the list does not change the queue.

### `ClearDeadLetterQueue`
```csharp
void ClearDeadLetterQueue()
```
Removes all entries from the dead-letter queue.

## Supporting Types

These types live in the same file and are needed to use the service.

### `public sealed class Notification`
The message being sent.

* `string Id`: A new GUID string by default.
* `string Title`: Defaults to an empty string.
* `string Message`: Defaults to an empty string.
* `NotificationPriority Priority`: Defaults to `Normal`.
* `DateTime CreatedAt`: The UTC creation time, set when the object is created.
* `Dictionary<string, string> Metadata`: Free-form key/value pairs. Use the `AddMetadata` and `GetMetadataValue` extensions in `NotificationExtensions` to read and write them.

### `public enum NotificationPriority`
Values, from lowest to highest: `Low` (0), `Normal` (1), `High` (2), `Critical` (3). The priority is part of the duplicate-suppression key. It does not change how providers deliver the notification.

### `public interface INotificationProvider`
The contract for a delivery channel.

```csharp
Task<bool> SendAsync(Notification notification);
string ProviderName { get; }
```

* `SendAsync`: Returns `true` on success. Returning `false`, or throwing an exception, counts as a failed delivery.
* `ProviderName`: The key used by `Register`, and the name shown in logs and dead-letter entries.

### `public sealed class ConsoleNotificationProvider : INotificationProvider`
A provider for development and testing. Its `ProviderName` is `"Console"`. It writes the priority, title, and message to standard output and always returns `true`.

### `public sealed class DeadLetterEntry`
A record of one failed delivery to one provider.

* `NotificationId`: The `Id` of the failed notification.
* `ProviderName`: The provider that failed.
* `Title`, `Message`, `Priority`: Copied from the notification.
* `FailedAt`: The UTC time the failure was recorded.
* `ErrorMessage`: The exception message, or `Delivery failed` when the provider returned `false`.
* `AttemptCount`: Defaults to `1`. It is not updated by the current implementation.

## Usage

### Example 1: Sending to the Console Provider
This example creates a service with default options, registers the console provider, and sends a notification with metadata.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;

public class DiskUsageNotifier
{
    private readonly NotificationService _notifications;

    public DiskUsageNotifier(ILogger logger)
    {
        _notifications = new NotificationService(logger);
        _notifications.Register(new ConsoleNotificationProvider());
    }

    public async Task<bool> ReportAsync(int usedPercent)
    {
        var notification = new Notification
        {
            Title = "Disk usage high",
            Message = $"Root filesystem is at {usedPercent}%.",
            Priority = usedPercent >= 90 ? NotificationPriority.Critical : NotificationPriority.High
        }.AddMetadata("filesystem", "/");

        bool delivered = await _notifications.SendAsync(notification);
        Console.WriteLine($"Delivered to all providers: {delivered}");

        return delivered;
    }
}
```

### Example 2: A Custom Webhook Provider
This example implements `INotificationProvider` for an HTTP webhook. The URL is checked with the `ValidateWebhookUrl` extension before the provider is used.

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CaddyVpsToolkit.Notifications;

public sealed class WebhookNotificationProvider : INotificationProvider
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _url;

    public WebhookNotificationProvider(string webhookUrl)
    {
        _url = webhookUrl.ValidateWebhookUrl();
    }

    public string ProviderName => "Webhook";

    public async Task<bool> SendAsync(Notification notification)
    {
        var payload = new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            Priority = notification.Priority.ToString(),
            notification.CreatedAt
        };

        using var response = await Http.PostAsJsonAsync(_url, payload);
        return response.IsSuccessStatusCode;
    }
}
```

Register it the same way as any other provider:

```csharp
var notifications = new NotificationService(logger);
notifications.Register(new WebhookNotificationProvider(webhookUrl));
```

### Example 3: Configuring Suppression, Retry, and Circuit Breaking
This example sets a shorter suppression window and enables retries and circuit breaking. `CircuitBreakerFactory` needs a logger and the options thresholds, which are the same values that `AddNotificationService` uses.

```csharp
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;
using CaddyVpsToolkit.Utilities;

var options = new NotificationSuppressionOptions
{
    SuppressionWindowSeconds = 60,
    MaxRetryAttempts = 3,
    InitialRetryDelayMs = 200,
    CircuitBreakerFailureThreshold = 5,
    CircuitBreakerRecoveryTimeoutSeconds = 120
};

var circuitBreakers = new CircuitBreakerFactory(
    logger,
    options.CircuitBreakerFailureThreshold,
    options.CircuitBreakerRecoveryTimeoutSeconds);

var notifications = new NotificationService(
    logger,
    options,
    retryPolicy: someRetryPolicy,
    circuitBreakerFactory: circuitBreakers);
```

`someRetryPolicy` stands for any `IRetryPolicy` implementation you have. When you pass `null`, the service uses `NoRetryPolicy`, which never retries.

### Example 4: Dependency Injection
`AddNotificationService` registers the options, the circuit breaker factory, and `NotificationService` as a singleton. The container must also provide an `ILogger`. Providers are not registered by the extension, so register them on the resolved instance.

```csharp
using System;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<ILogger>(new FileLogger("/var/log/caddy-vps-toolkit/notifications.log"));
services.AddNotificationService(options =>
{
    options.SuppressionWindowSeconds = 600;
    options.DeadLetterEnabled = true;
});

using var container = services.BuildServiceProvider();
var notifications = container.GetRequiredService<NotificationService>();
notifications.Register(new ConsoleNotificationProvider());
```

### Example 5: Inspecting and Clearing Dead Letters
This example prints failed deliveries from the queue and then clears it.

```csharp
using System;
using CaddyVpsToolkit.Notifications;

public static class DeadLetterReporter
{
    public static void PrintAndClear(NotificationService notifications)
    {
        foreach (var entry in notifications.GetDeadLetterEntries())
        {
            Console.Error.WriteLine(
                $"{entry.FailedAt:u} [{entry.Priority}] {entry.ProviderName}: {entry.Title} ({entry.ErrorMessage})");
        }

        notifications.ClearDeadLetterQueue();
    }
}
```

## Notes

*   **Return value of `SendAsync`**: The XML summary on `SendAsync` says "True if at least one provider succeeded". The implementation returns `true` only when every provider succeeded (`results.TrueForAll`). If one of two providers fails, the method returns `false`. Code that depends on the return value should rely on the implementation, not the summary.
*   **Failed sends are suppressed too**: The suppression key is recorded before providers are called. If every provider fails, an identical notification sent within `SuppressionWindowSeconds` returns `true` without being delivered again. A caller that retries after a failure must wait out the window or change the title, message, or priority.
*   **Suppression tracking cleanup**: When the tracking map grows beyond `MaxTrackedNotifications`, up to 100 entries are removed in dictionary enumeration order. That order is not guaranteed to be oldest-first, although the code comment says it is.
*   **`EscalationDetectionEnabled` has no effect**: The option exists on `NotificationSuppressionOptions`, but `NotificationService` does not read it. A higher-priority notification with the same title and message is still suppressed as a duplicate.
*   **`AttemptCount` is always `1`**: `DeadLetterEntry.AttemptCount` is never updated, so it does not reflect the number of retries that were made.
*   **Circuit breaker default**: Without a factory argument, the service uses `NoOpCircuitBreakerFactory`. `CircuitBreakerEnabled` only has an effect when a real factory is supplied. `AddNotificationService` supplies one.
*   **Retry default**: `AddNotificationService` does not register an `IRetryPolicy`. Unless the application registers one, the constructor default (`NoRetryPolicy`) applies, so provider calls are not retried.
*   **Thread safety**: `SendAsync` and the dead-letter methods use locks for the suppression map and the queue. `Register` is not locked, so do not call it while sends are running.
*   **Dead-letter limit**: The queue holds at most `MaxDeadLetterEntries` entries. The oldest entry is removed when a new one would exceed the limit. Entries exist only in memory and are lost on restart.
*   **Logging**: Provider results, suppressed duplicates, and failed deliveries are written through the `ILogger` passed to the constructor. Exceptions thrown by the logger are not caught, so they can propagate out of `SendAsync`.
