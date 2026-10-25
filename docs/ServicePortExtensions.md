# ServicePortExtensions

`ServicePortExtensions` is a static class of extension methods for [ServicePort](ServicePort.md). The source lives in `Domain/Models/ServicePortExtensions.cs` under the `CaddyVpsToolkit.Domain.Models` namespace. The methods classify a port number (privileged, registered, dynamic, HTTP), look up its IANA service name, find other mappings that share a port, detect external-port conflicts, and build a readable description.

Unlike the [LogEntryFluentExtensions](LogEntryFluentExtensions.md) methods, these extensions do not accept `null` receivers. Each method calls `ArgumentNullException.ThrowIfNull` on its input first, so a `null` port or collection throws instead of returning a default value. The class holds no state, so the methods can be called from any thread.

The class defines eleven public methods:

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsPrivilegedPort` | `bool` | Checks whether `ExternalPort` is in the range 1–1023. |
| `IsWellKnownPort` | `bool` | Same result as `IsPrivilegedPort`. |
| `IsRegisteredPort` | `bool` | Checks whether `ExternalPort` is in the range 1024–49151. |
| `IsDynamicPort` | `bool` | Checks whether `ExternalPort` is in the range 49152–65535. |
| `GetIanaServiceName` | `string?` | Returns the IANA service name for a known `ExternalPort`, or `null`. |
| `GetPortCategory` | `string` | Returns a category label for `ExternalPort`. |
| `IsHttpPort` | `bool` | Checks whether `ExternalPort` is 80, 443, 8080, or 8443. |
| `GetPortsByInternalPort` | `IReadOnlyList<ServicePort>` | Filters a collection by `InternalPort`. |
| `GetPortsByExternalPort` | `IReadOnlyList<ServicePort>` | Filters a collection by `ExternalPort`. |
| `ConflictsWith` | `bool` | Checks whether two mappings use the same external port and protocol. |
| `GetFormattedDescription` | `string` | Builds a one-line description of the mapping. |

The classification methods read `ExternalPort`, the port exposed on the host. Only `GetPortsByInternalPort` reads `InternalPort`.

## API

### `IsPrivilegedPort`

```csharp
public static bool IsPrivilegedPort(this ServicePort port)
```

Determines whether the mapping's external port is privileged, meaning the range 1–1023.

- **Parameters**
  - `port`: The `ServicePort` to evaluate. Must not be `null`.
- **Return value**: `true` if `ExternalPort` is between 1 and 1023 inclusive; otherwise `false`.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

### `IsWellKnownPort`

```csharp
public static bool IsWellKnownPort(this ServicePort port)
```

Determines whether the mapping's external port is a well-known port.

- **Parameters**
  - `port`: The `ServicePort` to evaluate. Must not be `null`.
- **Return value**: The same value that `IsPrivilegedPort` returns for this port.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

The XML comment on this method says "ports 0-1023", but the method calls `IsPrivilegedPort`, which checks 1–1023. Port 0 is never a valid `ExternalPort` (`ServicePort.Validate` requires at least 1), so the difference has no effect on validated mappings. Use `IsPrivilegedPort` and `IsWellKnownPort` interchangeably.

### `IsRegisteredPort`

```csharp
public static bool IsRegisteredPort(this ServicePort port)
```

Determines whether the mapping's external port is in the registered range.

- **Parameters**
  - `port`: The `ServicePort` to evaluate. Must not be `null`.
- **Return value**: `true` if `ExternalPort` is between 1024 and 49151 inclusive; otherwise `false`.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

### `IsDynamicPort`

```csharp
public static bool IsDynamicPort(this ServicePort port)
```

Determines whether the mapping's external port is in the dynamic or private range.

- **Parameters**
  - `port`: The `ServicePort` to evaluate. Must not be `null`.
- **Return value**: `true` if `ExternalPort` is between 49152 and 65535 inclusive; otherwise `false`.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

### `GetIanaServiceName`

```csharp
public static string? GetIanaServiceName(this ServicePort port)
```

Looks up the IANA service name for the mapping's external port.

- **Parameters**
  - `port`: The `ServicePort` to look up. Must not be `null`.
- **Return value**: The service name for one of the 29 ports in the built-in table, or `null` if the port is not in the table. The table includes `22` → `"ssh"`, `80` → `"http"`, `443` → `"https"`, `3306` → `"mysql"`, `5432` → `"postgresql"`, and `6379` → `"redis"`.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

The lookup uses only the port number. It does not check `Protocol`, so `53` returns `"domain"` for both TCP and UDP. The table is fixed in code, so ports that are not in it, such as `8443`, return `null`.

### `GetPortCategory`

```csharp
public static string GetPortCategory(this ServicePort port)
```

Returns a category label for the mapping's external port.

- **Parameters**
  - `port`: The `ServicePort` to classify. Must not be `null`.
- **Return value**: One of the following strings:
  - `"Well-known port"` for values up to 1023. This includes zero and negative values.
  - `"Registered port"` for 1024–49151.
  - `"Dynamic/Private port"` for 49152–65535.
  - `"Reserved"` for values above 65535.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

The category boundaries match the `Is*Port` methods for all valid port numbers.

### `IsHttpPort`

```csharp
public static bool IsHttpPort(this ServicePort port)
```

Determines whether the mapping's external port is commonly used for HTTP or HTTPS traffic.

- **Parameters**
  - `port`: The `ServicePort` to evaluate. Must not be `null`.
- **Return value**: `true` if `ExternalPort` is 80, 443, 8080, or 8443; otherwise `false`.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

The check is based on the port number only. A UDP mapping on port 443 returns `true`.

### `GetPortsByInternalPort`

```csharp
public static IReadOnlyList<ServicePort> GetPortsByInternalPort(this IEnumerable<ServicePort> ports, int internalPort)
```

Returns every mapping in the sequence whose `InternalPort` equals `internalPort`.

- **Parameters**
  - `ports`: The mappings to search. Must not be `null`.
  - `internalPort`: The internal port number to match.
- **Return value**: A new read-only list with the matching mappings, in their original order. The list is empty if nothing matches. The method does not remove duplicates.
- **Exceptions**: `ArgumentNullException` if `ports` is `null`. A `null` element inside the sequence causes a `NullReferenceException`.

### `GetPortsByExternalPort`

```csharp
public static IReadOnlyList<ServicePort> GetPortsByExternalPort(this IEnumerable<ServicePort> ports, int externalPort)
```

Returns every mapping in the sequence whose `ExternalPort` equals `externalPort`.

- **Parameters**
  - `ports`: The mappings to search. Must not be `null`.
  - `externalPort`: The external port number to match.
- **Return value**: A new read-only list with the matching mappings, in their original order. The list is empty if nothing matches.
- **Exceptions**: `ArgumentNullException` if `ports` is `null`. A `null` element inside the sequence causes a `NullReferenceException`.

Both filter methods read the sequence once and copy the matches, so the returned list does not change if the source collection changes later.

### `ConflictsWith`

```csharp
public static bool ConflictsWith(this ServicePort port, ServicePort other)
```

Determines whether two mappings compete for the same external endpoint.

- **Parameters**
  - `port`: The `ServicePort` to check. Must not be `null`.
  - `other`: The `ServicePort` to compare against. Must not be `null`.
- **Return value**: `true` if both mappings have the same `ExternalPort` and the same `Protocol`; otherwise `false`.
- **Exceptions**: `ArgumentNullException` if `port` or `other` is `null`.

Only `ExternalPort` and `Protocol` are compared. `InternalPort`, `ServiceId`, and `IsPublic` are ignored, so two services that publish different internal ports on the same external port conflict. A mapping also conflicts with itself, so exclude the candidate when checking it against a collection that contains it.

### `GetFormattedDescription`

```csharp
public static string GetFormattedDescription(this ServicePort port)
```

Builds a one-line description of the mapping, including the port mapping, protocol, category, and IANA name.

- **Parameters**
  - `port`: The `ServicePort` to describe. Must not be `null`.
- **Return value**: One of the following forms:
  - Without an IANA name: `"{InternalPort} → {ExternalPort}/{Protocol} ({category})"`
  - With an IANA name: `"{InternalPort} → {ExternalPort}/{Protocol} ({category}, {ianaName})"`
  - With an IANA name and a non-empty `Description`: the same as above with `" - {Description}"` appended.
- **Exceptions**: `ArgumentNullException` if `port` is `null`.

`Protocol` is written with the enum name, so the output shows `Tcp` or `Udp`, not `tcp` or `udp`. The `Description` is appended only when the IANA name is present, so a mapping without an IANA name omits its description from the result.

## Usage

The following examples show common ways to use these methods. They assume the usual `using` directive for the namespace:

```csharp
using CaddyVpsToolkit.Domain.Models;
```

The examples use `PortProtocol` for the protocol. `ServicePort.Protocol` is an enum, not a string.

### Example 1: Classifying a port

The classification methods can be called on a single mapping:

```csharp
using System;

var httpPort = new ServicePort
{
    InternalPort = 8080,
    ExternalPort = 80,
    Protocol = PortProtocol.Tcp,
    Description = "HTTP traffic"
};

Console.WriteLine(httpPort.IsPrivilegedPort());   // True
Console.WriteLine(httpPort.IsHttpPort());         // True
Console.WriteLine(httpPort.GetPortCategory());    // Well-known port
Console.WriteLine(httpPort.GetIanaServiceName()); // http
```

### Example 2: Describing a mapping

`GetFormattedDescription` combines the port, protocol, category, and IANA name into one line:

```csharp
using System;

var sshPort = new ServicePort
{
    InternalPort = 22,
    ExternalPort = 22,
    Protocol = PortProtocol.Tcp,
    Description = "SSH access"
};

var dbPort = new ServicePort
{
    InternalPort = 3000,
    ExternalPort = 3000,
    Protocol = PortProtocol.Tcp
};

Console.WriteLine(sshPort.GetFormattedDescription());
// 22 → 22/Tcp (Well-known port, ssh) - SSH access

Console.WriteLine(dbPort.GetFormattedDescription());
// 3000 → 3000/Tcp (Registered port)
```

### Example 3: Finding conflicts before adding a mapping

`ConflictsWith` lets a caller reject a new mapping that would use an external port already claimed by another mapping:

```csharp
using System.Collections.Generic;
using System.Linq;

public static class PortPlanner
{
    public static IReadOnlyList<ServicePort> FindConflicts(
        IEnumerable<ServicePort> existing,
        ServicePort candidate)
    {
        return existing
            .Where(p => !ReferenceEquals(p, candidate) && p.ConflictsWith(candidate))
            .ToList();
    }
}
```

The `ReferenceEquals` check skips the candidate when it is already in the collection, because a mapping always conflicts with itself.

### Example 4: Grouping mappings by port

The filter methods return a read-only list, so the result can be returned directly from a service method:

```csharp
using System;
using System.Collections.Generic;

var ports = new List<ServicePort>
{
    new() { ServiceId = "web", InternalPort = 8080, ExternalPort = 80, Protocol = PortProtocol.Tcp },
    new() { ServiceId = "api", InternalPort = 8080, ExternalPort = 8081, Protocol = PortProtocol.Tcp },
    new() { ServiceId = "dns", InternalPort = 53, ExternalPort = 53, Protocol = PortProtocol.Udp }
};

IReadOnlyList<ServicePort> onInternal8080 = ports.GetPortsByInternalPort(8080);
Console.WriteLine(onInternal8080.Count); // 2

IReadOnlyList<ServicePort> onExternal53 = ports.GetPortsByExternalPort(53);
Console.WriteLine(onExternal53[0].GetFormattedDescription());
// 53 → 53/Udp (Well-known port, domain)
```

### Example 5: Separating ports by range

The range methods can be combined to build a summary of a port list:

```csharp
using System.Collections.Generic;
using System.Linq;

public static class PortSummary
{
    public static (int Privileged, int Registered, int Dynamic) Count(IEnumerable<ServicePort> ports)
    {
        var list = ports.ToList();

        return (
            list.Count(p => p.IsPrivilegedPort()),
            list.Count(p => p.IsRegisteredPort()),
            list.Count(p => p.IsDynamicPort()));
    }
}
```

Every mapping with a valid external port falls into exactly one of the three ranges. A mapping with an out-of-range port, such as `70000`, is counted in none of them.

## Notes

*   **Null handling:** Every method throws `ArgumentNullException` when its `ServicePort` or collection argument is `null`. This differs from [LogEntryFluentExtensions](LogEntryFluentExtensions.md), which returns a default value for a `null` receiver. Check for `null` before calling these methods on a value that may be missing.
*   **Null elements:** The two filter methods and the call to `GetPortsByInternalPort` do not check individual elements of the sequence. A `null` element causes a `NullReferenceException`.
*   **External versus internal ports:** Classification, IANA lookup, HTTP detection, and conflict checks all use `ExternalPort`. Only `GetPortsByInternalPort` uses `InternalPort`.
*   **Stateless:** None of the methods read or write shared state, so they are safe to call concurrently on mappings that are not being modified. See [ServicePort](ServicePort.md) for the data type itself.
