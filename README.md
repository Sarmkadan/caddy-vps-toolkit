## SslCertificateInfoExtensions

The `SslCertificateInfoExtensions` class provides a set of extension methods for working with `SslCertificateInfo` objects. These methods simplify common certificate operations, such as determining the certificate's status, formatting its validity period, and checking its issuer.

Example usage:
```csharp
var certificate = new SslCertificateInfo { ExpiresAt = DateTime.UtcNow.AddDays(30) };
var (status, message) = certificate.GetStatus();
Console.WriteLine($"Certificate status: {status}, Message: {message}");

var validityPeriod = certificate.FormatValidityPeriod();
Console.WriteLine($"Validity period: {validityPeriod}");

var daysUntilExpiry = certificate.FormatDaysUntilExpiry();
Console.WriteLine($"Days until expiry: {daysUntilExpiry}");

var isIssuedBy = certificate.IsIssuedBy("Example CA");
Console.WriteLine($"Is issued by: {isIssuedBy}");

var expiredCertificates = certificate.GetExpiredCertificates();
Console.WriteLine($"Expired certificates: {expiredCertificates.Count()}");

var expiringCertificates = certificate.GetExpiringCertificates();
Console.WriteLine($"Expiring certificates: {expiringCertificates.Count()}");

var issuerOrganization = certificate.GetIssuerOrganization();
Console.WriteLine($"Issuer organization: {issuerOrganization}");
```
