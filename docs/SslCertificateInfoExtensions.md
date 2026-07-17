# SslCertificateInfoExtensions

The `SslCertificateInfoExtensions` class provides a set of static extension methods designed to simplify the inspection, formatting, and filtering of `SslCertificateInfo` objects within the `caddy-vps-toolkit` ecosystem. These utilities facilitate common operational tasks such as determining certificate validity status, calculating time remaining until expiration, identifying issuers, and filtering collections for certificates requiring immediate attention due to expiry.

## API

### GetStatus
Determines the current validity status of a certificate and provides a human-readable explanation.
*   **Signature:** `public static (SslCertificateStatus Status, string Message) GetStatus(this SslCertificateInfo certificate)`
*   **Purpose:** Evaluates the certificate's dates and revocation state to return a specific `SslCertificateStatus` enum value alongside a descriptive message.
*   **Parameters:**
    *   `certificate`: The `SslCertificateInfo` instance to evaluate.
*   **Return Value:** A tuple containing the `SslCertificateStatus` and a `string` message detailing the reason for the status (e.g., "Valid", "Expired since...", "Not yet valid").
*   **Exceptions:** Throws `ArgumentNullException` if `certificate` is null.

### FormatValidityPeriod
Generates a standardized string representation of the certificate's validity window.
*   **Signature:** `public static string FormatValidityPeriod(this SslCertificateInfo certificate)`
*   **Purpose:** Formats the `NotBefore` and `NotAfter` dates into a single readable string indicating the active period.
*   **Parameters:**
    *   `certificate`: The `SslCertificateInfo` instance to format.
*   **Return Value:** A string in the format "YYYY-MM-DD to YYYY-MM-DD" (or similar standard date format defined by the implementation).
*   **Exceptions:** Throws `ArgumentNullException` if `certificate` is null.

### FormatDaysUntilExpiry
Calculates and formats the number of days remaining until the certificate expires.
*   **Signature:** `public static string FormatDaysUntilExpiry(this SslCertificateInfo certificate)`
*   **Purpose:** Computes the difference between the current UTC time and the `NotAfter` date, returning a localized or standardized string representation (e.g., "15 days", "Expired 3 days ago").
*   **Parameters:**
    *   `certificate`: The `SslCertificateInfo` instance to evaluate.
*   **Return Value:** A string representing the time delta.
*   **Exceptions:** Throws `ArgumentNullException` if `certificate` is null.

### IsIssuedBy
Verifies if a certificate was issued by a specific organization.
*   **Signature:** `public static bool IsIssuedBy(this SslCertificateInfo certificate, string issuerName)`
*   **Purpose:** Compares the certificate's issuer organization name against the provided string, typically performing a case-insensitive comparison.
*   **Parameters:**
    *   `certificate`: The `SslCertificateInfo` instance to check.
    *   `issuerName`: The name of the expected Certificate Authority (CA).
*   **Return Value:** `true` if the issuer matches; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `certificate` or `issuerName` is null.

### GetExpiredCertificates
Filters a collection to return only certificates that have already expired.
*   **Signature:** `public static IEnumerable<SslCertificateInfo> GetExpiredCertificates(this IEnumerable<SslCertificateInfo> certificates)`
*   **Purpose:** Iterates through a sequence of certificates and yields those where the `NotAfter` date is prior to the current UTC time.
*   **Parameters:**
    *   `certificates`: The source collection of `SslCertificateInfo` objects.
*   **Return Value:** An `IEnumerable<SslCertificateInfo>` containing only expired certificates.
*   **Exceptions:** Throws `ArgumentNullException` if `certificates` is null.

### GetExpiringCertificates
Filters a collection to return certificates that are valid but approaching their expiration date.
*   **Signature:** `public static IEnumerable<SslCertificateInfo> GetExpiringCertificates(this IEnumerable<SslCertificateInfo> certificates, int thresholdDays = 30)`
*   **Purpose:** Yields certificates that are not yet expired but will expire within the specified `thresholdDays`.
*   **Parameters:**
    *   `certificates`: The source collection of `SslCertificateInfo` objects.
    *   `thresholdDays`: (Optional) The number of days within which a certificate is considered "expiring". Defaults to 30.
*   **Return Value:** An `IEnumerable<SslCertificateInfo>` containing certificates nearing expiration.
*   **Exceptions:** Throws `ArgumentNullException` if `certificates` is null.

### GetIssuerOrganization
Extracts the organization name from the certificate's issuer distinguished name.
*   **Signature:** `public static string GetIssuerOrganization(this SslCertificateInfo certificate)`
*   **Purpose:** Parses the issuer field to retrieve specifically the Organization (O) attribute.
*   **Parameters:**
    *   `certificate`: The `SslCertificateInfo` instance to parse.
*   **Return Value:** The issuer organization name, or `null`/empty if the attribute is not present.
*   **Exceptions:** Throws `ArgumentNullException` if `certificate` is null.

## Usage

### Example 1: Monitoring Dashboard Data Preparation
This example demonstrates how to generate a summary report for a list of managed certificates, highlighting those that require immediate renewal.

```csharp
using System;
using System.Linq;
using CaddyVpsToolkit.Security; // Hypothetical namespace

public class CertificateMonitor
{
    public void ReportStatus(IEnumerable<SslCertificateInfo> allCertificates)
    {
        var expiringSoon = allCertificates.GetExpiringCertificates(thresholdDays: 14);
        var expired = allCertificates.GetExpiredCertificates();

        Console.WriteLine("=== Critical Alerts ===");
        
        foreach (var cert in expired)
        {
            var status = cert.GetStatus();
            Console.WriteLine($"[CRITICAL] {cert.Subject}: {status.Message}");
        }

        foreach (var cert in expiringSoon)
        {
            Console.WriteLine($"[WARNING] {cert.Subject} expires in {cert.FormatDaysUntilExpiry()}");
            Console.WriteLine($"  Issuer: {cert.GetIssuerOrganization()}");
            Console.WriteLine($"  Valid: {cert.FormatValidityPeriod()}");
        }
    }
}
```

### Example 2: CA Compliance Validation
This example validates that all certificates in a configuration were issued by a trusted internal or external authority before deployment.

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Security;

public class DeploymentValidator
{
    private readonly string[] _trustedCAs = { "Let's Encrypt", "DigiCert Inc", "Internal Root CA" };

    public bool ValidateChain(IEnumerable<SslCertificateInfo> certificates)
    {
        foreach (var cert in certificates)
        {
            var issuer = cert.GetIssuerOrganization();
            
            // Check if the issuer matches any trusted CA
            bool isTrusted = false;
            foreach (var trustedCa in _trustedCAs)
            {
                if (cert.IsIssuedBy(trustedCa))
                {
                    isTrusted = true;
                    break;
                }
            }

            if (!isTrusted)
            {
                var status = cert.GetStatus();
                throw new InvalidOperationException(
                    $"Certificate for {cert.Subject} is invalid. Issuer '{issuer}' is not trusted. Status: {status.Message}");
            }
        }
        
        return true;
    }
}
```

## Notes

*   **Null Safety:** All extension methods strictly enforce null safety for the source `SslCertificateInfo` instance (the `this` parameter). Passing `null` will result in an `ArgumentNullException`. For collection-based extensions (`GetExpiredCertificates`, `GetExpiringCertificates`), the source enumerable itself must not be null, though the resulting enumeration may be empty.
*   **Time Zone Handling:** Calculations regarding expiration (e.g., `FormatDaysUntilExpiry`, `GetExpiredCertificates`) utilize Coordinated Universal Time (UTC) to ensure consistency regardless of the server's local time zone configuration.
*   **Thread Safety:** As this class consists entirely of stateless static methods that operate solely on passed-in parameters without modifying global state, it is fully thread-safe. Multiple threads may safely call these methods concurrently on the same or different certificate instances.
*   **Issuer Matching:** The `IsIssuedBy` method performs a comparison that is generally case-insensitive to accommodate variations in Distinguished Name formatting, but exact behavior may depend on the underlying string comparison logic implemented in the toolkit.
*   **Enumeration Timing:** The filtering methods (`GetExpiredCertificates`, `GetExpiringCertificates`) use deferred execution. The evaluation of certificate dates occurs when the returned `IEnumerable` is iterated, not when the method is called. Ensure the underlying collection is not modified during iteration.
