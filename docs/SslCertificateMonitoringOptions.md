# SslCertificateMonitoringOptions

`SslCertificateMonitoringOptions` is a small, mutable configuration object that sets the two expiry thresholds used by `SslCertificateMonitoringService` when it classifies a certificate. Days remaining at or below `WarnDays` produce a warning; days remaining at or below `CriticalDays` produce a critical status. The defaults (30 and 7 days) match the thresholds that were previously hard-coded in the service.

The class is declared in `Services/SslCertificateMonitoringOptions.cs` under the `CaddyVpsToolkit.Services` namespace. It is `sealed` and has no dependencies.

## API

### SslCertificateMonitoringOptions

```csharp
public sealed class SslCertificateMonitoringOptions
```

A plain options class with two settable properties and a `ToString` override. Create it with its parameterless constructor and assign only the properties you need; anything left unset keeps its default.

### WarnDays

```csharp
public int WarnDays { get; set; } = 30;
```

Number of days before expiry at which a certificate is reported with a warning status.

- **Default:** `30`.
- **Type:** `int`.
- **Behaviour:** A certificate whose `DaysUntilExpiry` is less than or equal to this value, and greater than `CriticalDays`, is classified as expiring soon with `isCritical: false`.

### CriticalDays

```csharp
public int CriticalDays { get; set; } = 7;
```

Number of days before expiry at which a certificate is reported with a critical status.

- **Default:** `7`.
- **Type:** `int`.
- **Behaviour:** A certificate whose `DaysUntilExpiry` is less than or equal to this value is classified as expiring soon with `isCritical: true`. This check runs before the `WarnDays` check, so it takes precedence.

### ToString

```csharp
public override string ToString()
```

Returns a diagnostic string that lists both thresholds.

**Returns:**
- `string`: For the defaults, `SslCertificateMonitoringOptions { WarnDays = 30, CriticalDays = 7 }`.

## Usage

Use the defaults by passing no options at all. Both `SslCertificateMonitoringService` and `CertificateMonitor` create a default instance when the `options` argument is `null`.

```csharp
using CaddyVpsToolkit.Services;

// Default thresholds: warn at 30 days, critical at 7 days.
var service = new SslCertificateMonitoringService(logger, notifications);
```

Override one or both thresholds with an object initializer. Values you do not set keep their defaults.

```csharp
using CaddyVpsToolkit.Services;

// Warn two weeks out and go critical in three days.
var options = new SslCertificateMonitoringOptions
{
    WarnDays = 14,
    CriticalDays = 3
};

var service = new SslCertificateMonitoringService(logger, notifications, options);
```

Set only `WarnDays` when you want an earlier warning but the default critical window.

```csharp
using CaddyVpsToolkit.Services;

var options = new SslCertificateMonitoringOptions();
options.WarnDays = 45; // CriticalDays stays at 7.

var service = new SslCertificateMonitoringService(logger, notifications, options);
```

Build the options from configuration values read elsewhere, falling back to the defaults when a value is missing.

```csharp
using CaddyVpsToolkit.Services;

int? configuredWarn = /* read from appsettings or environment */ null;
int? configuredCritical = /* read from appsettings or environment */ null;

var defaults = new SslCertificateMonitoringOptions();
var options = new SslCertificateMonitoringOptions
{
    WarnDays = configuredWarn ?? defaults.WarnDays,
    CriticalDays = configuredCritical ?? defaults.CriticalDays
};

var monitor = new CaddyVpsToolkit.Monitoring.CertificateMonitor(options);
```

## Notes

- **Read once at construction.** `SslCertificateMonitoringService` copies `WarnDays` and `CriticalDays` into private fields in its constructor. Changing the options object afterwards has no effect on an existing service instance; create a new service to apply new thresholds.
- **No validation.** The class does not check its values. Negative numbers, zero, and `WarnDays` smaller than `CriticalDays` are all accepted. Because the critical check runs first, a `WarnDays` value below `CriticalDays` means the warning band is never reached. Keep `WarnDays` greater than or equal to `CriticalDays`.
- **Expired certificates are not affected.** A certificate whose expiry date is already in the past is reported as expired before either threshold is considered.
- **Not thread-safe.** The properties are plain auto-properties with no synchronization. Configure the object before sharing it across threads, and do not mutate it while a service is being constructed from it. Because the service takes a snapshot of the values, one configured instance can be shared by several services as long as nobody changes it afterwards.
