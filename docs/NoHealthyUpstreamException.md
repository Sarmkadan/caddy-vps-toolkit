# NoHealthyUpstreamException

`Core/Exceptions/NoHealthyUpstreamException.cs` defines an exception for the case where every upstream server in a pool is unhealthy and the configured policy refuses to route traffic to any of them. It lives in the `CaddyVpsToolkit.Core.Exceptions` namespace and derives directly from `System.Exception`, not from [CaddyVpsException](CaddyVpsException.md). It is `sealed`.

The type is tied to the `FailFast` value of `AllUpstreamsUnhealthyPolicy` (see `Configuration/UpstreamManagementOptions.cs`). Under `FailFast`, the load balancer is meant to throw this exception instead of sending a request to an unhealthy server. Under `FailOpen`, which is the default, no exception is thrown and the least-failed upstream is selected.

> **Status:** At the time of writing, no code in the repository constructs this exception. The only references are its own definition, the `<see>` links in `UpstreamManagementOptions.cs`, and a `using` directive in `LoadBalancing/AdaptiveLoadBalancer.cs`. The examples below show how to use it, but a `FailFast` pool does not throw it yet.

## API

### `public string PoolId`
The ID of the pool that has no healthy upstreams. It is set from the `poolId` constructor argument and is never `null`.

### `public IReadOnlyCollection<string> UpstreamIds`
The IDs of all upstream servers in the pool, healthy or not. It is set from the `upstreamIds` constructor argument and is never `null`. Its `Count` is the number used in the default message.

### `public NoHealthyUpstreamException(string poolId, IReadOnlyCollection<string> upstreamIds)`
Creates an exception with a generated message:

`No healthy upstreams available in pool '<poolId>'. All <count> upstreams are unhealthy.`

**Parameters:**
- `poolId` (`string`): The ID of the pool. Must not be `null`.
- `upstreamIds` (`IReadOnlyCollection<string>`): The IDs of every upstream in the pool. Must not be `null`.

**Result:**
- `Message`: the generated text above, with `<count>` taken from `upstreamIds.Count`
- `PoolId`, `UpstreamIds`: the values as passed

---

### `public NoHealthyUpstreamException(string poolId, IReadOnlyCollection<string> upstreamIds, string message)`
Creates an exception with a message supplied by the caller.

**Parameters:**
- `poolId` (`string`): The ID of the pool. Must not be `null`.
- `upstreamIds` (`IReadOnlyCollection<string>`): The IDs of every upstream in the pool. Must not be `null`.
- `message` (`string`): The message to use as-is.

**Result:**
- `Message`: the `message` value as passed
- `PoolId`, `UpstreamIds`: the values as passed

---

### `public NoHealthyUpstreamException(string poolId, IReadOnlyCollection<string> upstreamIds, string message, Exception innerException)`
Creates an exception with a caller-supplied message and an underlying cause.

**Parameters:**
- `poolId` (`string`): The ID of the pool. Must not be `null`.
- `upstreamIds` (`IReadOnlyCollection<string>`): The IDs of every upstream in the pool. Must not be `null`.
- `message` (`string`): The message to use as-is.
- `innerException` (`Exception`): The exception that caused the failure. It is stored in `InnerException`.

**Result:**
- `Message`: the `message` value as passed
- `InnerException`: the `innerException` value as passed
- `PoolId`, `UpstreamIds`: the values as passed

## Usage

### Example 1: Throwing under the FailFast policy
A selector that has checked every upstream and found none healthy can throw the exception directly:

```csharp
using System.Collections.Generic;
using CaddyVpsToolkit.Core.Exceptions;

IReadOnlyCollection<string> upstreamIds = new[] { "blog-1", "blog-2", "blog-3" };

// Every upstream failed its last health check
throw new NoHealthyUpstreamException("blog-pool", upstreamIds);
// Message: No healthy upstreams available in pool 'blog-pool'. All 3 upstreams are unhealthy.
// PoolId: "blog-pool"
// UpstreamIds: blog-1, blog-2, blog-3
```

### Example 2: Using a custom message
Pass a message when the default text does not fit the caller's context:

```csharp
using System.Collections.Generic;
using CaddyVpsToolkit.Core.Exceptions;

var upstreamIds = new List<string> { "api-1", "api-2" };

throw new NoHealthyUpstreamException(
    "api-pool",
    upstreamIds,
    "Refusing to route /v1/orders: both API upstreams failed health checks");
```

### Example 3: Keeping the underlying failure
When the last health check threw an exception, attach it so the cause is not lost:

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using CaddyVpsToolkit.Core.Exceptions;

try
{
    // Probe the last remaining upstream here
}
catch (HttpRequestException ex)
{
    throw new NoHealthyUpstreamException(
        "api-pool",
        new List<string> { "api-1" },
        "Last upstream probe failed with a network error",
        ex);
}
```

### Example 4: Handling the exception at the request boundary
Catch the type where requests are proxied and return a 503 instead of letting the exception escape:

```csharp
using System;
using CaddyVpsToolkit.Core.Exceptions;

try
{
    // Select an upstream and proxy the request here
}
catch (NoHealthyUpstreamException ex)
{
    Console.Error.WriteLine($"Pool '{ex.PoolId}' has no healthy upstreams ({ex.UpstreamIds.Count} total)");
    // Return HTTP 503 Service Unavailable to the client
}
```

## Notes

- **Not derived from `CaddyVpsException`**: This type does not have an `ErrorCode` or `Details`. Identify it by type, and read `PoolId` and `UpstreamIds` for context. If you need the common error-code handling, see [CaddyVpsException](CaddyVpsException.md) and [CoreExceptions](CoreExceptions.md).
- **Null checks run in different order**: The first constructor reads `upstreamIds.Count` while building the base message, before the null check runs. Passing `null` for `upstreamIds` therefore throws `NullReferenceException`, not `ArgumentNullException`. The other two constructors pass the message to the base class first and then check `upstreamIds`, so they throw `ArgumentNullException`. Pass a non-null collection in every case.
- **Count is captured at construction**: The message holds the count from when the exception was created. `UpstreamIds` is a reference to the collection passed in. If the caller later changes that collection, `UpstreamIds` changes too, but `Message` does not.
- **Policy default**: `AllUpstreamsUnhealthyPolicy` defaults to `FailOpen`, so this exception is only expected when a pool is explicitly configured with `FailFast`.
- **Related**: [AdaptiveLoadBalancer](AdaptiveLoadBalancer.md) and [UpstreamManagementOptions](UpstreamManagementOptions.md) cover the load-balancing side and the policy setting.
