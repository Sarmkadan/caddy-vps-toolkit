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

## ServicePortExtensions

`ServicePortExtensions` adds a collection of helper methods for the `ServicePort` model, making it easy to reason about port numbers, protocols, and their classifications (privileged, well‑known, registered, dynamic, HTTP, etc.). It also provides utilities for grouping ports, detecting conflicts, and generating human‑readable descriptions.

Example usage:
```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Domain.Models;

class Program
{
    static void Main()
    {
        // Create a few ServicePort instances
        var httpPort = new ServicePort
        {
            InternalPort = 8080,
            ExternalPort = 80,
            Protocol = "tcp",
            Description = "HTTP traffic"
        };

        var sshPort = new ServicePort
        {
            InternalPort = 22,
            ExternalPort = 22,
            Protocol = "tcp",
            Description = "SSH access"
        };

        // Individual checks
        Console.WriteLine($"httpPort.IsPrivilegedPort(): {httpPort.IsPrivilegedPort()}");
        Console.WriteLine($"sshPort.IsWellKnownPort(): {sshPort.IsWellKnownPort()}");
        Console.WriteLine($"sshPort.IsHttpPort(): {sshPort.IsHttpPort()}");
        Console.WriteLine($"httpPort.GetIanaServiceName(): {httpPort.GetIanaServiceName()}");
        Console.WriteLine($"sshPort.GetPortCategory(): {sshPort.GetPortCategory()}");
        Console.WriteLine($"httpPort.GetFormattedDescription(): {httpPort.GetFormattedDescription()}");

        // Collection helpers
        var ports = new List<ServicePort> { httpPort, sshPort };
        var byInternal = ports.GetPortsByInternalPort(22);
        var byExternal = ports.GetPortsByExternalPort(80);

        Console.WriteLine($"Ports with internal 22: {byInternal.Count}");
        Console.WriteLine($"Ports with external 80: {byExternal.Count}");

        // Conflict detection
        var anotherHttp = new ServicePort
        {
            InternalPort = 8081,
            ExternalPort = 80,
            Protocol = "tcp",
            Description = "Another HTTP service"
        };
        Console.WriteLine($"httpPort conflicts with anotherHttp: {httpPort.ConflictsWith(anotherHttp)}");
    }
}
```

The example demonstrates how to:

* Determine port classifications (`IsPrivilegedPort`, `IsWellKnownPort`, `IsRegisteredPort`, `IsDynamicPort`, `IsHttpPort`).
* Retrieve IANA service names and human‑readable categories (`GetIanaServiceName`, `GetPortCategory`).
* Search collections for ports sharing the same internal or external port (`GetPortsByInternalPort`, `GetPortsByExternalPort`).
* Detect port conflicts (`ConflictsWith`).
* Produce a formatted description that includes protocol, category, and optional IANA name (`GetFormattedDescription`).

## ConfigurationServiceJsonExtensions

`ConfigurationServiceJsonExtensions` adds JSON‑serialization helpers for `ConfigurationService`. It lets you turn the whole configuration into a JSON string, recreate a service from JSON, and safely attempt deserialization without throwing exceptions. Internally it uses a lightweight in‑memory `IConfigurationRepository` implementation to hold the deserialized key/value pairs.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;

class Program
{
    static async Task Main()
    {
        // Create a service and populate some configuration values
        var configService = new ConfigurationService();
        await configService.SetValueAsync("AppName", "Demo");
        await configService.SetValueAsync("Version", "1.0");

        // Serialize the whole configuration to JSON (indented for readability)
        string json = configService.ToJson(indented: true);
        Console.WriteLine("Serialized JSON:");
        Console.WriteLine(json);

        // Deserialize back to a new ConfigurationService instance
        var deserialized = ConfigurationServiceJsonExtensions.FromJson(json);
        Console.WriteLine("\nDeserialized value:");
        Console.WriteLine(await deserialized!.GetValueAsync("AppName"));

        // Try‑parse example that never throws
        if (ConfigurationServiceJsonExtensions.TryFromJson(json, out var parsed))
        {
            Console.WriteLine("\nTryFromJson succeeded:");
            Console.WriteLine(await parsed.GetValueAsync("Version"));
        }
    }
}
```

The example demonstrates the three public extension members (`ToJson`, `FromJson`, `TryFromJson`) together with the repository methods (`GetValueAsync`, `SetValueAsync`, `DeleteAsync`, `GetAllAsync`) that are used internally by the deserialized service.