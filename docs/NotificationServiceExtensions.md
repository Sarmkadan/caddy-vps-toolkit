# NotificationServiceExtensions

The `NotificationServiceExtensions` class provides extension methods for wiring the notification subsystem into the `IServiceCollection` dependency injection container, and for validating notification destinations (email addresses, webhook URLs, phone numbers, and message content) before they are sent. It is defined in the `CaddyVpsToolkit.Notifications` namespace and is a static class.

## API

### AddNotificationService (with options)
Registers `NotificationService`, the `NotificationSuppressionOptions` instance, and an `ICircuitBreakerFactory` as singletons in the service collection. The optional `configureOptions` delegate is invoked immediately against a new `NotificationSuppressionOptions` instance, so the configured values are captured at registration time.
*   **Parameters**: `this IServiceCollection services`, `Action<NotificationSuppressionOptions>? configureOptions = null`
*   **Returns**: The same `IServiceCollection`, to allow method chaining.
*   **Throws**: `ArgumentNullException` if `services` is `null`.

### AddNotificationService (defaults)
Convenience overload that registers the notification services using `NotificationSuppressionOptions` default values. It delegates to the overload above with `configureOptions: null`.
*   **Parameters**: `this IServiceCollection services`
*   **Returns**: The same `IServiceCollection`, to allow method chaining.
*   **Throws**: `ArgumentNullException` if `services` is `null`.

### ValidateEmail
Validates the format of an email address.
*   **Parameters**: `this string email`
*   **Returns**: The validated email address, unchanged.
*   **Throws**: `ArgumentException` if the value is null, empty, or not a valid email format.

### ValidateWebhookUrl
Validates a webhook URL's format and security constraints. The URL must use `http` or `https`, must contain a hostname, and must not point to localhost or a private address.
*   **Parameters**: `this string webhookUrl`
*   **Returns**: The validated webhook URL, unchanged.
*   **Throws**: `ArgumentException` if the URL is null, empty, malformed, uses another scheme, or targets a blocked address.

### ValidatePhoneNumber
Validates a phone number format. Accepted formats include international notation such as `+1234567890` and forms such as `123-456-7890`.
*   **Parameters**: `this string phoneNumber`
*   **Returns**: The validated phone number, unchanged.
*   **Throws**: `ArgumentException` if the value is null, empty, whitespace, or does not match an accepted format.

### ValidateMessageContent
Checks notification message content for template injection patterns before it is rendered or delivered.
*   **Parameters**: `this string message`
*   **Returns**: The validated message content, unchanged.
*   **Throws**: `ArgumentException` if the message is null or empty, or contains a template injection pattern.

### ValidateDestination
Validates a destination string by dispatching to the appropriate validator for the given `DestinationType` (`Email`, `Webhook`, or `Phone`).
*   **Parameters**: `this string destination`, `DestinationType destinationType`
*   **Returns**: The validated destination string, unchanged.
*   **Throws**: `ArgumentException` if the destination is invalid for the specified type.

## Usage

### Example 1: Registering the Notification Services with Custom Options
This example registers the notification services and overrides some of the suppression and circuit breaker defaults. The `ILogger` dependency must also be registered, because `NotificationService` and the circuit breaker factory resolve it from the container.

```csharp
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Notifications;
using CaddyVpsToolkit.Middleware;

public static class NotificationBootstrap
{
    public static IServiceCollection ConfigureNotifications(IServiceCollection services, ILogger logger)
    {
        services.AddSingleton(logger);

        services.AddNotificationService(options =>
        {
            options.Enabled = true;
            options.SuppressionWindowSeconds = 300;   // 5 minutes
            options.MaxTrackedNotifications = 1000;
            options.CircuitBreakerFailureThreshold = 5;
            options.CircuitBreakerRecoveryTimeoutSeconds = 60;
        });

        return services;
    }
}
```

### Example 2: Registering the Notification Services with Defaults
When the built-in defaults are acceptable, call the parameterless overload. It can be chained with other registrations.

```csharp
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;

var services = new ServiceCollection();

services
    .AddNotificationService()
    .AddSingleton<ILogger>(new MemoryLogger(LogLevel.Debug));
```

### Example 3: Validating a Destination Before Sending
The validation methods throw `ArgumentException` on invalid input, so callers can use them as guards before queuing a notification.

```csharp
using System;
using CaddyVpsToolkit.Notifications;

public static class NotificationRequestFactory
{
    public static (string Destination, string Message) Create(
        string destination,
        DestinationType destinationType,
        string message)
    {
        try
        {
            var validDestination = destination.ValidateDestination(destinationType);
            var validMessage = message.ValidateMessageContent();

            return (validDestination, validMessage);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Rejected notification request: {ex.Message}");
            throw;
        }
    }
}
```

### Example 4: Validating Individual Destination Types
Each destination type also has a dedicated validator. Use these when the destination type is already known.

```csharp
using CaddyVpsToolkit.Notifications;

string email = "ops@example.com".ValidateEmail();
string webhook = "https://hooks.example.com/notify".ValidateWebhookUrl();
string phone = "+1234567890".ValidatePhoneNumber();
```

## Notes

*   `AddNotificationService` only registers services; it does not register an `ILogger`. Make sure an `ILogger` implementation is available in the container before resolving `NotificationService`.
*   Because the `configureOptions` delegate runs during registration, changes to the options object after the call have no effect.
