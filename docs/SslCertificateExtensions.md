# SslCertificateExtensions

`SslCertificateExtensions` provides two kinds of helpers for SSL certificate monitoring in the `CaddyVpsToolkit.Extensions` namespace. The `AddSslCertificateMonitoring` overloads register monitoring into an `IServiceCollection`. The `GetCertificateStatus`, `RequiresRenewal`, and `GetStatusMessage` methods evaluate an `SslCertificateInfo` against warning and critical thresholds.

The registration methods extend `IServiceCollection`. The status methods extend `SslCertificateInfo`, so they are called on an instance, for example `certificate.GetCertificateStatus()`.

For the monitoring service that the registration methods wire up, see [ISslCertificateMonitoringService](ISslCertificateMonitoringService.md) and [SslCertificateMonitoringService](SslCertificateMonitoringService.md). For the certificate model, see [SslCertificateInfo](SslCertificateInfo.md).

## API

### `AddSslCertificateMonitoring` (service registration)
```csharp
public static IServiceCollection AddSslCertificateMonitoring(this IServiceCollection services)
```

Registers `SslCertificateMonitoringService` as the implementation of `ISslCertificateMonitoringService`, with a singleton lifetime.

**Parameters:**
- `services` (`IServiceCollection`): The service collection to add registrations to.

**Return value:**
- Returns the same `IServiceCollection` so calls can be chained.

**Exceptions:**
- Throws `ArgumentNullException` if `services` is `null`.

The registration does not resolve the service. `SslCertificateMonitoringService` needs an `ILogger` and a `NotificationService`, so those must be registered separately. Otherwise the container fails when the monitoring service is first requested.

---

### `AddSslCertificateMonitoring` (per-service registration)
```csharp
public static IServiceCollection AddSslCertificateMonitoring(
    this IServiceCollection services,
    ManagedService service)
```

Registers a `ManagedService` instance as a singleton so the monitoring service can check its certificate during periodic checks. Services whose `HostBinding` is empty, whitespace, or `localhost` (case-insensitive) are skipped. Nothing is registered for them.

**Parameters:**
- `services` (`IServiceCollection`): The service collection to configure.
- `service` (`ManagedService`): The managed service to enable SSL monitoring for.

**Return value:**
- Returns the same `IServiceCollection` so calls can be chained. This is also returned when the service is skipped.

**Exceptions:**
- Throws `ArgumentNullException` if `services` or `service` is `null`.
- Throws `ValidationException` if `service` fails `Validate()`. Validation runs only after the `HostBinding` check, so a skipped service is not validated.

---

### `GetCertificateStatus`
```csharp
public static SslCertificateStatus GetCertificateStatus(
    this SslCertificateInfo certificate,
    int warnDays = 30,
    int criticalDays = 7)
```

Classifies a certificate by how many days remain before it expires.

**Parameters:**
- `certificate` (`SslCertificateInfo`): The certificate to check.
- `warnDays` (`int`, optional): The warning threshold in days. Defaults to `30`.
- `criticalDays` (`int`, optional): The critical threshold in days. Defaults to `7`.

**Return value:**
- `Expired` if `ExpiresAt` is in the past, or if `DaysUntilExpiry` is `0`.
- `Critical` if `DaysUntilExpiry` is at most `criticalDays`.
- `ExpiringSoon` if `DaysUntilExpiry` is at most `warnDays`.
- `Valid` otherwise.

**Exceptions:**
- Throws `ArgumentNullException` if `certificate` is `null`.

---

### `RequiresRenewal`
```csharp
public static bool RequiresRenewal(
    this SslCertificateInfo certificate,
    int warnDays = 30,
    int criticalDays = 7)
```

Reports whether a certificate needs renewal now. It is `true` when `GetCertificateStatus` returns `Critical` or `Expired`.

**Parameters:**
- `certificate` (`SslCertificateInfo`): The certificate to check.
- `warnDays` (`int`, optional): The warning threshold in days. Defaults to `30`.
- `criticalDays` (`int`, optional): The critical threshold in days. Defaults to `7`.

**Return value:**
- Returns `true` if renewal is required, otherwise `false`. `ExpiringSoon` and `Valid` return `false`.

**Exceptions:**
- Throws `ArgumentNullException` if `certificate` is `null`.

---

### `GetStatusMessage`
```csharp
public static string GetStatusMessage(
    this SslCertificateInfo certificate,
    int warnDays = 30,
    int criticalDays = 7)
```

Builds a human-readable summary of the certificate's status. The status comes from `GetCertificateStatus`.

**Parameters:**
- `certificate` (`SslCertificateInfo`): The certificate to describe.
- `warnDays` (`int`, optional): The warning threshold in days. Defaults to `30`.
- `criticalDays` (`int`, optional): The critical threshold in days. Defaults to `7`.

**Return value:**
- Returns a message that depends on the status:
  - `Valid`: `Certificate valid for {days} day(s).`
  - `ExpiringSoon`: `Certificate expires in {days} day(s) on {yyyy-MM-dd}.`
  - `Critical`: the same as `ExpiringSoon`, with ` - RENEWAL REQUIRED!` appended.
  - `Expired`: `Certificate expired on {yyyy-MM-dd} - IMMEDIATE ACTION REQUIRED!`

**Exceptions:**
- Throws `ArgumentNullException` if `certificate` is `null`.

---

## Usage

### Example 1: Registering the Monitoring Service
```csharp
var services = new ServiceCollection();

// ILogger and NotificationService must also be registered, or the
// monitoring service cannot be resolved.
services.AddSslCertificateMonitoring();
```

### Example 2: Registering a Managed Service
```csharp
// Services bound to localhost are skipped; public bindings are validated and registered
services.AddSslCertificateMonitoring(new ManagedService
{
    Name = "blog",
    ExecutablePath = "/opt/blog/blog",
    WorkingDirectory = "/opt/blog",
    Port = 5000,
    HostBinding = "blog.example.com"
});
```

### Example 3: Checking Status With Default Thresholds
```csharp
var status = certificate.GetCertificateStatus();

if (status == SslCertificateStatus.Valid)
{
    Console.WriteLine("No action needed.");
}
```

### Example 4: Custom Thresholds and Renewal Decisions
```csharp
// Warn 14 days ahead and treat anything within 3 days as critical
if (certificate.RequiresRenewal(warnDays: 14, criticalDays: 3))
{
    Console.WriteLine(certificate.GetStatusMessage(warnDays: 14, criticalDays: 3));
}
```

## Notes

- **Expiry rounding**: `DaysUntilExpiry` truncates to whole days. A certificate with less than 24 hours left has `DaysUntilExpiry` of `0`, so `GetCertificateStatus` returns `Expired` and `RequiresRenewal` returns `true`. The status is `Expired` even though the certificate is still valid.
- **Error status is not produced**: `GetCertificateStatus` never returns `SslCertificateStatus.Error`. The `Error` branch in `GetStatusMessage` only runs for a value passed in from elsewhere, which these methods do not do.
- **Threshold validation**: The methods do not check that `criticalDays` is less than or equal to `warnDays`. If the values are reversed, `Critical` is reported for certificates that would otherwise be `ExpiringSoon`, and the `ExpiringSoon` band is never used.
- **Skipped services**: `AddSslCertificateMonitoring(service)` returns without registering anything for `localhost` or empty host bindings. A caller that expects the service to be monitored should check `HostBinding` first.
- **Repeated registration**: Each call to the per-service overload adds another singleton. Calling it twice with the same `ManagedService` registers two entries.
- **Thread safety**: The status methods only read the certificate and the current UTC time, so they are safe to call from multiple threads on the same instance.
