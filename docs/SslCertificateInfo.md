# SslCertificateInfo

The `SslCertificateInfo` type serves as a data container within the `caddy-vps-toolkit` project, encapsulating the metadata and validation status of an SSL/TLS certificate associated with a specific domain. It aggregates critical details such as the certificate subject, issuer, validity period, and the current operational status, providing a standardized structure for reporting certificate health checks, expiration warnings, or retrieval errors.

## API

The following members define the public interface of `SslCertificateInfo`:

### Instance Properties

*   **`public string Domain`**
    Gets the domain name for which the certificate was issued or checked. This property identifies the target host in the validation context.

*   **`public string Subject`**
    Gets the subject distinguished name (DN) of the certificate, typically containing the Common Name (CN) and organization details of the entity owning the certificate.

*   **`public string Issuer`**
    Gets the issuer distinguished name (DN) of the certificate authority (CA) that signed and issued the certificate.

*   **`public DateTime IssuedAt`**
    Gets the precise date and time when the certificate became valid. This value represents the start of the certificate's validity period.

*   **`public DateTime ExpiresAt`**
    Gets the precise date and time when the certificate expires. This value represents the end of the certificate's validity period.

*   **`public SslCertificateStatus Status`**
    Gets the current validation status of the certificate (e.g., Valid, Expiring Soon, Expired, or Error). This enum value dictates the immediate action required regarding the certificate.

*   **`public SslCertificateInfo Certificate`**
    Gets a nested `SslCertificateInfo` object. In specific implementation contexts, this may represent the primary certificate details when the current instance acts as a wrapper or result container, though its presence alongside duplicate properties suggests a specific structural pattern for nested validation data.

*   **`public string Message`**
    Gets a human-readable message providing additional context about the certificate status. This is particularly useful when the `Status` indicates an error or a specific warning condition.

*   **`public DateTime CheckedAt`**
    Gets the date and time when the certificate status was last verified or the check operation was performed.

### Static Factory Methods

*   **`public static SslCertificateCheckResult CreateValid`**
    Creates a new `SslCertificateCheckResult` instance representing a successfully validated certificate.
    *   **Parameters**: None (assumes context or default valid state construction).
    *   **Return Value**: A `SslCertificateCheckResult` object with `Status` set to valid.
    *   **Throws**: No exceptions expected under normal operation.

*   **`public static SslCertificateCheckResult CreateExpiringSoon`**
    Creates a new `SslCertificateCheckResult` instance indicating the certificate is approaching its expiration date.
    *   **Parameters**: None.
    *   **Return Value**: A `SslCertificateCheckResult` object with `Status` set to expiring soon.
    *   **Throws**: No exceptions expected under normal operation.

*   **`public static SslCertificateCheckResult CreateExpired`**
    Creates a new `SslCertificateCheckResult` instance indicating the certificate has passed its expiration date.
    *   **Parameters**: None.
    *   **Return Value**: A `SslCertificateCheckResult` object with `Status` set to expired.
    *   **Throws**: No exceptions expected under normal operation.

*   **`public static SslCertificateCheckResult CreateError`**
    Creates a new `SslCertificateCheckResult` instance indicating a failure occurred during the certificate check process.
    *   **Parameters**: None.
    *   **Return Value**: A `SslCertificateCheckResult` object with `Status` set to error, typically populated with a descriptive `Message`.
    *   **Throws**: No exceptions expected under normal operation.

## Usage

### Example 1: Inspecting Certificate Validity
This example demonstrates how to access properties of an existing `SslCertificateInfo` instance to determine if a certificate requires renewal.

```csharp
using CaddyVpsToolkit;

public void CheckCertificateRenewal(SslCertificateInfo certInfo)
{
    if (certInfo.Status == SslCertificateStatus.ExpiringSoon)
    {
        Console.WriteLine($"Alert: Certificate for {certInfo.Domain} is expiring soon.");
        Console.WriteLine($"Issuer: {certInfo.Issuer}");
        Console.WriteLine($"Expires: {certInfo.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Details: {certInfo.Message}");
    }
    else if (certInfo.Status == SslCertificateStatus.Expired)
    {
        Console.WriteLine($"Critical: Certificate for {certInfo.Domain} has expired on {certInfo.ExpiresAt}.");
    }
}
```

### Example 2: Generating Status Results via Factory Methods
This example illustrates the use of static factory methods to generate standardized check results based on application logic.

```csharp
using CaddyVpsToolkit;
using System;

public SslCertificateCheckResult EvaluateCertificateHealth(DateTime expiresAt, bool checkSuccessful)
{
    if (!checkSuccessful)
    {
        return SslCertificateInfo.CreateError();
    }

    var timeUntilExpiry = expiresAt - DateTime.UtcNow;

    if (timeUntilExpiry.TotalDays <= 0)
    {
        return SslCertificateInfo.CreateExpired();
    }
    
    if (timeUntilExpiry.TotalDays <= 30)
    {
        return SslCertificateInfo.CreateExpiringSoon();
    }

    return SslCertificateInfo.CreateValid();
}
```

## Notes

*   **Property Redundancy**: The type definition includes the `Domain` property twice in its public signature. In runtime behavior, this typically resolves to a single backing field, but consumers should be aware that IntelliSense or reflection may expose this duplication depending on the compiler output.
*   **Nested Structure**: The presence of the `Certificate` property of type `SslCertificateInfo` suggests a potential for recursive data structures or a wrapper pattern where the outer instance holds metadata (like `CheckedAt` and `Status`) while the inner `Certificate` property holds the raw certificate details. Care should be taken to avoid infinite recursion if traversing this property graph.
*   **Thread Safety**: The instance properties are mutable getters without explicit synchronization mechanisms. While the static factory methods (`CreateValid`, `CreateExpired`, etc.) are inherently thread-safe as they do not rely on shared mutable state, instances of `SslCertificateInfo` should be treated as immutable once constructed or protected by external locking if modified across threads.
*   **Time Zones**: The `DateTime` properties (`IssuedAt`, `ExpiresAt`, `CheckedAt`) should be assumed to be in UTC unless otherwise documented in the consuming application, to ensure accurate comparison logic across different server time zones.
