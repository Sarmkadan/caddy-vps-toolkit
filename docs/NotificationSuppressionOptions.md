# NotificationSuppressionOptions

`NotificationSuppressionOptions` is a plain configuration object for the notification pipeline. It controls three things: duplicate suppression (how long an identical notification is held back), delivery resilience (retry, circuit breaking), and the in-memory dead-letter queue for failed deliveries. It is passed to `NotificationService` directly, or configured through the `AddNotificationService` registration helper.

Every property is a public get/set auto-property with a default value, so an instance created with `new` is already usable. The class has no validation and no methods other than the `ToString` override.

## API

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `Enabled` | `bool` | Turns duplicate suppression on or off. Defaults to `true`. | — | The configured value. | None. |
| `SuppressionWindowSeconds` | `int` | Window in which a repeated notification with the same key is suppressed. Defaults to `300` (5 minutes). | — | The configured value. | None. |
| `MaxTrackedNotifications` | `int` | Maximum number of recent notifications kept for suppression checks. Defaults to `1000`. | — | The configured value. | None. |
| `RetryEnabled` | `bool` | Enables retries of failed deliveries. Defaults to `true`. | — | The configured value. | None. |
| `MaxRetryAttempts` | `int` | Maximum retries for one delivery. Defaults to `3`. | — | The configured value. | None. |
| `InitialRetryDelayMs` | `int` | Delay before the first retry, in milliseconds. Defaults to `100`. | — | The configured value. | None. |
| `RetryBackoffMultiplier` | `double` | Factor applied between retries. Defaults to `2.0`. | — | The configured value. | None. |
| `MaxRetryDelayMs` | `int` | Upper bound on any single retry delay, in milliseconds. Defaults to `10000`. | — | The configured value. | None. |
| `CircuitBreakerEnabled` | `bool` | Enables per-provider circuit breaking. Defaults to `true`. | — | The configured value. | None. |
| `CircuitBreakerFailureThreshold` | `int` | Consecutive failures that open the circuit. Defaults to `5`. | — | The configured value. | None. |
| `CircuitBreakerRecoveryTimeoutSeconds` | `int` | Time an open circuit waits before trying recovery, in seconds. Defaults to `60`. | — | The configured value. | None. |
| `DeadLetterEnabled` | `bool` | Stores failed notifications in the dead-letter queue. Defaults to `true`. | — | The configured value. | None. |
| `MaxDeadLetterEntries` | `int` | Maximum dead-letter entries kept in memory. Older entries are removed first. Defaults to `100`. | — | The configured value. | None. |
| `EscalationDetectionEnabled` | `bool` | Intended to always send a higher-severity notification even inside the suppression window. Defaults to `true`. See the notes: the service does not read it. | — | The configured value. | None. |
| `ToString` | `string` | Returns a short summary of `Enabled`, `SuppressionWindowSeconds`, and `MaxTrackedNotifications`. | — | The summary string. | None. |

The class is `sealed` and lives in the `CaddyVpsToolkit.Notifications` namespace. It has only the implicit parameterless constructor.

## Usage

### Example 1: Using the defaults

Creating the object without setting anything gives the defaults listed above.

```csharp
using CaddyVpsToolkit.Notifications;

var options = new NotificationSuppressionOptions();

Console.WriteLine(options.Enabled);                 // True
Console.WriteLine(options.SuppressionWindowSeconds); // 300
Console.WriteLine(options.MaxTrackedNotifications);  // 1000
```

### Example 2: Customizing suppression with an object initializer

This example shortens the suppression window to one minute and keeps the other defaults.

```csharp
using CaddyVpsToolkit.Notifications;

var options = new NotificationSuppressionOptions
{
    SuppressionWindowSeconds = 60,
    MaxTrackedNotifications = 500
};

Console.WriteLine(options); // NotificationSuppressionOptions { Enabled = True, SuppressionWindowSeconds = 60, MaxTrackedNotifications = 500 }
```

### Example 3: Configuring options during service registration

`AddNotificationService` creates one `NotificationSuppressionOptions` instance, passes it to your delegate, and registers that instance as a singleton. The values are fixed at registration time, so later changes to the delegate's captured variables have no effect.

```csharp
using CaddyVpsToolkit.Notifications;

services.AddNotificationService(options =>
{
    options.Enabled = true;
    options.SuppressionWindowSeconds = 300; // 5 minutes
    options.MaxTrackedNotifications = 1000;
    options.CircuitBreakerFailureThreshold = 3;
    options.CircuitBreakerRecoveryTimeoutSeconds = 120;
});
```

To use the defaults without a delegate, call `services.AddNotificationService()` with no arguments.

### Example 4: Passing options directly to NotificationService

When you construct `NotificationService` yourself, pass the options instance as the second argument. A `null` value makes the service create a default instance.

```csharp
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;

var options = new NotificationSuppressionOptions
{
    SuppressionWindowSeconds = 60,
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
    circuitBreakerFactory: circuitBreakers);
```

`logger` stands for any `ILogger` instance you have. The retry settings in this object are not used by this constructor. Retries are controlled by the `IRetryPolicy` you pass in, as described in the notes below.

### Example 5: Turning suppression and dead-lettering off

Disabling suppression lets every notification through, and disabling the dead-letter queue stops failed deliveries from being stored.

```csharp
using CaddyVpsToolkit.Notifications;

var options = new NotificationSuppressionOptions
{
    Enabled = false,
    DeadLetterEnabled = false
};
```

## Notes

- **Threshold values are not validated.** Nothing checks that values are positive or consistent. For example, `MaxTrackedNotifications = 0` or a negative `SuppressionWindowSeconds` is accepted silently, and its effect depends on how `NotificationService` uses it.
- **`EscalationDetectionEnabled` has no effect.** The property exists, but `NotificationService` does not read it. A higher-priority notification with the same title and message is still suppressed as a duplicate. The same gap is described in [NotificationService](NotificationService.md).
- **Retry values are read only through the retry policy.** `MaxRetryAttempts`, `InitialRetryDelayMs`, `RetryBackoffMultiplier`, and `MaxRetryDelayMs` are not read by `NotificationService` or `AddNotificationService`. Retry behavior comes from the `IRetryPolicy` passed to the service, which defaults to a policy that never retries. `RetryEnabled` is read by the service, but its effect still depends on that policy.
- **Circuit breaker settings need a factory.** `CircuitBreakerEnabled` only has an effect when a real `ICircuitBreakerFactory` is supplied. `AddNotificationService` supplies one, using `CircuitBreakerFailureThreshold` and `CircuitBreakerRecoveryTimeoutSeconds`. Without a factory the service uses a no-op factory.
- **The service keeps a reference, not a copy.** `NotificationService` stores the instance it receives and reads its properties on each call, so a change made after construction is seen by the service. Do not change the values while notifications are being sent (see Thread safety below).
- **Thread safety.** The class is a mutable object with public setters. Set all values before passing the instance to a service, and do not change them while notifications are being sent.
- **`ToString` is a summary, not a full dump.** It includes only `Enabled`, `SuppressionWindowSeconds`, and `MaxTrackedNotifications`. Log the other properties individually if you need them in output.
