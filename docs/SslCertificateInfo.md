# SslCertificateInfo

Metadata for an SSL/TLS certificate retrieved from a remote domain. `SslCertificateInfo` is a sealed domain model in the `CaddyVpsToolkit.Domain.Models` namespace. It holds the subject, issuer, and validity window of a certificate, and exposes two computed helpers, `DaysUntilExpiry` and `IsValid`, for expiry checks. It contains no network or I/O logic; `SslCertificateMonitoringService` populates it from a live TLS handshake.

The same file also declares `SslCertificateStatus`, `CertificateStatusResult`, and `SslCertificateCheckResult`. Those types are documented separately. Extension methods for this model are described in [SslCertificateInfoExtensions](SslCertificateInfoExtensions.md).

## API

### `public string Domain`
Domain name the certificate was retrieved from. Defaults to `string.Empty`. The property is annotated with `[Required]`, but that attribute is only enforced when the object is validated through `System.ComponentModel.DataAnnotations`. Setting the property does not validate it.

### `public string Subject`
Certificate subject distinguished name, for example `CN=example.com`. Defaults to `string.Empty`.

### `public string Issuer`
Issuing authority distinguished name, for example `CN=R3, O=Let's Encrypt, C=US`. Defaults to `string.Empty`.

### `public DateTime IssuedAt`
Date and time at which the certificate became valid. The monitoring service stores this value in UTC. Values with `DateTimeKind.Unspecified` are treated as local time by the `IsValid` comparison.

### `public DateTime ExpiresAt`
Date and time at which the certificate expires. The monitoring service stores this value in UTC. Values with `DateTimeKind.Unspecified` are treated as local time by `DaysUntilExpiry` and `IsValid`.

### `public int DaysUntilExpiry`
Read-only computed property. Returns the number of whole days from the current UTC time until `ExpiresAt`. The value is truncated, not rounded, and is clamped to a minimum of `0`.

Once the certificate has expired, the value is `0`, which is the same value an unexpired certificate reports when less than one full day remains. Use `IsValid` or compare `ExpiresAt` to tell these cases apart.

### `public bool IsValid`
Read-only computed property. Returns `true` when the current UTC time is at or after `IssuedAt` and strictly before `ExpiresAt`. Returns `false` before the validity window starts and after it ends. This property does not check the trust chain, the host name, or revocation.

### `public override string ToString()`
Returns a diagnostic string of the form `SslCertificateInfo { Domain = ..., Subject = ..., Issuer = ..., IssuedAt = ..., ExpiresAt = ..., Domain = ... }`. The `Domain` value appears twice in the output (see Notes). The format is not intended for parsing.

## Usage

### Example 1: Building an instance from a remote certificate
This mirrors how `SslCertificateMonitoringService` maps an `X509Certificate2` into the model. Both dates are converted to UTC before they are stored.

```csharp
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using CaddyVpsToolkit.Domain.Models;

static SslCertificateInfo FromHost(string domain, int port = 443)
{
    using var tcp = new TcpClient(domain, port);
    using var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false);
    ssl.AuthenticateAsClient(domain);

    var x509 = (X509Certificate2)ssl.RemoteCertificate!;

    return new SslCertificateInfo
    {
        Domain = domain,
        Subject = x509.Subject,
        Issuer = x509.Issuer,
        IssuedAt = x509.NotBefore.ToUniversalTime(),
        ExpiresAt = x509.NotAfter.ToUniversalTime()
    };
}
```

The example performs a blocking handshake and does not check the certificate chain. The monitoring service uses a callback that accepts any certificate, because it only needs the metadata.

### Example 2: Reading expiry state directly from the model
Because `DaysUntilExpiry` is clamped to zero, check `IsValid` before you rely on the day count.

```csharp
using System;
using CaddyVpsToolkit.Domain.Models;

var cert = new SslCertificateInfo
{
    Domain = "example.com",
    Subject = "CN=example.com",
    Issuer = "CN=R3, O=Let's Encrypt, C=US",
    IssuedAt = DateTime.UtcNow.AddDays(-60),
    ExpiresAt = DateTime.UtcNow.AddDays(10)
};

if (!cert.IsValid)
{
    Console.WriteLine($"{cert.Domain} is outside its validity window (expired {cert.ExpiresAt:yyyy-MM-dd}).");
}
else if (cert.DaysUntilExpiry <= 30)
{
    Console.WriteLine($"{cert.Domain} expires in {cert.DaysUntilExpiry} day(s).");
}
```

### Example 3: Logging an instance
`ToString()` is convenient for logs and diagnostics.

```csharp
using System;
using CaddyVpsToolkit.Domain.Models;

var cert = new SslCertificateInfo { Domain = "api.example.com", Issuer = "CN=E5, O=Let's Encrypt, C=US" };

Console.WriteLine(cert);
// SslCertificateInfo { Domain = api.example.com, Subject = , Issuer = CN=E5, O=Let's Encrypt, C=US, IssuedAt = ..., ExpiresAt = ..., Domain = api.example.com }
```

## Notes

*   **Duplicate `Domain` in `ToString()`**: The override writes the `Domain` value twice. This is a known quirk of the current implementation. The output is informational only, so the duplication has no effect on behavior.
*   **Expired versus expiring today**: `DaysUntilExpiry` returns `0` for both an expired certificate and one with less than 24 hours left. Use `IsValid` to distinguish them.
*   **Time zones**: The monitoring service always stores UTC. If you construct instances by hand, pass UTC values. `DateTimeKind.Unspecified` values are interpreted as local time, which can shift the result by the local UTC offset.
*   **No validation on assignment**: The type has no `Validate()` method and its setters do not check input. `Domain` is only checked if the caller runs DataAnnotations validation.
*   **Mutability**: All properties have public setters. Treat an instance as a snapshot of one handshake, and do not share a single instance across threads while it is being modified.
*   **Relationship to results**: `SslCertificateCheckResult.Certificate` holds an `SslCertificateInfo` when a retrieval succeeded and is `null` when it failed. Its `Status` is derived from the thresholds applied by the monitoring service, not from `IsValid` alone.
