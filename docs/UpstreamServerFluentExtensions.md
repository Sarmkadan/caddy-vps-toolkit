# UpstreamServerFluentExtensions

`UpstreamServerFluentExtensions` is a static class of extension methods for [UpstreamServer](UpstreamServer.md). The source lives in `Domain/Models/UpstreamServerFluentExtensions.cs` under the `CaddyVpsToolkit.Domain.Models` namespace. The methods let callers format an upstream's endpoint, test whether it can receive traffic, and set its weight in a chained expression.

Unlike the [LogEntryFluentExtensions](LogEntryFluentExtensions.md) methods, these extensions do not accept `null` receivers. The methods do not call `ArgumentNullException.ThrowIfNull`, so a `null` server causes a `NullReferenceException` when the method reads a member. The class holds no state, so the methods can be called from any thread. Only `WithWeight` modifies its receiver, and the other two methods only read it.

The class defines three public methods:

| Method | Returns | Purpose |
|--------|---------|---------|
| `ToEndpointString` | `string` | Formats the server as `address:port`. |
| `IsHealthyAndEnabled` | `bool` | Checks whether the server is healthy and its status is `Active`. |
| `WithWeight` | `UpstreamServer` | Validates and sets `Weight`, then returns the same instance. |

## API

### `ToEndpointString`

```csharp
public static string ToEndpointString(this UpstreamServer server)
```

Returns the endpoint of the upstream server in the form `address:port`.

- **Parameters**
  - `server`: The `UpstreamServer` to format. Must not be `null`.
- **Return value**: The same string that `UpstreamServer.GetUpstreamAddress` returns, which is `$"{Address}:{Port}"`. For example, an `Address` of `"10.0.0.2"` and a `Port` of `8080` produce `"10.0.0.2:8080"`.
- **Exceptions**: `NullReferenceException` if `server` is `null`. The method does not check for `null` before it calls `GetUpstreamAddress`.

The method does not validate the address or port. An empty `Address` produces a string that starts with a colon, such as `":8080"`. Call `Validate` on the server first if the value comes from user input.

### `IsHealthyAndEnabled`

```csharp
public static bool IsHealthyAndEnabled(this UpstreamServer server)
```

Determines whether the upstream server is both healthy and enabled for traffic.

- **Parameters**
  - `server`: The `UpstreamServer` to evaluate. Must not be `null`.
- **Return value**: `true` if `IsHealthy` is `true` and `Status` is `UpstreamServerStatus.Active`; otherwise `false`.
- **Exceptions**: `NullReferenceException` if `server` is `null`.

The method checks two fields and nothing else. It does not return `true` for `HalfOpen`, which `UpstreamServer.IsAvailable` accepts, so a server that is recovering from an outage is excluded here even though `IsAvailable` would report it as available. Use `IsAvailable` directly when half-open servers should receive traffic. A server with `Status` set to `Draining` or `Disabled` returns `false` even when `IsHealthy` is `true`.

### `WithWeight`

```csharp
public static UpstreamServer WithWeight(this UpstreamServer server, int weight)
```

Sets the weight of the upstream server and returns the same instance, so the call can be chained with other configuration.

- **Parameters**
  - `server`: The `UpstreamServer` to update. Must not be `null`.
  - `weight`: The new weight. Must be between 1 and 100 inclusive.
- **Return value**: The same `UpstreamServer` instance as `server`, with `Weight` set to `weight` and `UpdatedAt` set to the current UTC time.
- **Exceptions**:
  - `ArgumentOutOfRangeException` if `weight` is less than 1 or greater than 100. The check runs before the receiver is read, so an invalid weight throws this exception even when `server` is `null`.
  - `NullReferenceException` if `server` is `null` and `weight` is in range.

`WithWeight` changes the object it receives rather than returning a copy. Any other variable that refers to the same instance sees the new weight. Only `Weight` and `UpdatedAt` change; the method does not call `Validate` and does not touch `Status`, `IsHealthy`, or the other fields.

## Usage

The following examples show common ways to use these methods. They assume the usual `using` directive for the namespace:

```csharp
using CaddyVpsToolkit.Domain.Models;
```

### Example 1: Building an endpoint for configuration

`ToEndpointString` gives the `address:port` form that is used when writing an upstream into a generated configuration:

```csharp
using System;

var backend = new UpstreamServer
{
    Address = "10.0.0.2",
    Port = 8080
};

Console.WriteLine(backend.ToEndpointString());
// 10.0.0.2:8080
```

### Example 2: Selecting servers that can receive traffic

`IsHealthyAndEnabled` works as a predicate in LINQ, so a pool can be filtered without repeating the status and health checks:

```csharp
using System.Collections.Generic;
using System.Linq;

public static class UpstreamTargets
{
    public static IReadOnlyList<string> GetActiveEndpoints(IEnumerable<UpstreamServer> servers)
    {
        return servers
            .Where(s => s.IsHealthyAndEnabled())
            .Select(s => s.ToEndpointString())
            .ToList();
    }
}
```

A newly created `UpstreamServer` starts with `Status` set to `Active` and `IsHealthy` set to `true`, so it passes this filter until a health probe or an administrative change updates it.

### Example 3: Configuring weight while creating a server

`WithWeight` can be placed directly after the object initializer. The call fails fast on an invalid weight, so a bad value is caught when the server is built:

```csharp
using System;

try
{
    var primary = new UpstreamServer
    {
        Address = "10.0.0.2",
        Port = 8080
    }.WithWeight(5);

    var secondary = new UpstreamServer
    {
        Address = "10.0.0.3",
        Port = 8080
    }.WithWeight(250);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine(ex.Message);
    // Weight must be between 1 and 100. (Parameter 'weight')
}
```

Because `WithWeight` returns the same instance, `primary` has `Weight` set to `5`. The second call throws before `secondary` is assigned, so the variable is never created.

### Example 4: Updating weight on an existing server

The return value is not needed when the caller only wants to change the weight of a server it already holds. The change is visible through the original reference:

```csharp
using System;

var server = new UpstreamServer
{
    Address = "10.0.0.4",
    Port = 9000
};

var alias = server;
server.WithWeight(10);

Console.WriteLine(alias.Weight); // 10
```

Since the method mutates the server, do not call it on an instance that other code is reading from another thread without synchronization.

## Notes

*   **Null handling:** None of the methods check for a `null` receiver. Each one throws `NullReferenceException` when it reads a member. This differs from [LogEntryFluentExtensions](LogEntryFluentExtensions.md), which returns a default value for a `null` receiver. Check for `null` before calling these methods on a value that may be missing. The one exception is `WithWeight` with an out-of-range weight, which throws `ArgumentOutOfRangeException` first.
*   **Health versus availability:** `IsHealthyAndEnabled` and `UpstreamServer.IsAvailable` differ on `HalfOpen`. Choose the method that matches the traffic rule your code needs.
*   **Mutation:** `WithWeight` is the only method in this class that changes state. It modifies the receiver in place and updates `UpdatedAt`. The other two methods only read.
*   **Thread safety:** The methods hold no shared state. Calling `WithWeight` on an instance that another thread is reading or writing needs external locking.
*   **Validation:** `WithWeight` checks only the weight range. It does not validate `Address`, `Port`, or the other fields. See [UpstreamServer](UpstreamServer.md) for the full validation rules.
