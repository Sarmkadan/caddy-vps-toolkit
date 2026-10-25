# CertificateStatusOptions

`CertificateStatusOptions` is a small configuration object that defines the expiry thresholds used when classifying an SSL certificate's health. It holds two day counts: the point at which a certificate is considered *expiring soon* (`WarningDays`) and the point at which it is considered *critical* (`CriticalDays`). It is passed to the `SslCertificateInfoExtensions.GetStatus` overload that accepts options, and it can check its own values with `Validate`.

## API

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `WarningDays` | `int` | Number of days before expiry that yields an `ExpiringSoon` status. Defaults to `30`, which matches Let's Encrypt's 90-day lifetime with renewal at 30 days. | — | The configured threshold. | None. |
| `CriticalDays` | `int` | Number of days before expiry that yields a `Critical` status. Defaults to `7`. | — | The configured threshold. | None. |
| `Validate` | `void` | Checks that the thresholds are consistent and positive. | — | Nothing. Returns normally when the options are valid. | `ArgumentException` if `WarningDays` is less than `CriticalDays`, if `WarningDays` is zero or negative, or if `CriticalDays` is zero or negative. |

The class is `sealed` and lives in the `CaddyVpsToolkit.Domain.Models` namespace. It has a parameterless constructor and no other constructors.

## Usage

### Example 1: Using the default thresholds

```csharp
using CaddyVpsToolkit.Domain.Models;

var options = new CertificateStatusOptions();

Console.WriteLine(options.WarningDays);  // 30
Console.WriteLine(options.CriticalDays); // 7
```

### Example 2: Customizing thresholds and validating them

Set the properties with an object initializer, then call `Validate` before using the options. `Validate` throws if the values are inconsistent, so calling it early gives a clear error instead of a misleading status later.

```csharp
using System;
using CaddyVpsToolkit.Domain.Models;

var options = new CertificateStatusOptions
{
    WarningDays = 14,
    CriticalDays = 3
};

options.Validate(); // Passes: the critical threshold is inside the warning threshold.
```

### Example 3: Checking a certificate with custom options

`SslCertificateInfoExtensions.GetStatus` calls `Validate` itself, so passing an invalid instance throws an `ArgumentException` at that point.

```csharp
using System;
using CaddyVpsToolkit.Domain.Models;

var certificate = new SslCertificateInfo
{
    Domain = "example.com",
    ExpiresAt = DateTime.UtcNow.AddDays(10)
};

var options = new CertificateStatusOptions
{
    WarningDays = 14,
    CriticalDays = 3
};

CertificateStatusResult result = certificate.GetStatus(options);

Console.WriteLine(result.Health);     // ExpiringSoon
Console.WriteLine(result.DaysRemaining); // 10
Console.WriteLine(result.Message);    // Certificate expires in 10 day(s) on ... .
```

### Example 4: Handling invalid thresholds

```csharp
using System;
using CaddyVpsToolkit.Domain.Models;

var options = new CertificateStatusOptions
{
    WarningDays = 5,
    CriticalDays = 10
};

try
{
    options.Validate();
}
catch (ArgumentException ex)
{
    // WarningDays (5) cannot be less than CriticalDays (10). Critical threshold must trigger before warning threshold.
    Console.Error.WriteLine(ex.Message);
}
```

### Example 5: Relying on defaults when no options are supplied

Passing `null` to the options overload of `GetStatus` makes the method use a default `CertificateStatusOptions` (30 and 7 days).

```csharp
using CaddyVpsToolkit.Domain.Models;

CertificateStatusResult result = certificate.GetStatus(null);
```

## Notes

- **Classification boundaries.** `GetStatus` checks the thresholds in this order, using the certificate's `DaysUntilExpiry`:
  1. `<= 0` returns `Expired`.
  2. `<= CriticalDays` returns `Critical`.
  3. `<= WarningDays` returns `ExpiringSoon`.
  4. Anything else returns `Valid`.

  Both thresholds are inclusive. A certificate with exactly `CriticalDays` days left is `Critical`.
- **Validation is not automatic.** Setting `WarningDays` or `CriticalDays` does not call `Validate`. An inconsistent pair is accepted silently until `Validate` runs, which happens when `GetStatus` receives the options.
- **Validation order.** `Validate` checks `WarningDays < CriticalDays` first, then `WarningDays <= 0`, then `CriticalDays <= 0`. Only the first failing check throws. For example, `WarningDays = 0` and `CriticalDays = 0` fails the second check, and `WarningDays = -2` with `CriticalDays = -1` fails the first.
- **Thread safety.** The class is a mutable POCO with public setters. Do not share one instance across threads while it is being changed.
- **No `ToString` override.** The class uses the default `object.ToString()`, which returns the type name. Log the two `int` values directly if you need them in output.
