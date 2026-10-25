# SslCertificateMonitoringService

`SslCertificateMonitoringService` connects to remote hosts over TLS on port 443, reads the server certificate, classifies it by how close it is to expiry, and sends renewal alerts through `NotificationService`. It implements `ISslCertificateMonitoringService` and lives in `Services/SslCertificateMonitoringService.cs`.

The service does not validate the certificate chain. Its remote certificate callback always accepts the certificate, so an expired, self-signed, or untrusted certificate still produces a result with its metadata. The `Error` status is reserved for connection and TLS negotiation failures.

The class is `sealed` and holds no per-call state, so one instance can be shared across threads. It is registered as a singleton through `AddSslCertificateMonitoring()` in `Extensions/SslCertificateExtensions.cs`, and `Program.cs` registers it the same way.

## Status classification

Each check produces one of the `SslCertificateStatus` values. Thresholds come from `SslCertificateMonitoringOptions`.

| Status | Condition | Alert priority |
|--------|-----------|----------------|
| `Valid` | More than `WarnDays` days remain | Not alerted |
| `ExpiringSoon` | `DaysUntilExpiry` is at most `WarnDays` and greater than `CriticalDays` | `Normal` |
| `Critical` | `DaysUntilExpiry` is at most `CriticalDays` | `Critical` |
| `Expired` | `ExpiresAt` is earlier than the current UTC time | `Critical` |
| `Error` | The connection or TLS handshake failed | `High` |

The checks run in this order: expired first, then critical, then warning. A certificate that is already expired is always `Expired`, regardless of the thresholds.

## API

### `SslCertificateMonitoringService(ILogger, NotificationService, SslCertificateMonitoringOptions?)`

```csharp
public SslCertificateMonitoringService(
    ILogger logger,
    NotificationService notifications,
    SslCertificateMonitoringOptions? options = null)
```

Creates an instance. The thresholds are read from `options` once, at construction time. Changing the options object afterward has no effect on the instance.

**Parameters**
- `logger` (`ILogger`): Receives debug, info, and warning messages for each check and alert.
- `notifications` (`NotificationService`): Used to dispatch alerts. Providers must be registered on it separately. Without any registered provider, alerts are not delivered anywhere.
- `options` (`SslCertificateMonitoringOptions?`): Warning and critical thresholds. When `null`, the defaults are used: `WarnDays = 30` and `CriticalDays = 7`.

**Exceptions**
- `ArgumentNullException`: Thrown when `logger` or `notifications` is `null`. The parameter name is set accordingly.

### `CheckCertificateAsync(string domain, CancellationToken cancellationToken = default)`

```csharp
public async Task<Result<SslCertificateCheckResult>> CheckCertificateAsync(
    string domain,
    CancellationToken cancellationToken = default)
```

Connects to `domain` on port 443, reads the server certificate, and returns its classification. This method is part of `ISslCertificateMonitoringService`.

Network and TLS failures do not produce a failed `Result`. They produce a successful `Result` whose check result has `Status = Error` and whose `Message` contains the exception message. The method only returns a failed `Result` for input validation.

**Parameters**
- `domain` (`string`): Host name to check, for example `example.com`. The same value is used as the TCP target and as the TLS `TargetHost` for SNI. It must not be `null`, empty, or whitespace.
- `cancellationToken` (`CancellationToken`): Cancels the check. The connection also has an internal 10-second timeout.

**Return value**
- `Task<Result<SslCertificateCheckResult>>`:
  - Failure with error code `INVALID_DOMAIN` when `domain` is `null`, empty, or whitespace.
  - Success with the classified `SslCertificateCheckResult` otherwise. `Certificate` is `null` only when status is `Error`.

**Exceptions**
- `OperationCanceledException`: Propagates when `cancellationToken` is canceled. It also propagates when the 10-second connection timeout expires, because the timeout is an `OperationCanceledException` and is rethrown rather than converted to an `Error` result. See the Notes section.

### `CheckAllServicesAsync(IEnumerable<ManagedService> services, CancellationToken cancellationToken = default)`

```csharp
public async Task<IReadOnlyList<SslCertificateCheckResult>> CheckAllServicesAsync(
    IEnumerable<ManagedService> services,
    CancellationToken cancellationToken = default)
```

Runs `CheckCertificateAsync` for each managed service in sequence and returns the results. This method is part of `ISslCertificateMonitoringService`.

Services whose `HostBinding` is `null`, empty, whitespace, or `localhost` (compared case-insensitively) are skipped. A skipped service has no entry in the returned list.

**Parameters**
- `services` (`IEnumerable<ManagedService>`): Services to check. The enumeration must not be `null`.
- `cancellationToken` (`CancellationToken`): Checked before each service. Cancellation stops the loop and discards the results collected so far.

**Return value**
- `Task<IReadOnlyList<SslCertificateCheckResult>>`: A read-only list with one entry per checked service, in input order. The list is empty when every service is skipped.

**Exceptions**
- `OperationCanceledException`: Propagates from the cancellation check or from a per-host timeout. A single host that times out therefore stops the whole batch. See the Notes section.
- `ArgumentNullException` (or `NullReferenceException`): Thrown when `services` is `null`, because the method enumerates it without a guard.

### `SendRenewalAlertsAsync(IEnumerable<SslCertificateCheckResult> results, CancellationToken cancellationToken = default)`

```csharp
public async Task SendRenewalAlertsAsync(
    IEnumerable<SslCertificateCheckResult> results,
    CancellationToken cancellationToken = default)
```

Sends one notification for each result whose status is not `Valid`. The notification title is `SSL Certificate Alert: {Domain}`, and the body is the result's `Message`. This method is part of `ISslCertificateMonitoringService`.

**Parameters**
- `results` (`IEnumerable<SslCertificateCheckResult>`): Results to evaluate, typically the output of `CheckAllServicesAsync`. The enumeration is materialized before any alert is sent.
- `cancellationToken` (`CancellationToken`): Checked before each alert.

**Return value**
- `Task`: Completes after every alert has been attempted. It completes immediately when no result needs an alert.

**Exceptions**
- `OperationCanceledException`: Propagates from the cancellation check. Alerts already dispatched are not undone.
- `ArgumentNullException`: Thrown when `results` is `null`.

## Usage

### Example 1: Resolving the service through DI

Because the service is registered as a singleton, most consumers receive the interface through their constructor.

```csharp
public sealed class CertificateWatchJob
{
    private readonly ISslCertificateMonitoringService _monitor;

    public CertificateWatchJob(ISslCertificateMonitoringService monitor)
    {
        _monitor = monitor;
    }

    public async Task RunAsync(IEnumerable<ManagedService> services, CancellationToken ct)
    {
        IReadOnlyList<SslCertificateCheckResult> results =
            await _monitor.CheckAllServicesAsync(services, ct);

        await _monitor.SendRenewalAlertsAsync(results, ct);
    }
}
```

### Example 2: Checking a single domain

```csharp
ILogger logger = new FileLogger("/var/log/caddy-vps-toolkit/ssl.log");
var notifications = new NotificationService(logger);
var monitor = new SslCertificateMonitoringService(logger, notifications);

Result<SslCertificateCheckResult> outcome =
    await monitor.CheckCertificateAsync("example.com");

if (!outcome.IsSuccess)
{
    Console.WriteLine($"Invalid request: {outcome.ErrorMessage} ({outcome.ErrorCode})");
    return;
}

SslCertificateCheckResult check = outcome.Data;
Console.WriteLine($"{check.Domain}: {check.Status} - {check.Message}");

if (check.Certificate is not null)
{
    Console.WriteLine($"Issuer:  {check.Certificate.Issuer}");
    Console.WriteLine($"Expires: {check.Certificate.ExpiresAt:u}");
}
```

### Example 3: Custom thresholds and console alerts

```csharp
ILogger logger = new FileLogger("/var/log/caddy-vps-toolkit/ssl.log");

var notifications = new NotificationService(logger);
notifications.Register(new ConsoleNotificationProvider());

var options = new SslCertificateMonitoringOptions
{
    WarnDays = 21,    // ExpiringSoon from 21 days out
    CriticalDays = 3  // Critical from 3 days out
};

var monitor = new SslCertificateMonitoringService(logger, notifications, options);

var services = new List<ManagedService>
{
    new ManagedService { Name = "web", HostBinding = "app.example.com" },
    new ManagedService { Name = "api", HostBinding = "api.example.com" }
};

IReadOnlyList<SslCertificateCheckResult> results =
    await monitor.CheckAllServicesAsync(services);

// Sends an alert for every result that is not Valid.
await monitor.SendRenewalAlertsAsync(results);
```

### Example 4: Reporting without sending alerts

`SendRenewalAlertsAsync` is optional. Results can be inspected directly, for example to print a summary table.

```csharp
IReadOnlyList<SslCertificateCheckResult> results =
    await monitor.CheckAllServicesAsync(services);

foreach (SslCertificateCheckResult result in results.OrderBy(r => r.Status))
{
    int? daysLeft = result.Certificate?.DaysUntilExpiry;
    Console.WriteLine($"{result.Domain,-30} {result.Status,-12} {daysLeft?.ToString() ?? "-"}");
}
```

## Notes

- **Port and protocol.** The service always connects to port 443 and always attempts TLS. Hosts that serve HTTPS on another port, or that only speak plain HTTP, are reported as `Error`.
- **No chain validation.** Because every certificate is accepted, `Status` reflects only the expiry date. A certificate that is valid for the wrong host name, or signed by an untrusted authority, is reported as `Valid` if its dates are fine. Use another mechanism if trust must be verified.
- **Timeout surfaces as an exception.** The 10-second connection timeout is implemented with a linked `CancellationTokenSource`. When it fires, `OperationCanceledException` propagates out of `CheckCertificateAsync` and is not converted to an `Error` result. Callers that need per-host isolation should catch it themselves, or call `CheckCertificateAsync` per host instead of `CheckAllServicesAsync`.
- **Batch behavior.** `CheckAllServicesAsync` checks hosts one after another, so the total time grows with the number of services. A timeout or cancellation on any host ends the batch.
- **Expiry rounding.** `DaysUntilExpiry` is truncated to whole days and is zero once the certificate has expired. A certificate that expires in less than a day is therefore `Critical` with `DaysUntilExpiry = 0`, and an already-expired certificate is `Expired`.
- **Threshold ordering.** Both thresholds are compared with `<=`. Set `CriticalDays` lower than `WarnDays`. If `CriticalDays` is greater than `WarnDays`, every certificate inside the critical window is reported as `Critical`, and nothing is reported as `ExpiringSoon`.
- **Alert scope.** `SendRenewalAlertsAsync` alerts on every non-`Valid` result, including `Error`, and it does not track what it sent before. Repeated identical alerts are suppressed by `NotificationService`, which keys on title, message, and priority and reports a suppressed duplicate as dispatched. Because the message includes the day count, an alert repeats when that count changes.
- **Delivery failures.** When a notification is not delivered, `SendRenewalAlertsAsync` logs a warning and continues with the next result. Failed deliveries are kept in `NotificationService`'s dead-letter queue when its dead-letter option is enabled.
- **Logging.** Every check logs a debug line before the connection and an info line after classification. Connection failures log a warning. Alert dispatch logs one info line with the total count.
