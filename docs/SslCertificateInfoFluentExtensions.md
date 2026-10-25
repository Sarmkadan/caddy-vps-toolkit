# SslCertificateInfoFluentExtensions

`SslCertificateInfoFluentExtensions` is a static class of extension methods for [SslCertificateInfo](SslCertificateInfo.md). The source lives in `Domain/Models/SslCertificateInfoFluentExtensions.cs` under the `CaddyVpsToolkit.Domain.Models` namespace. The methods let callers count the days left on a certificate, test whether it expires within a time window, and get a short status string, all in one chained expression.

Unlike [LogEntryFluentExtensions](LogEntryFluentExtensions.md), these methods do not accept `null` receivers. Each one reads the certificate's dates directly, so a `null` `SslCertificateInfo` throws `NullReferenceException`. The class holds no state, so the methods can be called from any thread.

The class defines three public methods:

| Method | Returns | Purpose |
|--------|---------|---------|
| `DaysUntilExpiry` | `int` | Counts the whole days until expiry, measured from a supplied `DateTime`. |
| `IsExpiringWithin` | `bool` | Checks whether a valid certificate expires within a `TimeSpan` from now. |
| `ToStatusString` | `string` | Returns `"Valid"`, `"ExpiringSoon"`, `"Critical"`, or `"Expired"`. |

The class also contains an overload of `DaysUntilExpiry` without the `now` parameter. That one is the `SslCertificateInfo.DaysUntilExpiry` property, which is defined on the model and not in this class. See [SslCertificateInfo](SslCertificateInfo.md) for it.

## API

### `DaysUntilExpiry`

```csharp
public static int DaysUntilExpiry(this SslCertificateInfo info, DateTime now)
```

Calculates the number of whole days from `now` until the certificate's `ExpiresAt`. This overload takes the reference time as an argument, so results can be reproduced in tests and in batch reports.

- **Parameters**
  - `info`: The `SslCertificateInfo` to measure. Must not be `null`.
  - `now`: The point in time to count from.
- **Return value**: The difference in `TotalDays` between `ExpiresAt` and `now`, truncated toward zero. Returns `0` if the certificate has already expired.
- **Exceptions**: `NullReferenceException` if `info` is `null`.

The method converts both dates to UTC before subtracting. A `DateTime` with `DateTimeKind.Utc` is used as is. Any other kind goes through `ToUniversalTime()`, so a `DateTimeKind.Unspecified` value is read as local time. Use UTC values to avoid surprises.

The method does not check `IssuedAt`. A certificate that is not yet valid still returns a positive count.

Because the property and the method share a name, `cert.DaysUntilExpiry` (no parentheses) reads the property, which uses `DateTime.UtcNow`, and `cert.DaysUntilExpiry(now)` calls this method. Both return `int` and both clamp at zero.

### `IsExpiringWithin`

```csharp
public static bool IsExpiringWithin(this SslCertificateInfo info, TimeSpan timeSpan)
```

Determines whether the certificate is currently valid and expires within `timeSpan` from the current moment.

- **Parameters**
  - `info`: The `SslCertificateInfo` to check. Must not be `null`.
  - `timeSpan`: The time window to test against. Compared through its `TotalDays` value.
- **Return value**: `true` if `info.IsValid` is `true` and the certificate's `DaysUntilExpiry` property is less than or equal to `timeSpan.TotalDays`. Otherwise `false`.
- **Exceptions**: `NullReferenceException` if `info` is `null`.

The method reads the current time through `DateTime.UtcNow` and has no `now` parameter. The days value is truncated to a whole number first, so a certificate with 30.9 days left counts as 30. `IsExpiringWithin(TimeSpan.FromDays(30))` returns `true` for it.

An expired certificate, or one whose `IssuedAt` is in the future, returns `false`. This is the opposite of the "expiring" meaning, so check `IsValid` or `ToStatusString` if you need to report expired certificates. A valid certificate with fewer than one full day left reports `0` days, so it returns `true` for any non-negative window, including `TimeSpan.Zero`. A negative window always returns `false`.

### `ToStatusString`

```csharp
public static string ToStatusString(this SslCertificateInfo info)
```

Returns a short status word for the certificate, based on its validity window and remaining days.

- **Parameters**
  - `info`: The `SslCertificateInfo` to classify. Must not be `null`.
- **Return value**: One of four strings, checked in this order:
  - `"Expired"` if `info.IsValid` is `false`. This covers both past-expiry and not-yet-issued certificates.
  - `"Critical"` if `DaysUntilExpiry` is 7 or fewer.
  - `"ExpiringSoon"` if `DaysUntilExpiry` is 8 to 30.
  - `"Valid"` otherwise.
- **Exceptions**: `NullReferenceException` if `info` is `null`.

The thresholds, 7 and 30 days, are fixed in the method. Unlike [SslCertificateInfoExtensions](SslCertificateInfoExtensions.md) `GetStatus`, the method takes no options, so a different threshold needs a separate helper.

The result is a plain string, not the `SslCertificateStatus` enum. The strings match the enum names for `Valid`, `ExpiringSoon`, `Critical`, and `Expired`, but the enum's `Error` member is never returned here. Also, a not-yet-issued certificate is reported as `"Expired"`, which can be misleading in a report.

## Usage

The following examples show common ways to combine these methods. They assume the usual `using` directive for the namespace:

```csharp
using CaddyVpsToolkit.Domain.Models;
```

### Example 1: Reporting days left at a fixed time

Passing an explicit `now` makes the result repeatable. The example below fixes the reference time, so the output does not change between runs:

```csharp
using System;

var now = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

var cert = new SslCertificateInfo
{
    Domain = "example.com",
    IssuedAt = now.AddDays(-60),
    ExpiresAt = now.AddDays(20.9)
};

Console.WriteLine(cert.DaysUntilExpiry(now));  // 20 (fractional day truncated)

var expired = new SslCertificateInfo
{
    Domain = "old.example.com",
    IssuedAt = now.AddDays(-90),
    ExpiresAt = now.AddDays(-1)
};

Console.WriteLine(expired.DaysUntilExpiry(now));  // 0
```

### Example 2: Selecting certificates that need renewal

`IsExpiringWithin` works as a predicate in LINQ. The example below returns the certificates that will expire within 30 days, skipping any that are already invalid:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public static class RenewalPlanner
{
    public static IReadOnlyList<SslCertificateInfo> FindRenewalCandidates(IEnumerable<SslCertificateInfo> certificates)
    {
        return certificates
            .Where(c => c.IsExpiringWithin(TimeSpan.FromDays(30)))
            .ToList();
    }
}
```

Because `IsExpiringWithin` returns `false` for expired certificates, this list does not include them. Add a separate `IsValid` check if the expired ones should be reported too.

### Example 3: Building a status summary

`ToStatusString` gives a consistent label for dashboards and notifications. The example below groups a list of certificates by status:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public static class CertificateSummary
{
    public static IReadOnlyDictionary<string, int> CountByStatus(IEnumerable<SslCertificateInfo> certificates)
    {
        return certificates
            .GroupBy(c => c.ToStatusString())
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
```

The result always contains only the four strings the method can return, so the keys can be used directly in a fixed set of labels.

### Example 4: Combining the methods for an alert decision

The three methods can be combined when a single check should decide whether to send an alert:

```csharp
using System;

public static class CertificateAlert
{
    public static string? BuildAlert(SslCertificateInfo cert)
    {
        var status = cert.ToStatusString();

        if (status is "Expired" or "Critical")
            return $"{cert.Domain}: {status}, {cert.DaysUntilExpiry} day(s) left";

        if (cert.IsExpiringWithin(TimeSpan.FromDays(30)))
            return $"{cert.Domain}: expires within 30 days";

        return null;
    }
}
```

The `Expired` branch comes first, so a not-yet-issued certificate is reported with the same wording as an expired one. Use `IssuedAt` to tell them apart if the message needs to be exact.

## Notes

*   **Null handling:** None of the methods accept a `null` receiver. Each one reads `info` directly, so a `null` value throws `NullReferenceException`. Check for `null` before calling these methods on a value that may be missing. This differs from [LogEntryFluentExtensions](LogEntryFluentExtensions.md), which returns a default value for a `null` receiver.
*   **Clock sources:** `DaysUntilExpiry(now)` uses the `now` argument, but `IsExpiringWithin` and `ToStatusString` use `DateTime.UtcNow` through the model's `IsValid` and `DaysUntilExpiry` members. Tests that need a fixed time should call `DaysUntilExpiry(now)` directly, or set the certificate dates relative to the real clock.
*   **Date kinds:** Store `IssuedAt` and `ExpiresAt` as UTC. A `DateTimeKind.Unspecified` value is converted as if it were local time, which can shift the day count by the local offset.
*   **Whole days:** Every day count is truncated toward zero, and negative values are clamped to `0`. A certificate with twelve hours left reports `0`, and so does one that expired yesterday. Use `ExpiresAt` directly if you need the fractional value.
*   **Stateless:** None of the methods read or write shared state, so they can be used concurrently on the same certificate as long as the `SslCertificateInfo` itself is not being modified during the call. See [SslCertificateInfo](SslCertificateInfo.md) for the data type.
