# ServicePort

`ServicePort` represents a network port mapping used by a service managed through the Caddy VPS Toolkit. It captures the internal and external port numbers, the transport protocol, and metadata such as identifiers, description, visibility, and creation timestamp.

## API

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `Id` | `string` | Unique identifier for the port mapping within the toolkit. | — | The identifier string. | None. |
| `ServiceId` | `string` | Identifier of the service to which this port mapping belongs. | — | The service identifier string. | None. |
| `InternalPort` | `int` | Port number on the container or internal service endpoint. | — | Value between 1 and 65535. | None. |
| `ExternalPort` | `int` | Port number exposed on the host or public interface. | — | Value between 1 and 65535. | None. |
| `Protocol` | `PortProtocol` | Transport protocol used for the mapping (e.g., TCP, UDP). | — | One of the defined `PortProtocol` enum values. | None. |
| `Description` | `string` | Human‑readable description of the port mapping’s purpose. | — | Descriptive text; may be empty or null. | None. |
| `IsPublic` | `bool` | Indicates whether the external port is accessible from outside the host. | — | `true` if the port is publicly reachable, `false` otherwise. | None. |
| `CreatedAt` | `DateTime` | Timestamp when the port mapping was created. | — | UTC date and time of creation. | None. |
| `Validate` | `void` | Checks the consistency of the port mapping properties. Throws if any invariant is violated. | — | — | `ArgumentException` if `InternalPort` or `ExternalPort` is outside the valid range (1‑65535), if `Protocol` is undefined, or if `Id`/`ServiceId` are null or empty. |
| `GetPortMapping` | `string` | Produces a canonical string representation of the mapping, suitable for logging or configuration files. | — | A string formatted as `"{InternalPort}/{Protocol}->{ExternalPort}"` (e.g., `"8080/tcp->80"`). | None. |

## Usage

### Example 1: Creating and validating a port mapping

```csharp
using CaddyVpsToolkit.Models;

var port = new ServicePort
{
    Id = "port-001",
    ServiceId = "web-app",
    InternalPort = 80,
    ExternalPort = 8080,
    Protocol = PortProtocol.Tcp,
    Description = "HTTP traffic for the web application",
    IsPublic = true,
    CreatedAt = DateTime.UtcNow
};

try
{
    port.Validate(); // throws if any field is invalid
    Console.WriteLine($"Mapping: {port.GetPortMapping()}");
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Invalid port mapping: {ex.Message}");
}
```

### Example 2: Reading existing mappings and filtering public ones

```csharp
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Models;

IEnumerable<ServicePort> mappings = LoadPortMappings(); // hypothetical loader

var publicMappings = mappings
    .Where(p => p.IsPublic && p.Protocol == PortProtocol.Tcp)
    .Select(p => new { p.ServiceId, p.ExternalPort, p.Description });

foreach (var m in publicMappings)
{
    Console.WriteLine($"Service {m.ServiceId} exposes port {m.ExternalPort}: {m.Description}");
}
```

## Notes

- The `Validate` method does **not** check for conflicts with other `ServicePort` instances; callers must ensure uniqueness of `(ServiceId, InternalPort, Protocol)` or `(ServiceId, ExternalPort, Protocol)` as required by their deployment environment.
- `GetPortMapping` assumes the `PortProtocol` enum’s `ToString()` yields a lowercase protocol name (e.g., `"tcp"`). If the enum representation differs, the output format may need adjustment.
- All properties are mutable; therefore, the type is **not** thread‑safe for concurrent writes. Concurrent reads are safe provided no thread modifies the instance simultaneously.
- `CreatedAt` is intended to be set once at object construction; modifying it after creation may lead to misleading audit information.
- If `Description` is null, `GetPortMapping` will still return a valid string; the description field does not affect the port mapping representation.
